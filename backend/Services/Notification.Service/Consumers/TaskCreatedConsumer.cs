using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Notification.Service.Hubs;
using Notification.Service.Infrastructure.Persistence;
using Shared.Contracts.Events;
using NotificationEntity = Notification.Service.Domain.Entities.Notification;

namespace Notification.Service.Consumers;

public class TaskCreatedConsumer : IConsumer<TaskCreatedEvent>
{
    private readonly NotificationDbContext _db;
    private readonly IHubContext<BoardHub> _hub;
    private readonly ILogger<TaskCreatedConsumer> _logger;

    public TaskCreatedConsumer(NotificationDbContext db, IHubContext<BoardHub> hub, ILogger<TaskCreatedConsumer> logger)
    {
        _db = db;
        _hub = hub;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TaskCreatedEvent> ctx)
    {
        var msg = ctx.Message;
        // Idempotency check (EventId unique)
        if (await _db.Notifications.AnyAsync(n => n.EventId == msg.EventId))
        {
            _logger.LogInformation("[Consumer] TaskCreated duplicate {EventId} skipped", msg.EventId);
            return;
        }

        // Resolve workspaceId via ProjectId -> need to look up? Use ProjectId as workspace proxy or query Project.Service DB directly (same physical DB, different schema)
        // For now store ProjectId as WorkspaceId payload; consumer will fetch workspaceId via [project].Projects if available on same DB
        var workspaceId = await ResolveWorkspaceIdAsync(msg.ProjectId);
        var notif = new NotificationEntity(msg.EventId, msg.ProjectId, msg.TaskId, msg.ActorId, "TaskCreated", $"{{\"title\":\"{msg.Title}\"}}", workspaceId, msg.OccurredOnUtc);
        _db.Notifications.Add(notif);
        await _db.SaveChangesAsync();

        _logger.LogInformation("[Consumer] TaskCreated {TaskId} persisted {EventId}", msg.TaskId, msg.EventId);

        // Push via SignalR to workspace group
        await _hub.Clients.Group($"workspace:{workspaceId}").SendAsync("taskCreated", new { taskId = msg.TaskId, projectId = msg.ProjectId, title = msg.Title, actorId = msg.ActorId });
        await _hub.Clients.Group($"project:{msg.ProjectId}").SendAsync("taskCreated", msg);
    }

    private async Task<string> ResolveWorkspaceIdAsync(Guid projectId)
    {
        try
        {
            var wsId = await _db.Database.SqlQueryRaw<Guid>("SELECT WorkspaceId FROM [project].[Projects] WHERE Id = {0}", projectId).FirstOrDefaultAsync();
            return wsId == Guid.Empty ? projectId.ToString() : wsId.ToString();
        }
        catch { return projectId.ToString(); }
    }
}

public class TaskMovedConsumer : IConsumer<TaskMovedEvent>
{
    private readonly NotificationDbContext _db;
    private readonly IHubContext<BoardHub> _hub;
    private readonly ILogger<TaskMovedConsumer> _logger;

    public TaskMovedConsumer(NotificationDbContext db, IHubContext<BoardHub> hub, ILogger<TaskMovedConsumer> logger)
    {
        _db = db; _hub = hub; _logger = logger;
    }

    public async Task Consume(ConsumeContext<TaskMovedEvent> ctx)
    {
        var msg = ctx.Message;
        if (await _db.Notifications.AnyAsync(n => n.EventId == msg.EventId)) return;
        var workspaceId = await ResolveWorkspaceIdAsync(msg.ProjectId);
        var notif = new NotificationEntity(msg.EventId, msg.ProjectId, msg.TaskId, msg.ActorId, "TaskMoved", $"{{\"from\":\"{msg.FromListId}\",\"to\":\"{msg.ToListId}\"}}", workspaceId, msg.OccurredOnUtc);
        _db.Notifications.Add(notif);
        await _db.SaveChangesAsync();
        _logger.LogInformation("[Consumer] TaskMoved {TaskId} {From}->{To}", msg.TaskId, msg.FromListId, msg.ToListId);
        await _hub.Clients.Group($"workspace:{workspaceId}").SendAsync("taskMoved", new { taskId = msg.TaskId, projectId = msg.ProjectId, fromListId = msg.FromListId, toListId = msg.ToListId, position = msg.Position });
        await _hub.Clients.Group($"project:{msg.ProjectId}").SendAsync("taskMoved", msg);
    }

    private async Task<string> ResolveWorkspaceIdAsync(Guid projectId)
    {
        try
        {
            var wsId = await _db.Database.SqlQueryRaw<Guid>("SELECT WorkspaceId FROM [project].[Projects] WHERE Id = {0}", projectId).FirstOrDefaultAsync();
            return wsId == Guid.Empty ? projectId.ToString() : wsId.ToString();
        }
        catch { return projectId.ToString(); }
    }
}

public class TaskCommentedConsumer : IConsumer<TaskCommentedEvent>
{
    private readonly NotificationDbContext _db;
    private readonly IHubContext<BoardHub> _hub;
    private readonly ILogger<TaskCommentedConsumer> _logger;

    public TaskCommentedConsumer(NotificationDbContext db, IHubContext<BoardHub> hub, ILogger<TaskCommentedConsumer> logger)
    {
        _db = db; _hub = hub; _logger = logger;
    }

    public async Task Consume(ConsumeContext<TaskCommentedEvent> ctx)
    {
        var msg = ctx.Message;
        if (await _db.Notifications.AnyAsync(n => n.EventId == msg.EventId)) return;
        var workspaceId = await ResolveWorkspaceIdAsync(msg.ProjectId);
        var notif = new NotificationEntity(msg.EventId, msg.ProjectId, msg.TaskId, msg.ActorId, "TaskCommented", $"{{\"commentId\":\"{msg.CommentId}\"}}", workspaceId, msg.OccurredOnUtc);
        _db.Notifications.Add(notif);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group($"workspace:{workspaceId}").SendAsync("taskCommented", new { taskId = msg.TaskId, projectId = msg.ProjectId, commentId = msg.CommentId });
        await _hub.Clients.Group($"project:{msg.ProjectId}").SendAsync("taskCommented", msg);
    }

    private async Task<string> ResolveWorkspaceIdAsync(Guid projectId)
    {
        try
        {
            var wsId = await _db.Database.SqlQueryRaw<Guid>("SELECT WorkspaceId FROM [project].[Projects] WHERE Id = {0}", projectId).FirstOrDefaultAsync();
            return wsId == Guid.Empty ? projectId.ToString() : wsId.ToString();
        }
        catch { return projectId.ToString(); }
    }
}
