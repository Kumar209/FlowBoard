using MediatR;
using Microsoft.Extensions.Logging;
using Project.Service.Application.Caching;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Behaviors;

/*
 * ============================================================================
 * CachingBehavior.cs - SIMPLE SUMMARY FOR ANY DEVELOPER
 * ============================================================================
 * What it is: A MediatR Pipeline - a wrapper that runs BEFORE and AFTER every
 *   query handler. Think of it as a middleware for MediatR (like Api middleware
 *   for HTTP, but for Send(query)).
 *
 * What it handles: ONLY queries that need caching - those that implement
 *   ICacheableRequest<TResponse> (e.g., GetBoardQuery, GetTasksQuery).
 *   It does NOT handle every query - if a query does NOT implement
 *   ICacheableRequest, this behavior is skipped (MediatR checks where clause).
 *   It also NEVER handles Commands (CreateTask etc.) - commands go direct to
 *   handlers where we explicitly invalidate cache via IRedisCacheService.
 *
 * How it works (read-through, 2 cases):
 *   1. HIT:  Try Redis GetAsync(CacheKey) -> if found, return cached JSON immediately,
 *           SKIP calling the handler (no SQL). ~4ms.
 *   2. MISS: GetAsync returns null -> call next() which runs the real handler (SQL
 *           SELECT Projects+BoardLists+Tasks) -> get BoardDto -> SetAsync(CacheKey, BoardDto, Expiration 5m/2m) -> return.
 *   If Redis is disabled (PASTE_ or Upstash down), it just calls next() (DB fallback) and never throws - best-effort.
 *
 * Why concrete class not interface: MediatR defines interface IPipelineBehavior<,> (abstraction). We ALWAYS write a CONCRETE class
 *   CachingBehavior<TRequest,TResponse> that IMPLEMENTS that interface. Program.cs registers it as
 *   AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>)) so MediatR can create it via DIP and inject IRedisCacheService + ILogger.
 *   You never write an interface for your behavior - you implement MediatR's interface.
 *
 * TTL used: CacheKeys.BoardTtlMinutes = 5m (board:{projectId} - larger, less churn), CacheKeys.TasksTtlMinutes = 2m
 *   (tasks:{projectId}:{hash12} - filtered, more churn). Set in each ICacheableRequest.Expiration.
 *
 * Where used in future: ANY service that needs Redis (File file:{taskId}, Notification notif:{userId}, Gemini gemini:{promptHash})
 *   just make its Query implement ICacheableRequest<T> with CacheKey + Expiration - pipeline auto-caches, no controller code.
 *
 * What you see in logs: [Cache] HIT board:{id} vs [Cache] MISS -> SET board:{id} 00:05:00 (Serilog Debug).
 * ============================================================================
 */
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableRequest<TResponse>
{
    private readonly IRedisCacheService _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IRedisCacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Try HIT
        try
        {
            var cached = await _cache.GetAsync<TResponse>(request.CacheKey);
            if (cached != null)
            {
                _logger.LogDebug("[Cache] HIT {Key}", request.CacheKey);
                return cached;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] Get failed {Key} - fallback to DB", request.CacheKey);
        }

        // MISS -> call handler (DB)
        var response = await next();

        // SET (best-effort)
        try
        {
            await _cache.SetAsync(request.CacheKey, response!, request.Expiration);
            _logger.LogDebug("[Cache] SET {Key} {Exp}", request.CacheKey, request.Expiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] Set failed {Key}", request.CacheKey);
        }

        return response;
    }
}
