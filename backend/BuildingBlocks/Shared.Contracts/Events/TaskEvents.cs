namespace Shared.Contracts.Events;

public record TaskCreatedEvent(
    Guid TaskId,
    Guid ProjectId,
    Guid ListId,
    string Title,
    Guid ActorId,
    DateTime OccurredOnUtc,
    Guid EventId,
    string CorrelationId) : IIntegrationEvent;

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

public record TaskCommentedEvent(
    Guid TaskId,
    Guid ProjectId,
    Guid CommentId,
    Guid ActorId,
    DateTime OccurredOnUtc,
    Guid EventId,
    string CorrelationId) : IIntegrationEvent;

public record FileUploadedEvent(
    Guid AttachmentId,
    Guid TaskId,
    Guid UploaderId,
    string FileName,
    string Url,
    DateTime OccurredOnUtc,
    Guid EventId,
    string CorrelationId) : IIntegrationEvent;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
    string CorrelationId { get; }
}
