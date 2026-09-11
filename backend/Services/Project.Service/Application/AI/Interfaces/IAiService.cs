using Project.Service.Application.AI.DTOs;
using SharedKernel;

namespace Project.Service.Application.AI.Interfaces;

/// <summary>
/// IAiService - orchestrator for AI operations (draft/enhance/criteria/breakdown). Handles rate limiting, provider selection (Gemini fixed + Groq selectable), token/cost calc, AiUsageLog persistence (hash/preview only, not full Prompt/ResponseJson), fallback. DIP - ready to extract to microservice.
/// </summary>
public interface IAiService
{
    Task<Result<AiGenerateResult>> GenerateAsync(AiGenerateRequest request, Guid callerUserId, CancellationToken ct = default);
    Task<IReadOnlyList<AiUsageLogDto>> GetUsageAsync(Guid? orgId, Guid? projectId, Guid? userId, CancellationToken ct = default);
    Task<IReadOnlyList<AiUsageSummaryDto>> GetUsageSummaryAsync(Guid? orgId, Guid? projectId, CancellationToken ct = default);
}
