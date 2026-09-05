namespace Project.Service.Application.Interfaces;

/// <summary>
/// IRedisCacheService - abstraction for Upstash Redis cache (board:{projectId} 5m, tasks:{hash} 2m). Application depends on interface (DIP), Infrastructure provides RedisCacheService. Mockable for unit tests without Redis.
/// </summary>
public interface IRedisCacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan ttl);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
}
