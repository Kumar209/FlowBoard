using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Notification.Service.Application.Interfaces;
using System.Security.Claims;

namespace Notification.Service.Hubs;

/// <summary>
/// BoardHub - SignalR 10.0 Hub for realtime board sync. Groups per workspace:{id} and project:{id} (Task 3.2).
/// OnConnected validates JWT via ?access_token (Program.cs JwtBearer OnMessageReceived for /hubs), adds to groups from claims.
/// </summary>
[Authorize]
public class BoardHub : Hub
{
    private readonly ILogger<BoardHub> _logger;
    private readonly IApplicationDbContext _db;
    public BoardHub(ILogger<BoardHub> logger, IApplicationDbContext db) { _logger = logger; _db = db; }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User?.FindFirst("sub")?.Value;
        var workspaces = Context.User?.FindAll("workspace_id").Select(c => c.Value).ToList() ?? new List<string>();
        // Also try Role claims to support 6 roles grouping if needed (future)
        var roles = Context.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        _logger.LogInformation("[BoardHub] Connected {ConnId} user {UserId} workspaces {Ws}", Context.ConnectionId, userId, string.Join(",", workspaces));

        // Add to workspace groups (MNC-grade: one connection belongs to multiple workspaces)
        foreach (var ws in workspaces.Distinct())
        {
            if (!string.IsNullOrWhiteSpace(ws))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace:{ws}");
                _logger.LogInformation("[BoardHub] {ConnId} joined workspace:{Ws}", Context.ConnectionId, ws);
            }
        }

        // Also allow client to explicitly join project group via JoinProject
        await base.OnConnectedAsync();
        await Clients.Caller.SendAsync("connected", new { connectionId = Context.ConnectionId, userId });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("[BoardHub] Disconnected {ConnId} {Ex}", Context.ConnectionId, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinProject(string projectId)
    {
        if (!Guid.TryParse(projectId, out var pid)) return;
        var allowedWorkspaces = Context.User?.FindAll("workspace_id").Select(c => c.Value).ToHashSet() ?? new HashSet<string>();
        if (allowedWorkspaces.Count == 0)
        {
            _logger.LogWarning("[BoardHub] Denied JoinProject {Pid} for {ConnId} — no workspace claims", projectId, Context.ConnectionId);
            await Clients.Caller.SendAsync("joinDenied", new { projectId, reason = "No workspace membership" });
            return;
        }
        // Verify project belongs to one of user's workspaces via [project].[Projects] lookup
        try
        {
            var row = await _db.Database.SqlQueryRaw<ProjectWorkspaceRow>("SELECT WorkspaceId FROM [project].[Projects] WHERE Id = {0}", pid).FirstOrDefaultAsync();
            if (row == null)
            {
                _logger.LogWarning("[BoardHub] Denied JoinProject {Pid} — project not found", projectId);
                await Clients.Caller.SendAsync("joinDenied", new { projectId, reason = "Project not found" });
                return;
            }
            if (!allowedWorkspaces.Contains(row.WorkspaceId.ToString()))
            {
                _logger.LogWarning("[BoardHub] Denied JoinProject {Pid} workspace {Ws} for {ConnId} — not in JWT workspaces", projectId, row.WorkspaceId, Context.ConnectionId);
                await Clients.Caller.SendAsync("joinDenied", new { projectId, reason = "Not a member of project's workspace" });
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[BoardHub] JoinProject DB check failed for {Pid}, denying", projectId);
            await Clients.Caller.SendAsync("joinDenied", new { projectId, reason = "Verification failed" });
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project:{projectId}");
        await Clients.Caller.SendAsync("joinedProject", projectId);
    }
    private class ProjectWorkspaceRow { public Guid WorkspaceId { get; set; } }

    public async Task LeaveProject(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project:{projectId}");
    }

    public async Task JoinWorkspace(string workspaceId)
    {
        if (!Guid.TryParse(workspaceId, out _)) return;
        var allowedWorkspaces = Context.User?.FindAll("workspace_id").Select(c => c.Value).ToHashSet() ?? new HashSet<string>();
        if (!allowedWorkspaces.Contains(workspaceId))
        {
            _logger.LogWarning("[BoardHub] Denied JoinWorkspace {Ws} for {ConnId} — not in JWT workspaces", workspaceId, Context.ConnectionId);
            await Clients.Caller.SendAsync("joinDenied", new { workspaceId, reason = "Not a member of workspace" });
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace:{workspaceId}");
        await Clients.Caller.SendAsync("joinedWorkspace", workspaceId);
    }

    // Typing indicator (for Task 3.3 realtime) — also requires project membership
    public async Task UserTyping(string projectId, string taskId)
    {
        if (!Guid.TryParse(projectId, out var pid)) return;
        var allowedWorkspaces = Context.User?.FindAll("workspace_id").Select(c => c.Value).ToHashSet() ?? new HashSet<string>();
        try
        {
            var row = await _db.Database.SqlQueryRaw<ProjectWorkspaceRow>("SELECT WorkspaceId FROM [project].[Projects] WHERE Id = {0}", pid).FirstOrDefaultAsync();
            if (row == null || !allowedWorkspaces.Contains(row.WorkspaceId.ToString())) return;
        }
        catch { return; }
        var name = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? Context.UserIdentifier ?? "Someone";
        await Clients.Group($"project:{projectId}").SendAsync("userTyping", new { projectId, taskId, user = name, connectionId = Context.ConnectionId });
    }

    public async Task UserOnline(string workspaceId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await Clients.Group($"workspace:{workspaceId}").SendAsync("userOnline", new { workspaceId, userId });
    }
}
