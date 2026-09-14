using MediatR;

namespace Project.Service.Application.Caching;

/// <summary>
/// Marker for queries cached via Redis pipeline. Provides CacheKey + Expiration.
/// </summary>
public interface ICacheableRequest<TResponse> : IRequest<TResponse>
{
    string CacheKey { get; }
    TimeSpan Expiration { get; }
    // Optional: X-Cache header is set via HttpContext in Api, not here - behavior is Http-agnostic
}
