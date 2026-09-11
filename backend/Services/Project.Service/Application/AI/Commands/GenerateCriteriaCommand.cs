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
/// GenerateCriteria - 7.4 AI Acceptance Criteria (C). task-detail Acceptance card under Description manual Add + ✨ Generate → POST /api/ai/criteria {taskId,title,description,model,projectId} → Gemini 3.5 Flash {criteria:string[4-6]} checkbox editable Apply pending until Save PUT acceptanceCriteriaJson nullable.
/// Operation=criteria, rate limited via IAiService (3/min). Logs hash/preview.
/// </summary>
public record GenerateCriteriaCommand(string Title, string Description, string Model, Guid CallerId, Guid? ProjectId, Guid? TaskId) : IRequest<Result<GenerateCriteriaResponse>>;

public record GenerateCriteriaResponse(string[] Criteria, string Provider, string Model, string RawJson);

public class GenerateCriteriaValidator : AbstractValidator<GenerateCriteriaCommand>
{
    public GenerateCriteriaValidator()
    {
        RuleFor(x => x.Model).NotEmpty().Must(m => m.ToLower().Contains("gemini")).WithMessage("Model must be gemini-3.5-flash (Groq disabled)");
        RuleFor(x => x.CallerId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300).WithMessage("Title required");
    }
}

public class GenerateCriteriaHandler : IRequestHandler<GenerateCriteriaCommand, Result<GenerateCriteriaResponse>>
{
    private readonly IAiService _ai;
    private readonly IApplicationDbContext _db;
    public GenerateCriteriaHandler(IAiService ai, IApplicationDbContext db) { _ai = ai; _db = db; }

    public async Task<Result<GenerateCriteriaResponse>> Handle(GenerateCriteriaCommand req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) return Result<GenerateCriteriaResponse>.Failure("Title required");
        var mLower = req.Model?.Trim().ToLowerInvariant() ?? "";
        if (mLower.Contains("groq") || mLower.Contains("llama")) return Result<GenerateCriteriaResponse>.Failure("Groq disabled — only Gemini 3.5 Flash available");

        Guid? orgId = null, wsId = null;
        if (req.ProjectId.HasValue)
        {
            try
            {
                var proj = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == req.ProjectId.Value, ct);
                if (proj != null)
                {
                    wsId = proj.WorkspaceId;
                    var orgRow = await _db.Database.SqlQueryRaw<OrgRow>("SELECT OrganizationId FROM [identity].[Workspaces] WHERE Id = @p0", wsId.Value).FirstOrDefaultAsync(ct);
                    orgId = orgRow?.OrganizationId;
                }
            }
            catch { }
        }

        string prompt;
        if (string.IsNullOrWhiteSpace(req.Description))
            prompt = $"Title: {req.Title}\nNo existing description. Generate 4-6 acceptance criteria (Given/When/Then or bullet) for this issue.";
        else
            prompt = $"Title: {req.Title}\nDescription: {req.Description}\n\nGenerate 4-6 acceptance criteria (Given/When/Then or bullet) for this issue.";
        if (prompt.Length > 2000) prompt = prompt[..2000];

        var aiReq = new AiGenerateRequest(orgId, wsId, req.ProjectId, req.TaskId, "criteria", prompt, req.Model ?? "gemini-3.5-flash");
        var result = await _ai.GenerateAsync(aiReq, req.CallerId, ct);
        if (!result.IsSuccess) return Result<GenerateCriteriaResponse>.Failure(result.Error!);

        var raw = result.Value!.RawJson;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            string[] criteria = Array.Empty<string>();
            if (root.TryGetProperty("criteria", out var v) && v.ValueKind == JsonValueKind.Array)
                criteria = v.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            else if (root.TryGetProperty("acceptanceCriteria", out var v2) && v2.ValueKind == JsonValueKind.Array)
                criteria = v2.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToArray();
            else
            {
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Name.Equals("criteria", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        criteria = prop.Value.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToArray();
                        break;
                    }
                }
            }
            if (criteria.Length == 0)
            {
                // Fallback: try to parse as single string array like ["a","b"]
                try { criteria = JsonSerializer.Deserialize<string[]>(raw) ?? Array.Empty<string>(); } catch { }
            }
            criteria = criteria.Select(c => c.Trim()).Where(c => c.Length > 0).Select(c => c.Length > 300 ? c[..300] : c).ToArray();
            if (criteria.Length == 0) criteria = new[] { "Criteria generation returned empty — please try again or add manually." };
            return Result<GenerateCriteriaResponse>.Success(new GenerateCriteriaResponse(criteria, result.Value.Provider, result.Value.Model, raw));
        }
        catch
        {
            return Result<GenerateCriteriaResponse>.Failure("AI criteria failed — please try again.");
        }
    }
    private class OrgRow { public Guid OrganizationId { get; set; } }
}
