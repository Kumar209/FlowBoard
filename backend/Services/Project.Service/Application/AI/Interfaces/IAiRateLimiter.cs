namespace Project.Service.Application.AI.Interfaces;

/// <summary>
/// IAiRateLimiter - Redis-backed 3/min per user per model (key ai:{userId}:{model}) + 5 RPM total. Returns 429 with RetryAfter when exceeded. Best-effort - if Redis missing, allows (local dev).
/// </summary>
public interface IAiRateLimiter
{
    Task<(bool Allowed, int RetryAfterSeconds)> TryAcquireAsync(Guid userId, string model, CancellationToken ct = default);
}
