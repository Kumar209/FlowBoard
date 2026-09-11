namespace Project.Service.Application.AI.DTOs;

/// <summary>
/// DTOs for AI module (Application layer, no Infra). Separate AI folder DIP - ready to extract to dedicated microservice without touching Domain.
/// </summary>
public record AiGenerateRequest(
    Guid? OrgId,
    Guid? WorkspaceId,
    Guid? ProjectId,
    Guid? TaskId,
    string Operation, // draft | enhance | criteria | breakdown
    string Prompt, // 10-500 chars validated via FluentValidation in 7.2+
    string Model // gemini-2.5-flash | llama-3.1-8b selectable via UI radio
);

public record AiGenerateResult(
    string Provider, // gemini | groq
    string Model,
    string RawJson, // parsed stable JSON string from provider (title/description etc. for 7.2+); truncated preview stored in AiUsageLog
    int InputTokens,
    int OutputTokens,
    int DurationMs,
    bool FallbackUsed,
    string? FailureReason
);

public record AiUsageLogDto(
    Guid Id,
    Guid? OrgId,
    Guid? WorkspaceId,
    Guid? ProjectId,
    Guid UserId,
    Guid? TaskId,
    string Operation,
    string Provider,
    string Model,
    int InputTokens,
    int OutputTokens,
    int TotalTokens,
    decimal Cost,
    string Status,
    string? FailureReason,
    bool FallbackUsed,
    int DurationMs,
    string PromptHash,
    string? PromptPreview,
    string? ResponsePreview,
    DateTime CreatedAt
);

public record AiUsageSummaryDto(
    string Provider,
    string Model,
    int TotalRequests,
    int TotalTokens,
    decimal TotalCost,
    int AvgDurationMs
);
