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
/// GenerateDraft - 7.2 AI Draft (A). Issues header ✨ AI Draft → prompt 10-500 → POST /api/ai/draft {prompt,model,projectId} → Gemini/Groq JSON {title,description,checklist,labels,priority,issueType,storyPoints} → preview isDraft (no Id) editable → Create POST /tasks. Human-in-the-loop, no auto-create.
/// Operation=draft, rate limited via IAiService (Redis 3/min per ai:{userId}:{model} + 5 RPM global → 429). Logs to [project].AiUsageLogs hash/preview only.
/// </summary>
public record GenerateDraftCommand(string Prompt, string Model, Guid CallerId, Guid? ProjectId) : IRequest<Result<GenerateDraftResponse>>;

public record GenerateDraftResponse(
    string Title,
    string Description,
    string[] Checklist,
    string[] Labels,
    string Priority,
    string IssueType,
    int? StoryPoints,
    string Provider,
    string Model,
    string RawJson // original for debug, preview stored in AiUsageLog
);

public class GenerateDraftValidator : AbstractValidator<GenerateDraftCommand>
{
    public GenerateDraftValidator()
    {
        RuleFor(x => x.Prompt).NotEmpty().MinimumLength(10).MaximumLength(500).WithMessage("Prompt 10-500 chars required");
        RuleFor(x => x.Model).NotEmpty().Must(m => IsAllowed(m)).WithMessage("Model must be gemini-3.5-flash or llama-3.1-8b");
        RuleFor(x => x.CallerId).NotEmpty();
    }
    private static bool IsAllowed(string m)
    {
        if (string.IsNullOrWhiteSpace(m)) return false;
        var lower = m.Trim().ToLowerInvariant();
        return lower.Contains("gemini") || lower.Contains("llama") || lower.Contains("groq");
    }
}

public class GenerateDraftHandler : IRequestHandler<GenerateDraftCommand, Result<GenerateDraftResponse>>
{
    private readonly IAiService _ai;
    private readonly IApplicationDbContext _db;
    public GenerateDraftHandler(IAiService ai, IApplicationDbContext db) { _ai = ai; _db = db; }

    public async Task<Result<GenerateDraftResponse>> Handle(GenerateDraftCommand req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt) || req.Prompt.Length < 10 || req.Prompt.Length > 500)
            return Result<GenerateDraftResponse>.Failure("Prompt must be 10-500 chars");
        if (string.IsNullOrWhiteSpace(req.Model))
            return Result<GenerateDraftResponse>.Failure("Model required");

        Guid? orgId = null, wsId = null;
        if (req.ProjectId.HasValue)
        {
            try
            {
                var proj = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == req.ProjectId.Value, ct);
                if (proj != null)
                {
                    wsId = proj.WorkspaceId;
                    var wsOrg = await _db.Database.SqlQueryRaw<Guid?>("SELECT OrganizationId FROM [identity].[Workspaces] WHERE Id = @p0", wsId.Value).FirstOrDefaultAsync(ct);
                    orgId = wsOrg;
                }
            }
            catch { }
        }

        var aiReq = new AiGenerateRequest(orgId, wsId, req.ProjectId, null, "draft", req.Prompt, req.Model);
        var result = await _ai.GenerateAsync(aiReq, req.CallerId, ct);
        if (!result.IsSuccess) return Result<GenerateDraftResponse>.Failure(result.Error!);

        var raw = result.Value!.RawJson;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            string title = TryGetString(root, "title") ?? "AI Draft";
            string description = TryGetString(root, "description") ?? raw;
            string[] checklist = TryGetArray(root, "checklist");
            string[] labels = TryGetArray(root, "labels");
            string priority = TryGetString(root, "priority") ?? "Medium";
            // normalize priority 0-3 to string if needed
            if (int.TryParse(priority, out var pInt)) priority = pInt switch { 0 => "Low", 1 => "Medium", 2 => "High", 3 => "Urgent", _ => "Medium" };
            string issueType = TryGetString(root, "issueType") ?? "Task";
            int? storyPoints = TryGetInt(root, "storyPoints");

            // checklist/labels fallback to alternative keys
            if (checklist.Length == 0) checklist = TryGetArray(root, "checklist") ?? TryGetArray(root, "steps");
            if (labels.Length == 0) labels = TryGetArray(root, "labels") ?? TryGetArray(root, "tags");

            var resp = new GenerateDraftResponse(
                title.Length > 100 ? title[..100] : title,
                description,
                checklist,
                labels,
                priority,
                issueType,
                storyPoints,
                result.Value.Provider,
                result.Value.Model,
                raw
            );
            return Result<GenerateDraftResponse>.Success(resp);
        }
        catch (Exception ex)
        {
            return Result<GenerateDraftResponse>.Failure($"AI response parse failed: {ex.Message}. Raw: {raw[..Math.Min(200, raw.Length)]}");
        }
    }

    private static string? TryGetString(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString();
        // case-insensitive fallback
        foreach (var prop in root.EnumerateObject()) if (prop.Name.Equals(key, StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.String) return prop.Value.GetString();
        return null;
    }
    private static string[] TryGetArray(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var v)) { foreach (var prop in root.EnumerateObject()) if (prop.Name.Equals(key, StringComparison.OrdinalIgnoreCase)) { v = prop.Value; goto found; } return Array.Empty<string>(); }
    found:
        if (v.ValueKind == JsonValueKind.Array) return v.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToArray();
        return Array.Empty<string>();
    }
    private static int? TryGetInt(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var v))
        {
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var si)) return si;
        }
        return null;
    }
}
