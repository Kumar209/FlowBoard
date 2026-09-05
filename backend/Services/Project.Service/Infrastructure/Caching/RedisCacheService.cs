using StackExchange.Redis;
using System.Text.Json;

using Project.Service.Application.Interfaces;

namespace Project.Service.Infrastructure.Caching;

/// <summary>
/// RedisCacheService - Upstash Redis (same rediss:// key local/prod) for board:{projectId} TTL 5m, tasks:{hash} TTL 2m. Implements IRedisCacheService (DIP - Application depends on abstraction, testable via mock without Redis). Best-effort: if Redis missing or PASTE_, cache is no-op.
/// </summary>
public class RedisCacheService : IRedisCacheService
{
    private readonly IConnectionMultiplexer? _mux;
    private readonly IDatabase? _db;

    public RedisCacheService(IConfiguration config, ILogger<RedisCacheService> logger)
    {
        var conn = config["Redis:Connection"] ?? config["Redis__Connection"] ?? "";
        if (string.IsNullOrWhiteSpace(conn) || conn.Contains("PASTE_"))
        {
            logger.LogWarning("[Redis] No connection - caching disabled (set Redis:Connection rediss://...@upstash.io:6379)");
            return;
        }
        try
        {
            _mux = ConnectionMultiplexer.Connect(conn);
            _db = _mux.GetDatabase();
            logger.LogInformation("[Redis] Connected to Upstash");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Redis] Connect failed - caching disabled");
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        if (_db == null) return default;
        try
        {
            var val = await _db.StringGetAsync(key);
            if (val.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>((string)val!);
        }
        catch { return default; }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        if (_db == null) return;
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, ttl);
        }
        catch { }
    }

    public async Task RemoveAsync(string key)
    {
        if (_db == null) return;
        try { await _db.KeyDeleteAsync(key); } catch { }
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        if (_mux == null || _db == null) return;
        try
        {
            var endpoints = _mux.GetEndPoints();
            foreach (var ep in endpoints)
            {
                var server = _mux.GetServer(ep);
                // Upstash may not allow SCAN, fallback to simple keys delete via pattern if supported, else ignore
                try
                {
                    var keys = server.Keys(pattern: $"{prefix}*").ToArray();
                    if (keys.Length > 0) await _db.KeyDeleteAsync(keys);
                }
                catch { }
            }
        }
        catch { }
    }

    // Helpers moved to Application/Caching/CacheKeys for DIP (MNC grade) - keep obsolete for compat but delegate
    [Obsolete("Use CacheKeys.Board() from Application.Caching - do not reference Infra from Api")]
    public static string BoardKey(Guid projectId) => Application.Caching.CacheKeys.Board(projectId);
    [Obsolete("Use CacheKeys.Tasks() from Application.Caching")]
    public static string TasksKey(Guid projectId, string hash) => Application.Caching.CacheKeys.Tasks(projectId, hash);
}
