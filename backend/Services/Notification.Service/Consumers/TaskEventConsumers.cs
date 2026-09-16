using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Notification.Service.Application.Interfaces;
using Notification.Service.Hubs;
using Shared.Contracts.Events;

namespace Notification.Service.Consumers;

public class TaskCreatedConsumer : IConsumer<TaskCreatedEvent>
{
    private readonly INotificationService _service;
    private readonly IHubContext<BoardHub> _hub;
    private readonly ILogger<TaskCreatedConsumer> _logger;

    public TaskCreatedConsumer(INotificationService service, IHubContext<BoardHub> hub, ILogger<TaskCreatedConsumer> logger)
    {
        _service = service; _hub = hub; _logger = logger;
    }

    public async Task Consume(ConsumeContext<TaskCreatedEvent> ctx)
    {
        var msg = ctx.Message;
        var result = await _service.PersistTaskCreatedAsync(msg.EventId, msg.ProjectId, msg.WorkspaceId, msg.TaskId, msg.Title, msg.ActorId, msg.RecipientUserIds ?? new List<Guid>(), msg.OccurredOnUtc);
        if (!result.IsSuccess) _logger.LogWarning("[Consumer] TaskCreated persist failed {EventId} {Err}", msg.EventId, result.Error);
        else _logger.LogInformation("[Consumer] TaskCreated {TaskId} persisted {EventId}", msg.TaskId, msg.EventId);
        try
        {
            // Board refresh for project members
            await _hub.Clients.Group($"project:{msg.ProjectId}").SendAsync("taskCreated", new { taskId = msg.TaskId, projectId = msg.ProjectId, workspaceId = msg.WorkspaceId, title = msg.Title, actorId = msg.ActorId });
            // Personal bell for each recipient
            foreach (var uid in msg.RecipientUserIds ?? new List<Guid>())
                await _hub.Clients.Group($"user:{uid}").SendAsync("notification", new { id = msg.EventId, eventId = msg.EventId, action = "TaskCreated", projectId = msg.ProjectId, taskId = msg.TaskId });
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[Consumer] SignalR publish failed for taskCreated {TaskId} — Redis backplane timeout, local delivery only", msg.TaskId); }
    }
}

public class TaskMovedConsumer : IConsumer<TaskMovedEvent>
{
    private readonly INotificationService _service;
    private readonly IHubContext<BoardHub> _hub;
    private readonly ILogger<TaskMovedConsumer> _logger;

    public TaskMovedConsumer(INotificationService service, IHubContext<BoardHub> hub, ILogger<TaskMovedConsumer> logger)
    {
        _service = service; _hub = hub; _logger = logger;
    }

    public async Task Consume(ConsumeContext<TaskMovedEvent> ctx)
    {
        var msg = ctx.Message;
        var result = await _service.PersistTaskMovedAsync(msg.EventId, msg.ProjectId, msg.WorkspaceId, msg.TaskId, msg.FromListId ?? Guid.Empty, msg.FromListName ?? "", msg.ToListId ?? Guid.Empty, msg.ToListName ?? "", msg.BoardName ?? "", msg.SprintName ?? "", msg.TaskTitle ?? "", msg.ProjectName ?? "", msg.ActorName ?? "", msg.ActorRole ?? "", msg.Position, msg.ActorId, msg.RecipientUserIds ?? new List<Guid>(), msg.OccurredOnUtc);
        if (!result.IsSuccess) _logger.LogWarning("[Consumer] TaskMoved persist failed {EventId} {Err}", msg.EventId, result.Error);
        else _logger.LogInformation("[Consumer] TaskMoved {TaskId} {From}->{To}", msg.TaskId, msg.FromListId, msg.ToListId);
        try
        {
            await _hub.Clients.Group($"project:{msg.ProjectId}").SendAsync("taskMoved", new { taskId = msg.TaskId, projectId = msg.ProjectId, workspaceId = msg.WorkspaceId, fromListId = msg.FromListId, toListId = msg.ToListId, position = msg.Position });
            foreach (var uid in msg.RecipientUserIds ?? new List<Guid>())
                await _hub.Clients.Group($"user:{uid}").SendAsync("notification", new { id = msg.EventId, eventId = msg.EventId, action = "TaskMoved", projectId = msg.ProjectId, taskId = msg.TaskId });
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[Consumer] SignalR publish failed for taskMoved — local only"); }
    }
}

public class TaskCommentedConsumer : IConsumer<TaskCommentedEvent>
{
    private readonly INotificationService _service;
    private readonly IHubContext<BoardHub> _hub;
    private readonly ILogger<TaskCommentedConsumer> _logger;

