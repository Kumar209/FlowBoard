namespace Project.Service.Application.Interfaces;

/// <summary>
/// Abstraction for Redis cache (board:{projectId} 5m, tasks:{hash} 2m). Application depends on interface, infrastructure provides implementation. Testable without Redis.
/// </summary>
public interface IRedisCacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan ttl);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
    Task<bool> TryAcquireLockAsync(string key, string value, TimeSpan ttl);
    Task<bool> ReleaseLockAsync(string key, string value);
}
