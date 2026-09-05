using MediatR;

namespace Project.Service.Application.Caching;

/// <summary>
/// ICacheableRequest - MNC-grade marker for queries that should be cached via Redis pipeline. Implementors provide CacheKey (e.g., CacheKeys.Board(id)) + Expiration (e.g., 5m). CachingBehavior<TRequest,TResponse> will intercept before handler, return cached Hit, or call handler then Set. Keeps Api thin and reuse across all future Redis usages (Tasks 3.x real-time, 4.x etc.).
/// </summary>
public interface ICacheableRequest<TResponse> : IRequest<TResponse>
{
    string CacheKey { get; }
    TimeSpan Expiration { get; }
    // Optional: X-Cache header is set via HttpContext in Api, not here - behavior is Http-agnostic
}
