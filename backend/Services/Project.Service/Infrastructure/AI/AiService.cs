using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.AI.DTOs;
using Project.Service.Application.AI.Interfaces;
using Project.Service.Application.Interfaces;
using Project.Service.Domain.Entities;
using Project.Service.Infrastructure.AI.Providers;
using SharedKernel;

namespace Project.Service.Infrastructure.AI;

/// <summary>
/// AiService - orchestrator for AI ops. Implements IAiService DIP (Application defines, Infrastructure implements). Handles: rate limit (Redis 3/min per ai:{userId}:{model}, 5 RPM global), provider selection (Gemini fixed vs Groq selectable via UI radio), SHA256 hash + 500 preview (not full Prompt/ResponseJson), fallback detection, AiUsageLog persistence in [project].AiUsageLogs, token/cost calc. Ready to extract to microservice (move AI folder + entity).
/// </summary>
public class AiService : IAiService
{
    private readonly IApplicationDbContext _db;
    private readonly IAiRateLimiter _limiter;
    private readonly GeminiProvider _gemini;
    private readonly GroqProvider _groq;
    private readonly ILogger<AiService> _logger;

    public AiService(IApplicationDbContext db, IAiRateLimiter limiter, GeminiProvider gemini, GroqProvider groq, ILogger<AiService> logger)
    {
        _db = db;
        _limiter = limiter;
        _gemini = gemini;
        _groq = groq;
        _logger = logger;
    }

    public async Task<Result<AiGenerateResult>> GenerateAsync(AiGenerateRequest request, Guid callerUserId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var model = NormalizeModel(request.Model);
        var providerName = model == "llama-3.1-8b-instant" || model == "llama-3.1-8b" ? "groq" : "gemini";
        // Rate limit
        var (allowed, retryAfter) = await _limiter.TryAcquireAsync(callerUserId, model, ct);
        if (!allowed)
        {
            var failure = $"Rate limit 3/min per {model}. Retry after {retryAfter}s. Global 5 RPM.";
            await LogAsync(request, callerUserId, providerName, model, 0, 0, 0m, "RateLimited", failure, false, (int)sw.ElapsedMilliseconds, request.Prompt, "{}", ct);
            return Result<AiGenerateResult>.Failure($"Too Many Requests - {failure} (Retry-After: {retryAfter}s)");
        }

        IAiProvider provider = providerName == "groq" ? _groq : _gemini;

        try
        {
            var result = await provider.GenerateAsync(request.Prompt, request.Operation, ct);
            sw.Stop();
            var cost = EstimateCost(result.InputTokens, result.OutputTokens, providerName);
            await LogAsync(request, callerUserId, result.Provider, result.Model, result.InputTokens, result.OutputTokens, cost, "Success", null, result.FallbackUsed, result.DurationMs, request.Prompt, result.RawJson, ct);
            // Return result with DurationMs overwritten by limiter+provider total if needed
            var final = result with { DurationMs = (int)sw.ElapsedMilliseconds };
            return Result<AiGenerateResult>.Success(final);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[AI] Generate failed {Op} {Model} provider {Provider}", request.Operation, model, providerName);
            // Do NOT silently switch model when user explicitly selected via UI radio (Gemini fixed vs Groq).
            // Respect user choice: log failure and return error — fallback only if explicitly allowed via config (not auto).
            string failureReason = ex.Message;
            await LogAsync(request, callerUserId, providerName, model, 0, 0, 0m, "Failed", failureReason, false, (int)sw.ElapsedMilliseconds, request.Prompt, "{}", ct);
            return Result<AiGenerateResult>.Failure($"AI generation failed ({providerName} {model}): {failureReason}");
        }
    }

    public async Task<IReadOnlyList<AiUsageLogDto>> GetUsageAsync(Guid? orgId, Guid? projectId, Guid? userId, CancellationToken ct = default)
    {
        var q = _db.AiUsageLogs.AsNoTracking().AsQueryable();
        if (orgId.HasValue) q = q.Where(x => x.OrgId == orgId.Value);
        if (projectId.HasValue) q = q.Where(x => x.ProjectId == projectId.Value);
        if (userId.HasValue) q = q.Where(x => x.UserId == userId.Value);
        q = q.OrderByDescending(x => x.CreatedAt).Take(200);
        var list = await q.ToListAsync(ct);
        return list.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<AiUsageSummaryDto>> GetUsageSummaryAsync(Guid? orgId, Guid? projectId, CancellationToken ct = default)
    {
        var q = _db.AiUsageLogs.AsNoTracking().AsQueryable();
        if (orgId.HasValue) q = q.Where(x => x.OrgId == orgId.Value);
        if (projectId.HasValue) q = q.Where(x => x.ProjectId == projectId.Value);
        var grouped = await q.GroupBy(x => new { x.Provider, x.Model })
            .Select(g => new AiUsageSummaryDto(
                g.Key.Provider,
                g.Key.Model,
                g.Count(),
                g.Sum(x => x.TotalTokens),
                g.Sum(x => x.Cost),
                (int)g.Average(x => x.DurationMs)
            )).ToListAsync(ct);
        return grouped;
    }

    private async Task LogAsync(AiGenerateRequest req, Guid userId, string provider, string model, int inTok, int outTok, decimal cost, string status, string? failure, bool fallbackUsed, int durationMs, string prompt, string rawJson, CancellationToken ct)
    {
        try
        {
            var hash = ComputeHash(prompt);
            var promptPreview = prompt.Length > 500 ? prompt[..500] : prompt;
            var responsePreview = rawJson.Length > 500 ? rawJson[..500] : rawJson;
            var log = new AiUsageLog(
                req.OrgId, req.WorkspaceId, req.ProjectId, userId, req.TaskId,
                req.Operation, provider, model, inTok, outTok, cost, status, failure, fallbackUsed, durationMs,
                hash, promptPreview, responsePreview);
            _db.AiUsageLogs.Add(log);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AI] Failed to persist AiUsageLog");
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string NormalizeModel(string model)
    {
        if (string.IsNullOrWhiteSpace(model)) return "gemini-2.5-flash";
        var m = model.Trim().ToLowerInvariant();
        if (m.Contains("groq") || m.Contains("llama") || m == "llama-3.1-8b") return "llama-3.1-8b-instant";
        if (m.Contains("gemini")) return "gemini-2.5-flash";
        return m;
    }

    private static decimal EstimateCost(int inTok, int outTok, string provider)
    {
        // Free tiers: Gemini 2.5 Flash free 15 RPM/1M TPM/1500 RPD, Groq free llama-3.1-8b. Cost 0 but keep calculation for future paid.
        // If paid: Gemini ~ $0.0001 per 1k tokens, Groq ~ $0.00005 - mock 0.
        return 0m;
    }

    private static AiUsageLogDto ToDto(AiUsageLog e) => new(
        e.Id, e.OrgId, e.WorkspaceId, e.ProjectId, e.UserId, e.TaskId, e.Operation, e.Provider, e.Model,
        e.InputTokens, e.OutputTokens, e.TotalTokens, e.Cost, e.Status, e.FailureReason, e.FallbackUsed, e.DurationMs,
        e.PromptHash, e.PromptPreview, e.ResponsePreview, e.CreatedAt);
}
