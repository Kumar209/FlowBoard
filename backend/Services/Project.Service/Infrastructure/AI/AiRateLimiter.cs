using Project.Service.Application.AI.Interfaces;
using Project.Service.Application.Interfaces;
using StackExchange.Redis;

namespace Project.Service.Infrastructure.AI;

/// <summary>
/// AiRateLimiter - Redis 3/min per user per model (key ai:{userId}:{model}) + global 5 RPM (ai:global). Uses INCR + EXPIRE 60. Best-effort: if Redis null or PASTE_, allows (local dev without Upstash still works). Returns 429 + RetryAfter 60 when exceeded. Like SDD rate limit table: 15 RPM Gemini but app enforces 3/min per user, 5 RPM total.
/// </summary>
public class AiRateLimiter : IAiRateLimiter
{
    private readonly IConnectionMultiplexer? _mux;
    private readonly IDatabase? _db;
    private readonly ILogger<AiRateLimiter> _logger;

    public AiRateLimiter(IConfiguration config, ILogger<AiRateLimiter> logger)
    {
        _logger = logger;
        var conn = config["Redis:Connection"] ?? config["Redis__Connection"] ?? "";
        if (string.IsNullOrWhiteSpace(conn) || conn.Contains("PASTE_"))
        {
            _logger.LogWarning("[AI RateLimiter] No Redis - rate limiting disabled (dev mode allows all)");
            return;
        }
        try
        {
            var opts = ConfigurationOptions.Parse(conn);
            opts.AbortOnConnectFail = false;
            opts.ConnectRetry = 3;
            _mux = ConnectionMultiplexer.Connect(opts);
            _db = _mux.GetDatabase();
            _logger.LogInformation("[AI RateLimiter] Connected");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AI RateLimiter] Connect failed - disabled");
        }
    }

    public async Task<(bool Allowed, int RetryAfterSeconds)> TryAcquireAsync(Guid userId, string model, CancellationToken ct = default)
    {
        if (_db == null) return (true, 0);

        try
        {
            var perUserKey = $"ai:{userId}:{model}";
            var globalKey = "ai:global";

            // INCR per-user
            var count = await _db.StringIncrementAsync(perUserKey);
            if (count == 1) await _db.KeyExpireAsync(perUserKey, TimeSpan.FromMinutes(1));

            if (count > 3)
            {
                var ttl = await _db.KeyTimeToLiveAsync(perUserKey);
                var retry = (int)(ttl?.TotalSeconds ?? 60);
                _logger.LogWarning("[AI RateLimiter] 429 per-user {UserId} {Model} count {Count}", userId, model, count);
                return (false, retry);
            }

            // Global 5 RPM
            var globalCount = await _db.StringIncrementAsync(globalKey);
            if (globalCount == 1) await _db.KeyExpireAsync(globalKey, TimeSpan.FromMinutes(1));
            if (globalCount > 5)
            {
                var ttl = await _db.KeyTimeToLiveAsync(globalKey);
                var retry = (int)(ttl?.TotalSeconds ?? 60);
                _logger.LogWarning("[AI RateLimiter] 429 global count {Count}", globalCount);
                return (false, retry);
            }

            return (true, 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AI RateLimiter] Error - allowing (best-effort)");
            return (true, 0);
        }
    }
}
