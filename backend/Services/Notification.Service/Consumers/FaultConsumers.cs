using MassTransit;
using Shared.Contracts.Events;

namespace Notification.Service.Consumers;

/// <summary>
/// DLQ observability: MassTransit moves failed messages (after retries exhausted) to {queue}_error queues
/// (notification-task-created_error, notification-task-moved_error, notification-task-commented_error).
/// These Fault consumers log the dead-letter for MNC-grade observability.
/// Recommended: add CloudAMQP alarm on _error queue depth > 0.
/// </summary>
public class TaskCreatedFaultConsumer : IConsumer<Fault<TaskCreatedEvent>>
{
    private readonly ILogger<TaskCreatedFaultConsumer> _logger;
    public TaskCreatedFaultConsumer(ILogger<TaskCreatedFaultConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<Fault<TaskCreatedEvent>> ctx)
    {
        var ex = ctx.Message.Exceptions.FirstOrDefault()?.Message ?? "unknown";
        _logger.LogError("[DLQ] TaskCreated Fault EventId={EventId} TaskId={TaskId} Error={Error} FaultedAt={At}",
            ctx.Message.Message.EventId, ctx.Message.Message.TaskId, ex, DateTime.UtcNow);
        return Task.CompletedTask;
    }
}

public class TaskMovedFaultConsumer : IConsumer<Fault<TaskMovedEvent>>
{
    private readonly ILogger<TaskMovedFaultConsumer> _logger;
    public TaskMovedFaultConsumer(ILogger<TaskMovedFaultConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<Fault<TaskMovedEvent>> ctx)
    {
        var ex = ctx.Message.Exceptions.FirstOrDefault()?.Message ?? "unknown";
        _logger.LogError("[DLQ] TaskMoved Fault EventId={EventId} TaskId={TaskId} Error={Error}",
            ctx.Message.Message.EventId, ctx.Message.Message.TaskId, ex);
        return Task.CompletedTask;
    }
}

public class TaskCommentedFaultConsumer : IConsumer<Fault<TaskCommentedEvent>>
{
    private readonly ILogger<TaskCommentedFaultConsumer> _logger;
    public TaskCommentedFaultConsumer(ILogger<TaskCommentedFaultConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<Fault<TaskCommentedEvent>> ctx)
    {
        var ex = ctx.Message.Exceptions.FirstOrDefault()?.Message ?? "unknown";
        _logger.LogError("[DLQ] TaskCommented Fault EventId={EventId} TaskId={TaskId} Error={Error}",
            ctx.Message.Message.EventId, ctx.Message.Message.TaskId, ex);
        return Task.CompletedTask;
    }
}
