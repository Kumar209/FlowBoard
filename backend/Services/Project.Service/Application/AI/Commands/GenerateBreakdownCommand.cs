using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.AI.DTOs;
using Project.Service.Application.AI.Interfaces;
using Project.Service.Application.Interfaces;
using SharedKernel;
using System.Text.Json;

namespace Project.Service.Application.AI.Commands;

/// <summary>
/// GenerateBreakdown - 7.5 AI Breakdown (D). Subtasks card ✨ Breakdown → POST /api/ai/breakdown {taskId,title,description,model,projectId} → Gemini 3.5 Flash {subtasks:string[3-6]} checkbox editable Create Selected pending until Save PUT+POST batch.
/// Operation=breakdown, rate limited via IAiService (3/min). Logs hash/preview.
/// </summary>
public record GenerateBreakdownCommand(string Title, string Description, string Model, Guid CallerId, Guid? ProjectId, Guid? TaskId) : IRequest<Result<GenerateBreakdownResponse>>;

public record GenerateBreakdownResponse(string[] Subtasks, string Provider, string Model, string RawJson);

public class GenerateBreakdownValidator : AbstractValidator<GenerateBreakdownCommand>
{
    public GenerateBreakdownValidator()
    {
        RuleFor(x => x.Model).NotEmpty().Must(m => m.ToLower().Contains("gemini")).WithMessage("Model must be gemini-3.5-flash (Groq disabled)");
        RuleFor(x => x.CallerId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300).WithMessage("Title required");
    }
}

public class GenerateBreakdownHandler : IRequestHandler<GenerateBreakdownCommand, Result<GenerateBreakdownResponse>>
{
    private readonly IAiService _ai;
    private readonly IApplicationDbContext _db;
    public GenerateBreakdownHandler(IAiService ai, IApplicationDbContext db) { _ai = ai; _db = db; }

    public async Task<Result<GenerateBreakdownResponse>> Handle(GenerateBreakdownCommand req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) return Result<GenerateBreakdownResponse>.Failure("Title required");
        var mLower = req.Model?.Trim().ToLowerInvariant() ?? "";
        if (mLower.Contains("groq") || mLower.Contains("llama")) return Result<GenerateBreakdownResponse>.Failure("Groq disabled — only Gemini 3.5 Flash available");

        Guid? orgId = null, wsId = null;
        if (req.ProjectId.HasValue)
        {
            try
            {
                var proj = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == req.ProjectId.Value, ct);
                if (proj != null)
                {
                    wsId = proj.WorkspaceId;
                    var org = await _db.Database.SqlQueryRaw<Guid?>("SELECT OrganizationId FROM [identity].[Workspaces] WHERE Id = @p0", wsId.Value).FirstOrDefaultAsync(ct);
                    orgId = org;
                }
            }
            catch { }
        }

        string prompt;
        if (string.IsNullOrWhiteSpace(req.Description))
            prompt = $"Title: {req.Title}\nNo existing description. Break down this issue into 3-6 clear subtask titles (short, actionable).";
        else
            prompt = $"Title: {req.Title}\nDescription: {req.Description}\n\nBreak down this issue into 3-6 clear subtask titles (short, actionable).";
        if (prompt.Length > 2000) prompt = prompt[..2000];

        var aiReq = new AiGenerateRequest(orgId, wsId, req.ProjectId, req.TaskId, "breakdown", prompt, req.Model ?? "gemini-3.5-flash");
        var result = await _ai.GenerateAsync(aiReq, req.CallerId, ct);
        if (!result.IsSuccess) return Result<GenerateBreakdownResponse>.Failure(result.Error!);

        var raw = result.Value!.RawJson;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            string[] subtasks = Array.Empty<string>();
            if (root.TryGetProperty("subtasks", out var v) && v.ValueKind == JsonValueKind.Array)
                subtasks = v.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            else if (root.TryGetProperty("tasks", out var v2) && v2.ValueKind == JsonValueKind.Array)
                subtasks = v2.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToArray();
            else
            {
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Name.Equals("subtasks", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        subtasks = prop.Value.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToArray();
                        break;
                    }
                }
            }
            if (subtasks.Length == 0)
            {
                try { subtasks = JsonSerializer.Deserialize<string[]>(raw) ?? Array.Empty<string>(); } catch { }
            }
            subtasks = subtasks.Select(c => c.Trim()).Where(c => c.Length > 0).Select(c => c.Length > 200 ? c[..200] : c).ToArray();
            if (subtasks.Length == 0) subtasks = new[] { "Breakdown generation returned empty — please try again or add manually." };
            // Ensure 3-6 range? Truncate if >6, keep as is if less
            if (subtasks.Length > 6) subtasks = subtasks[..6];
            return Result<GenerateBreakdownResponse>.Success(new GenerateBreakdownResponse(subtasks, result.Value.Provider, result.Value.Model, raw));
        }
        catch
        {
            return Result<GenerateBreakdownResponse>.Failure("AI breakdown failed — please try again.");
        }
    }
}