    public TaskCommentedConsumer(INotificationService service, IHubContext<BoardHub> hub, ILogger<TaskCommentedConsumer> logger)
    {
        _service = service; _hub = hub; _logger = logger;
    }

    public async Task Consume(ConsumeContext<TaskCommentedEvent> ctx)
    {
        var msg = ctx.Message;
        var result = await _service.PersistTaskCommentedAsync(msg.EventId, msg.ProjectId, msg.WorkspaceId, msg.TaskId, msg.CommentId, msg.ActorId, msg.RecipientUserIds ?? new List<Guid>(), msg.OccurredOnUtc);
        if (!result.IsSuccess) _logger.LogWarning("[Consumer] TaskCommented persist failed {EventId} {Err}", msg.EventId, result.Error);
        try
        {
            await _hub.Clients.Group($"project:{msg.ProjectId}").SendAsync("taskCommented", new { taskId = msg.TaskId, projectId = msg.ProjectId, workspaceId = msg.WorkspaceId, commentId = msg.CommentId });
            foreach (var uid in msg.RecipientUserIds ?? new List<Guid>())
                await _hub.Clients.Group($"user:{uid}").SendAsync("notification", new { id = msg.EventId, eventId = msg.EventId, action = "TaskCommented", projectId = msg.ProjectId, taskId = msg.TaskId });
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[Consumer] SignalR publish failed for taskCommented — local only"); }
    }
}

public class TaskAssignedConsumer : IConsumer<TaskAssignedEvent>
{
    private readonly INotificationService _service;
    private readonly IHubContext<BoardHub> _hub;
    private readonly ILogger<TaskAssignedConsumer> _logger;
    public TaskAssignedConsumer(INotificationService service, IHubContext<BoardHub> hub, ILogger<TaskAssignedConsumer> logger) { _service = service; _hub = hub; _logger = logger; }
    public async Task Consume(ConsumeContext<TaskAssignedEvent> ctx)
    {
        var msg = ctx.Message;
        var result = await _service.PersistTaskAssignedAsync(msg.EventId, msg.ProjectId, msg.WorkspaceId, msg.TaskId, msg.AssigneeId, msg.ActorId, msg.RecipientUserIds ?? new List<Guid>(), msg.OccurredOnUtc);
        if (!result.IsSuccess) _logger.LogWarning("[Consumer] TaskAssigned persist failed {EventId} {Err}", msg.EventId, result.Error);
        try
        {
            await _hub.Clients.Group($"project:{msg.ProjectId}").SendAsync("taskAssigned", new { taskId = msg.TaskId, projectId = msg.ProjectId, assigneeId = msg.AssigneeId });
            foreach (var uid in msg.RecipientUserIds ?? new List<Guid>())
                await _hub.Clients.Group($"user:{uid}").SendAsync("notification", new { id = msg.EventId, eventId = msg.EventId, action = "TaskAssigned", projectId = msg.ProjectId, taskId = msg.TaskId });
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[Consumer] SignalR publish failed for taskAssigned"); }
    }
}

public class TaskDeletedConsumer : IConsumer<TaskDeletedEvent>
{
    private readonly INotificationService _service;
    private readonly IHubContext<BoardHub> _hub;
    private readonly ILogger<TaskDeletedConsumer> _logger;
    public TaskDeletedConsumer(INotificationService service, IHubContext<BoardHub> hub, ILogger<TaskDeletedConsumer> logger) { _service = service; _hub = hub; _logger = logger; }
    public async Task Consume(ConsumeContext<TaskDeletedEvent> ctx)
    {
        var msg = ctx.Message;
        var result = await _service.PersistTaskDeletedAsync(msg.EventId, msg.ProjectId, msg.WorkspaceId, msg.TaskId, msg.TaskTitle, msg.ActorId, msg.RecipientUserIds ?? new List<Guid>(), msg.OccurredOnUtc);
        if (!result.IsSuccess) _logger.LogWarning("[Consumer] TaskDeleted persist failed {EventId} {Err}", msg.EventId, result.Error);
        try
        {
            await _hub.Clients.Group($"project:{msg.ProjectId}").SendAsync("taskDeleted", new { taskId = msg.TaskId, projectId = msg.ProjectId });
            foreach (var uid in msg.RecipientUserIds ?? new List<Guid>())
                await _hub.Clients.Group($"user:{uid}").SendAsync("notification", new { id = msg.EventId, eventId = msg.EventId, action = "TaskDeleted", projectId = msg.ProjectId, taskId = msg.TaskId });
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[Consumer] SignalR publish failed for taskDeleted"); }
    }
}

public class ProjectMemberAddedConsumer : IConsumer<ProjectMemberAddedEvent>
{
    private readonly INotificationService _service;
    private readonly IHubContext<BoardHub> _hub;
    private readonly ILogger<ProjectMemberAddedConsumer> _logger;
    public ProjectMemberAddedConsumer(INotificationService service, IHubContext<BoardHub> hub, ILogger<ProjectMemberAddedConsumer> logger) { _service = service; _hub = hub; _logger = logger; }
    public async Task Consume(ConsumeContext<ProjectMemberAddedEvent> ctx)
    {
        var msg = ctx.Message;
        var result = await _service.PersistProjectMemberAddedAsync(msg.EventId, msg.ProjectId, msg.WorkspaceId, msg.UserId, msg.ActorId, msg.RecipientUserIds ?? new List<Guid>(), msg.OccurredOnUtc);
        if (!result.IsSuccess) _logger.LogWarning("[Consumer] ProjectMemberAdded persist failed {EventId} {Err}", msg.EventId, result.Error);
        try
        {
            foreach (var uid in msg.RecipientUserIds ?? new List<Guid>())
                await _hub.Clients.Group($"user:{uid}").SendAsync("notification", new { id = msg.EventId, eventId = msg.EventId, action = "ProjectMemberAdded", projectId = msg.ProjectId });
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[Consumer] SignalR publish failed for projectMemberAdded"); }
    }
}

public class ComplaintCreatedConsumer : IConsumer<ComplaintCreatedEvent>
{
    private readonly INotificationService _service;
    private readonly IHubContext<BoardHub> _hub;
    private readonly ILogger<ComplaintCreatedConsumer> _logger;
    public ComplaintCreatedConsumer(INotificationService service, IHubContext<BoardHub> hub, ILogger<ComplaintCreatedConsumer> logger) { _service = service; _hub = hub; _logger = logger; }
    public async Task Consume(ConsumeContext<ComplaintCreatedEvent> ctx)
    {
        var msg = ctx.Message;
        var result = await _service.PersistComplaintCreatedAsync(msg.EventId, msg.OrganizationId, msg.ComplaintId, msg.Subject, msg.ActorId, msg.RecipientUserIds ?? new List<Guid>(), msg.OccurredOnUtc);
        if (!result.IsSuccess) _logger.LogWarning("[Consumer] ComplaintCreated persist failed {EventId} {Err}", msg.EventId, result.Error);
        try
        {
            foreach (var uid in msg.RecipientUserIds ?? new List<Guid>())
                await _hub.Clients.Group($"user:{uid}").SendAsync("notification", new { id = msg.EventId, eventId = msg.EventId, action = "ComplaintCreated", organizationId = msg.OrganizationId, complaintId = msg.ComplaintId });
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[Consumer] SignalR publish failed for complaintCreated"); }
    }
}
