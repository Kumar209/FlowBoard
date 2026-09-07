using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
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
    public BoardHub(ILogger<BoardHub> logger) => _logger = logger;

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
        if (Guid.TryParse(projectId, out _))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"project:{projectId}");
            await Clients.Caller.SendAsync("joinedProject", projectId);
        }
    }

    public async Task LeaveProject(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project:{projectId}");
    }

    public async Task JoinWorkspace(string workspaceId)
    {
        if (Guid.TryParse(workspaceId, out _))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace:{workspaceId}");
            await Clients.Caller.SendAsync("joinedWorkspace", workspaceId);
        }
    }

    // Typing indicator (for Task 3.3 realtime)
    public async Task UserTyping(string projectId, string taskId)
    {
        var name = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? Context.UserIdentifier ?? "Someone";
        await Clients.Group($"project:{projectId}").SendAsync("userTyping", new { projectId, taskId, user = name, connectionId = Context.ConnectionId });
    }

    public async Task UserOnline(string workspaceId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await Clients.Group($"workspace:{workspaceId}").SendAsync("userOnline", new { workspaceId, userId });
    }
}
