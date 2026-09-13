using System.Net;
using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.YARP.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private static int _cachedIpLimit = 200;
    private static int _cachedUserLimit = 300;
    private static DateTime _lastFetch = DateTime.MinValue;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(2) };
    private const int WindowSeconds = 60;
    // Lua script atomic sliding window counter: KEYS[1]=curKey, KEYS[2]=prevKey, ARGV[1]=limit, ARGV[2]=elapsedSeconds
    private const string LuaScript = @"
local cur = tonumber(redis.call('GET', KEYS[1]) or '0')
local prev = tonumber(redis.call('GET', KEYS[2]) or '0')
local weight = 1 - (tonumber(ARGV[2]) / 60)
if weight < 0 then weight = 0 end
if weight > 1 then weight = 1 end
local sliding = prev * weight + cur
if sliding + 1 > tonumber(ARGV[1]) then
  local retry = math.ceil(60 - tonumber(ARGV[2]))
  if retry < 1 then retry = 1 end
  return {0, sliding, retry}
end
local newCur = redis.call('INCR', KEYS[1])
if newCur == 1 then redis.call('EXPIRE', KEYS[1], 120) end
return {1, sliding + 1, 0}
";

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger, IServiceProvider sp)
    {
        _next = next;
        _logger = logger;
        _redis = sp.GetService<IConnectionMultiplexer>();
        if (_redis == null) _logger.LogWarning("[RateLimit] No Redis - rate limiting disabled (dev mode)");
        else _logger.LogInformation("[RateLimit] Redis sliding window counter ready (DI)");
    }

    public async Task InvokeAsync(HttpContext context)
    {
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
        var elapsed = now.Second + now.Millisecond / 1000.0;
        var bucket = now.ToUnixTimeSeconds() / WindowSeconds;
        var prevBucket = bucket - 1;

        await RefreshLimitsIfNeededAsync();

        string key;
        int limit;
        var userId = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            key = $"rl:user:{userId}";
            limit = _cachedUserLimit;
        }
        else
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            ip = ip.Replace("::1", "127.0.0.1");
            key = $"rl:ip:{ip}";
            limit = _cachedIpLimit;
        }

        var curKey = $"{key}:{bucket}";
        var prevKey = $"{key}:{prevBucket}";

        var result = await TryAcquireAsync(db, curKey, prevKey, limit, elapsed);
        if (!result.allowed)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = result.retryAfter.ToString();
            context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            _logger.LogWarning("[RateLimit] 429 {Key} limit {Limit} count {Count} retry {Retry}", key, limit, result.count, result.retryAfter);
            await context.Response.WriteAsJsonAsync(new { error = $"Too Many Requests - please try again after {result.retryAfter}s", retryAfter = result.retryAfter });
            return;
        }

        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = (limit - result.count).ToString();
        await _next(context);
    }

    private async Task<(bool allowed, int count, int retryAfter)> TryAcquireAsync(IDatabase db, string curKey, string prevKey, int limit, double elapsed)
    {
        try
        {
            var res = (RedisResult[])await db.ScriptEvaluateAsync(LuaScript, new RedisKey[] { curKey, prevKey }, new RedisValue[] { limit, elapsed });
            var allowed = (int)res[0] == 1;
            var count = (int)(double)res[1];
            var retry = (int)res[2];
            return (allowed, count, retry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RateLimit] Lua error - allowing (best-effort)");
            return (true, 0, 0);
        }
    }

    private async Task RefreshLimitsIfNeededAsync()
    {
        if ((DateTime.UtcNow - _lastFetch).TotalSeconds < 30) return;
        try
        {
            var resp = await _http.GetAsync("http://localhost:5001/api/platform/rate-limits");
            if (!resp.IsSuccessStatusCode) return;
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("apiRequestsPerMinute", out var api)) _cachedIpLimit = api.GetInt32();
            if (doc.RootElement.TryGetProperty("apiRequestsPerMinute", out var api2)) _cachedUserLimit = api2.GetInt32(); // use same for now, gateway user limit = api
            // Try separate gatewayUser if exists
            if (doc.RootElement.TryGetProperty("gatewayUser", out var gu)) _cachedUserLimit = gu.GetInt32();
            _lastFetch = DateTime.UtcNow;
        }
        catch (Exception ex) { _logger.LogDebug(ex, "[RateLimit] fetch dynamic limits failed - use cached 200/300"); }
    }
}
