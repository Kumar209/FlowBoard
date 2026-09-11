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
/// GenerateEnhance - 7.3 AI Enhance (B). task-detail Description ✨ Enhance → POST /api/ai/enhance {taskId,title,description,model} → Gemini 3.5 Flash {title,description} diff Current vs AI Apply pending until Save PUT.
/// Operation=enhance, rate limited via IAiService (3/min). Logs hash/preview.
/// </summary>
public record GenerateEnhanceCommand(string Title, string Description, string Model, Guid CallerId, Guid? ProjectId, Guid? TaskId) : IRequest<Result<GenerateEnhanceResponse>>;

public record GenerateEnhanceResponse(string Title, string Description, string Provider, string Model, string RawJson);

public class GenerateEnhanceValidator : AbstractValidator<GenerateEnhanceCommand>
{
    public GenerateEnhanceValidator()
    {
        RuleFor(x => x.Model).NotEmpty().Must(m => m.ToLower().Contains("gemini")).WithMessage("Model must be gemini-3.5-flash (Groq disabled)");
        RuleFor(x => x.CallerId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300).WithMessage("Title required");
    }
}

public class GenerateEnhanceHandler : IRequestHandler<GenerateEnhanceCommand, Result<GenerateEnhanceResponse>>
{
    private readonly IAiService _ai;
    private readonly IApplicationDbContext _db;
    public GenerateEnhanceHandler(IAiService ai, IApplicationDbContext db) { _ai = ai; _db = db; }

    public async Task<Result<GenerateEnhanceResponse>> Handle(GenerateEnhanceCommand req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) return Result<GenerateEnhanceResponse>.Failure("Title required");
        if (req.Title.Length > 300) return Result<GenerateEnhanceResponse>.Failure("Title max 300");
        var mLower = req.Model?.Trim().ToLowerInvariant() ?? "";
        if (mLower.Contains("groq") || mLower.Contains("llama")) return Result<GenerateEnhanceResponse>.Failure("Groq disabled — only Gemini 3.5 Flash available");

        Guid? orgId = null, wsId = req.ProjectId != null ? null : null;
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

        // MNC Jira: description optional — if empty, generate from title only
        string prompt;
        if (string.IsNullOrWhiteSpace(req.Description))
            prompt = $"Title: {req.Title}\nNo existing description. Generate clear description from title only (objective, steps, acceptance hints). Keep original intent.";
        else
            prompt = $"Title: {req.Title}\nDescription: {req.Description}\n\nEnhance for clarity, fix grammar, add structured steps and acceptance hints. Keep original intent.";
        if (prompt.Length > 2000) prompt = prompt[..2000];

        var aiReq = new AiGenerateRequest(orgId, wsId, req.ProjectId, req.TaskId, "enhance", prompt, req.Model ?? "gemini-3.5-flash");
        var result = await _ai.GenerateAsync(aiReq, req.CallerId, ct);
        if (!result.IsSuccess) return Result<GenerateEnhanceResponse>.Failure(result.Error!);

        var raw = result.Value!.RawJson;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            string title = TryGetString(root, "title") ?? req.Title;
            string description = TryGetString(root, "description") ?? raw;
            return Result<GenerateEnhanceResponse>.Success(new GenerateEnhanceResponse(title, description, result.Value.Provider, result.Value.Model, raw));
        }
        catch
        {
            // Human Error Rule Section 10: never expose Raw/LineNumber/stack to client — log full server only
            return Result<GenerateEnhanceResponse>.Failure("AI enhance failed — please try again.");
        }
    }

    private static string? TryGetString(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString();
        foreach (var prop in root.EnumerateObject()) if (prop.Name.Equals(key, StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.String) return prop.Value.GetString();
        return null;
    }
}
