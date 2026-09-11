using SharedKernel;

namespace Project.Service.Domain.Entities;

/// <summary>
/// AiUsageLog - analytics for AI operations (draft/enhance/criteria/breakdown/usage). Stored in [project].AiUsageLogs (same flowboard DB, HasDefaultSchema project).
/// Never stores full Prompt/ResponseJson - only PromptHash (SHA256) + PromptPreview (500) + ResponsePreview (500) for privacy + GDPR.
/// Used by 7.6 Org sidebar and 7.7 Project sidebar GROUP BY Provider/Model tokens/cost. Separate AI folder DIP ready to extract to microservice.
/// </summary>
public class AiUsageLog : BaseEntity
{
    public Guid? OrgId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? TaskId { get; private set; }

    public string Operation { get; private set; } = string.Empty; // draft | enhance | criteria | breakdown | usage
    public string Provider { get; private set; } = string.Empty; // gemini | groq
    public string Model { get; private set; } = string.Empty; // gemini-3.5-flash | llama-3.1-8b-instant

    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public int TotalTokens { get; private set; }
    public decimal Cost { get; private set; } // USD, 0 for free tier

    public string Status { get; private set; } = "Success"; // Success | Failed | RateLimited
    public string? FailureReason { get; private set; }
    public bool FallbackUsed { get; private set; }
    public int DurationMs { get; private set; }

    public string PromptHash { get; private set; } = string.Empty; // SHA256 hex 64
    public string? PromptPreview { get; private set; } // first 500 chars
    public string? ResponsePreview { get; private set; } // first 500 chars

    // Alias for docs
    public Guid? WsId => WorkspaceId;

    private AiUsageLog() { }

    public AiUsageLog(
        Guid? orgId,
        Guid? workspaceId,
        Guid? projectId,
        Guid userId,
        Guid? taskId,
        string operation,
        string provider,
        string model,
        int inputTokens,
        int outputTokens,
        decimal cost,
        string status,
        string? failureReason,
        bool fallbackUsed,
        int durationMs,
        string promptHash,
        string? promptPreview,
        string? responsePreview)
    {
        OrgId = orgId;
        WorkspaceId = workspaceId;
        ProjectId = projectId;
        UserId = userId;
        TaskId = taskId;
        Operation = operation;
        Provider = provider;
        Model = model;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        TotalTokens = inputTokens + outputTokens;
        Cost = cost;
        Status = status;
        FailureReason = failureReason;
        FallbackUsed = fallbackUsed;
        DurationMs = durationMs;
        PromptHash = promptHash;
        PromptPreview = promptPreview;
        ResponsePreview = responsePreview;
    }
}
