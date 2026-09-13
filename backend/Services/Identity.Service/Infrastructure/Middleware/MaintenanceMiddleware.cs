using System.Text.Json;
using Identity.Service.Application.Interfaces;
using SharedKernel;

namespace Identity.Service.Infrastructure.Middleware;

public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MaintenanceMiddleware> _logger;

    public MaintenanceMiddleware(RequestDelegate next, ILogger<MaintenanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider sp)
    {
        // Skip health, swagger, scalar, platform public
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/health") || path.StartsWith("/swagger") || path.StartsWith("/scalar") || path.StartsWith("/openapi"))
        {
            await _next(context);
            return;
        }
        // Allow superadmin to manage settings even during maintenance
        if (path.StartsWith("/api/superadmin"))
        {
            // Check if user is superadmin - let them through to disable maintenance
            var roles = context.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value)
                .Concat(context.User.FindAll("role").Select(c => c.Value)).ToArray();
            if (Roles.IsSuperAdmin(roles))
            {
                await _next(context);
                return;
            }
        }
        // Allow public platform maintenance status and login/register preflight? No - block login/register when active
        if (path.StartsWith("/api/platform/maintenance") || path.StartsWith("/api/platform/general"))
        {
            await _next(context);
            return;
        }

        try
        {
            using var scope = sp.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IPlatformSettingsService>();
            if (await svc.IsMaintenanceActiveAsync())
            {
                var m = await svc.GetMaintenanceAsync();
                var retryAfter = 60;
                if (DateTime.TryParse(m.EndAt, out var end)) retryAfter = Math.Max(60, (int)(end.ToUniversalTime() - DateTime.UtcNow).TotalSeconds);
                context.Response.StatusCode = 503;
                context.Response.Headers["Retry-After"] = retryAfter.ToString();
                context.Response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(new { error = "Platform under maintenance — please try again later.", maintenance = new { scheduledAt = m.ScheduledAt, endAt = m.EndAt, announcement = m.Announcement, retryAfter } });
                await context.Response.WriteAsync(payload);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Maintenance check failed - allow request");
        }

        await _next(context);
    }
}
