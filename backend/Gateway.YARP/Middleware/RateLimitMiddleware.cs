using System.Net;
using StackExchange.Redis;

namespace Gateway.YARP.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RateLimitMiddleware> _logger;
    // Limits sliding window counter
    private const int IpLimit = 60;
    private const int UserLimit = 100;
    private const int WindowSeconds = 60;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger, IConfiguration config)
    {
        _next = next;
        _logger = logger;
        var conn = config["Redis:Connection"] ?? config["Redis__Connection"] ?? config["Redis__Connection:Connection"] ?? "";
        if (string.IsNullOrWhiteSpace(conn) || conn.Contains("PASTE_"))
        {
            _logger.LogWarning("[RateLimit] No Redis - rate limiting disabled (dev mode)");
            _redis = null;
        }
        else
        {
            try
            {
                // Upstash uses full rediss:// url, StackExchange needs parsing via ConfigurationOptions
                if (conn.StartsWith("rediss://") || conn.StartsWith("redis://"))
                {
                    var opts = ConfigurationOptions.Parse(conn);
                    opts.AbortOnConnectFail = false;
                    opts.ConnectRetry = 3;
                    opts.ConnectTimeout = 5000;
                    _redis = ConnectionMultiplexer.Connect(opts);
                }
                else
                {
                    // Alternative format: host:port,password=...,ssl=True
                    _redis = ConnectionMultiplexer.Connect(conn);
                }
                _logger.LogInformation("[RateLimit] Connected to Redis for sliding window counter");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[RateLimit] Redis connect failed - disabled");
                _redis = null;
            }
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip health and docs
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/health") || path.StartsWith("/swagger") || path.StartsWith("/scalar") || path.StartsWith("/openapi"))
        {
            await _next(context);
            return;
        }

        if (_redis == null)
        {
            await _next(context);
            return;
        }

        var db = _redis.GetDatabase();
        var now = DateTimeOffset.UtcNow;
        var userId = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        string key;
        int limit;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            key = $"rl:user:{userId}";
            limit = UserLimit;
        }
        else
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? "unknown";
            // For local dev, ip may be ::1, normalize
            ip = ip.Replace("::1", "127.0.0.1");
            key = $"rl:ip:{ip}";
            limit = IpLimit;
        }

        var allowed = await IsAllowedAsync(db, key, limit, now);
        if (!allowed.allowed)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = allowed.retryAfter.ToString();
            context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            _logger.LogWarning("[RateLimit] 429 {Key} limit {Limit} retry {Retry}", key, limit, allowed.retryAfter);
            await context.Response.WriteAsJsonAsync(new { error = $"Too Many Requests - please try again after {allowed.retryAfter}s", retryAfter = allowed.retryAfter });
            return;
        }

        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = (limit - allowed.count).ToString();

        await _next(context);
    }

    private async Task<(bool allowed, int count, int retryAfter)> IsAllowedAsync(IDatabase db, string key, int limit, DateTimeOffset now)
    {
        try
        {
            // Sliding window counter: two minute buckets
            var window = TimeSpan.FromSeconds(WindowSeconds);
            var currentBucket = now.ToString("yyyyMMddHHmm");
            var prevBucket = now.AddMinutes(-1).ToString("yyyyMMddHHmm");
            var elapsedSeconds = now.Second + now.Millisecond / 1000.0;
            // Keys
            var curKey = $"{key}:{currentBucket}";
            var prevKey = $"{key}:{prevBucket}";

            var curCountTask = db.StringIncrementAsync(curKey);
            var prevCountTask = db.StringGetAsync(prevKey);
            // Ensure expire
            var curCount = await curCountTask;
            if (curCount == 1) await db.KeyExpireAsync(curKey, TimeSpan.FromSeconds(120));
            var prevCount = 0L;
            var prevVal = await prevCountTask;
            if (prevVal.HasValue && long.TryParse(prevVal.ToString(), out var pc)) prevCount = pc;

            // Weighted sliding count
            var slidingCount = prevCount * (1 - elapsedSeconds / 60.0) + curCount;
            var countInt = (int)Math.Ceiling(slidingCount);
            if (countInt > limit)
            {
                var retry = (int)Math.Ceiling(60 - elapsedSeconds);
                if (retry < 1) retry = 1;
                return (false, countInt, retry);
            }
            return (true, countInt, 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RateLimit] Error - allowing (best-effort)");
            return (true, 0, 0);
        }
    }
}
