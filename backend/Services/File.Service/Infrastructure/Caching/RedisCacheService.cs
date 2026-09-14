using System.Text.Json;
using StackExchange.Redis;
using File.Service.Application.Interfaces;
namespace File.Service.Infrastructure.Caching;
public class RedisCacheService : IRedisCacheService
{
    private readonly IConnectionMultiplexer? _mux;
    private readonly IDatabase? _db;
    public RedisCacheService(IConfiguration config, ILogger<RedisCacheService> logger)
    {
        var conn = config["Redis:Connection"] ?? config["Redis__Connection"] ?? "";
        if (string.IsNullOrWhiteSpace(conn) || conn.Contains("PASTE_")) { logger.LogWarning("[Redis] No connection - file caching disabled"); return; }
        try { var opts = ConfigurationOptions.Parse(conn); opts.AbortOnConnectFail = false; opts.ConnectRetry = 3; _mux = ConnectionMultiplexer.Connect(opts); _db = _mux.GetDatabase(); logger.LogInformation("[Redis] Connected - file"); }
        catch (Exception ex) { logger.LogWarning(ex, "[Redis] Connect failed - file"); }
    }
    public async Task<T?> GetAsync<T>(string key) { if (_db == null) return default; try { var v = await _db.StringGetAsync(key); if (v.IsNullOrEmpty) return default; return JsonSerializer.Deserialize<T>((string)v!); } catch { return default; } }
    public async Task SetAsync<T>(string key, T value, TimeSpan ttl) { if (_db == null) return; try { await _db.StringSetAsync(key, JsonSerializer.Serialize(value), ttl); } catch { } }
    public async Task RemoveAsync(string key) { if (_db == null) return; try { await _db.KeyDeleteAsync(key); } catch { } }
    public async Task RemoveByPrefixAsync(string prefix) { if (_mux == null || _db == null) return; try { foreach (var ep in _mux.GetEndPoints()) { var s = _mux.GetServer(ep); try { var k = s.Keys(pattern: $"{prefix}*").ToArray(); if (k.Length > 0) await _db.KeyDeleteAsync(k); } catch { } } } catch { } }
}
