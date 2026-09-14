namespace Identity.Service.Application.Interfaces;

/// <summary>
/// Abstraction for Redis cache — testable without Redis.
/// </summary>
public interface IRedisCacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan ttl);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
}
