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
        var result = await _service.PersistTaskCreatedAsync(msg.EventId, msg.ProjectId, msg.WorkspaceId, msg.TaskId, msg.Title, msg.ActorId, msg.OccurredOnUtc);
        if (!result.IsSuccess) _logger.LogWarning("[Consumer] TaskCreated persist failed {EventId} {Err}", msg.EventId, result.Error);
        else _logger.LogInformation("[Consumer] TaskCreated {TaskId} persisted {EventId}", msg.TaskId, msg.EventId);
        await _hub.Clients.Group($"workspace:{msg.WorkspaceId}").SendAsync("taskCreated", new { taskId = msg.TaskId, projectId = msg.ProjectId, workspaceId = msg.WorkspaceId, title = msg.Title, actorId = msg.ActorId });
        await _hub.Clients.Group($"project:{msg.ProjectId}").SendAsync("taskCreated", msg);
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
        await _service.PersistTaskMovedAsync(msg.EventId, msg.ProjectId, msg.WorkspaceId, msg.TaskId, msg.FromListId, msg.ToListId, msg.Position, msg.ActorId, msg.OccurredOnUtc);
        _logger.LogInformation("[Consumer] TaskMoved {TaskId} {From}->{To}", msg.TaskId, msg.FromListId, msg.ToListId);
        await _hub.Clients.Group($"workspace:{msg.WorkspaceId}").SendAsync("taskMoved", new { taskId = msg.TaskId, projectId = msg.ProjectId, workspaceId = msg.WorkspaceId, fromListId = msg.FromListId, toListId = msg.ToListId, position = msg.Position });
        await _hub.Clients.Group($"project:{msg.ProjectId}").SendAsync("taskMoved", msg);
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
        await _service.PersistTaskCommentedAsync(msg.EventId, msg.ProjectId, msg.WorkspaceId, msg.TaskId, msg.CommentId, msg.ActorId, msg.OccurredOnUtc);
        await _hub.Clients.Group($"workspace:{msg.WorkspaceId}").SendAsync("taskCommented", new { taskId = msg.TaskId, projectId = msg.ProjectId, workspaceId = msg.WorkspaceId, commentId = msg.CommentId });
        await _hub.Clients.Group($"project:{msg.ProjectId}").SendAsync("taskCommented", msg);
    }
}
