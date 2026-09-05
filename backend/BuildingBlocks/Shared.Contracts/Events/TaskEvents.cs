namespace Shared.Contracts.Events;

/// <summary>
/// Shared.Contracts - type-safe integration events published via MassTransit + CloudAMQP (same key local/prod) + Outbox (Project OutboxMessages). IIntegrationEvent guarantees EventId/OccurredOnUtc/CorrelationId for idempotency + tracing across microservices.
/// </summary>

/// <summary>
/// TaskCreated - fired when TaskItem created in Project. Consumers: Notification.Service persists Notification + SignalR group workspace:{id} sendAsync. Used in Task 3.1 Outbox.
/// </summary>
public record TaskCreatedEvent(
    Guid TaskId,
    Guid ProjectId,
    Guid ListId,
    string Title,
    Guid ActorId,
    DateTime OccurredOnUtc,
    Guid EventId,
    string CorrelationId) : IIntegrationEvent;

/// <summary>
/// TaskMoved - fired when TaskItem dragged between lists (CDK drag-drop). Carries FromListId/ToListId/Position. Consumer updates board cache (Upstash invalidation) + live sync.
/// </summary>
public record TaskMovedEvent(
    Guid TaskId,
    Guid ProjectId,
    Guid FromListId,
    Guid ToListId,
    int Position,
    Guid ActorId,
    DateTime OccurredOnUtc,
    Guid EventId,
    string CorrelationId) : IIntegrationEvent;

/// <summary>
/// TaskCommented - fired when Comment added to Task. Consumer sends Brevo notification + realtime SignalR. Payload includes CommentId.
/// </summary>
public record TaskCommentedEvent(
    Guid TaskId,
    Guid ProjectId,
    Guid CommentId,
    Guid ActorId,
    DateTime OccurredOnUtc,
    Guid EventId,
    string CorrelationId) : IIntegrationEvent;

/// <summary>
/// FileUploaded - fired after Cloudinary upload (File.Service). Carries AttachmentId, FileName, secure Url + thumb. Consumer logs activity + notifies.
/// </summary>
public record FileUploadedEvent(
    Guid AttachmentId,
    Guid TaskId,
    Guid UploaderId,
    string FileName,
    string Url,
    DateTime OccurredOnUtc,
    Guid EventId,
    string CorrelationId) : IIntegrationEvent;

/// <summary>
/// IIntegrationEvent - marker for MassTransit contracts. Ensures idempotent consumers (check EventId) + correlation tracing across Gateway -> Project -> Notification.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
    string CorrelationId { get; }
}
