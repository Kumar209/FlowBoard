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
            await _hub.Clients.Group($"workspace:{msg.WorkspaceId}").SendAsync("taskCreated", new { taskId = msg.TaskId, projectId = msg.ProjectId, workspaceId = msg.WorkspaceId, title = msg.Title, actorId = msg.ActorId });
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
        var result = await _service.PersistTaskMovedAsync(msg.EventId, msg.ProjectId, msg.WorkspaceId, msg.TaskId, msg.FromListId, msg.FromListName ?? "", msg.ToListId, msg.ToListName ?? "", msg.BoardName ?? "", msg.SprintName ?? "", msg.TaskTitle ?? "", msg.ProjectName ?? "", msg.ActorName ?? "", msg.ActorRole ?? "", msg.Position, msg.ActorId, msg.RecipientUserIds ?? new List<Guid>(), msg.OccurredOnUtc);
        if (!result.IsSuccess) _logger.LogWarning("[Consumer] TaskMoved persist failed {EventId} {Err}", msg.EventId, result.Error);
        else _logger.LogInformation("[Consumer] TaskMoved {TaskId} {From}->{To}", msg.TaskId, msg.FromListId, msg.ToListId);
        try
        {
            await _hub.Clients.Group($"project:{msg.ProjectId}").SendAsync("taskMoved", new { taskId = msg.TaskId, projectId = msg.ProjectId, workspaceId = msg.WorkspaceId, fromListId = msg.FromListId, toListId = msg.ToListId, position = msg.Position });
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
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[Consumer] SignalR publish failed for taskCommented — local only"); }
    }
}
