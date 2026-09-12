using Serilog.Context;

namespace Gateway.YARP.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string HeaderName = "X-Correlation-Id";
    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var cid = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(cid))
        {
            cid = Guid.NewGuid().ToString();
            context.Request.Headers[HeaderName] = cid;
        }
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = cid!;
            return Task.CompletedTask;
        });
        context.Items[HeaderName] = cid!;
        // Enrich Serilog context - from JWT claims and route values without DB lookup
        var userId = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var workspaceId = context.Request.RouteValues["workspaceId"]?.ToString() ?? context.Request.RouteValues["wid"]?.ToString() ?? context.User.FindFirst("workspace_id")?.Value ?? "";
        var projectId = context.Request.RouteValues["projectId"]?.ToString() ?? context.Request.RouteValues["pid"]?.ToString() ?? "";
        // OrganizationId from JWT if present, else skip DB lookup
        var orgId = context.User.FindFirst("org_id")?.Value ?? context.User.FindFirst("OrganizationId")?.Value ?? "";

        using (LogContext.PushProperty("CorrelationId", cid!))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("OrganizationId", orgId))
        using (LogContext.PushProperty("WorkspaceId", workspaceId))
        using (LogContext.PushProperty("ProjectId", projectId))
        {
            await _next(context);
        }
    }
}
