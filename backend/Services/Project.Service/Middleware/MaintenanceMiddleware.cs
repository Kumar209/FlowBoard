using System.Text.Json;
using SharedKernel;

namespace Project.Service.Middleware;

public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MaintenanceMiddleware> _logger;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(2) };
    private static bool _cachedActive;
    private static DateTime _lastFetch = DateTime.MinValue;
    private static string _cachedPayload = "";

    public MaintenanceMiddleware(RequestDelegate next, ILogger<MaintenanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/health") || path.StartsWith("/swagger") || path.StartsWith("/scalar") || path.StartsWith("/openapi"))
        {
            await _next(context);
            return;
        }
        if (path.StartsWith("/api/superadmin"))
        {
            var roles = context.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value)
                .Concat(context.User.FindAll("role").Select(c => c.Value)).ToArray();
            if (Roles.IsSuperAdmin(roles)) { await _next(context); return; }
        }
        if (path.StartsWith("/api/platform/maintenance") || path.StartsWith("/api/platform/general"))
        {
            await _next(context);
            return;
        }

        try
        {
            if ((DateTime.UtcNow - _lastFetch).TotalSeconds > 300)
            {
                try
                {
                    var resp = await _http.GetAsync("http://localhost:5001/api/platform/maintenance");
                    if (resp.IsSuccessStatusCode)
                    {
                        var json = await resp.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        _cachedActive = doc.RootElement.TryGetProperty("isActive", out var a) && a.GetBoolean();
                        _cachedPayload = json;
                    }
                    _lastFetch = DateTime.UtcNow;
                }
                catch (Exception ex) { _logger.LogDebug(ex, "Maintenance fetch failed"); }
            }
            if (_cachedActive)
            {
                var retryAfter = 60;
                try
                {
                    using var doc = JsonDocument.Parse(_cachedPayload);
                    if (doc.RootElement.TryGetProperty("endAt", out var e) && DateTime.TryParse(e.GetString(), out var end))
                        retryAfter = Math.Max(60, (int)(end.ToUniversalTime() - DateTime.UtcNow).TotalSeconds);
                } catch { }
                context.Response.StatusCode = 503;
                context.Response.Headers["Retry-After"] = retryAfter.ToString();
                context.Response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(new { error = "Platform under maintenance — please try again later.", maintenance = new { retryAfter } });
                await context.Response.WriteAsync(payload);
                return;
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Maintenance check failed - allow request"); }

        await _next(context);
    }
}
