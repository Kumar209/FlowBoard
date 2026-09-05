using SharedKernel;

namespace Project.Service.Domain.Entities;

/// <summary>
/// OutboxMessage - transactional outbox pattern for reliable MassTransit publishing (Task 3.1). Stores integration events (TaskCreated/Moved/Commented) as JSON Payload with Type, OccurredOn, ProcessedAt, Error. Saved in same DB transaction as domain changes, then BackgroundService polls every 2s to publish to CloudAMQP (same key local/prod) and marks ProcessedAt. Prevents lost events on crash/restart, retry 3x + _error queue.
/// </summary>
public class OutboxMessage : BaseEntity
{
    public string Type { get; private set; } = string.Empty; // e.g., TaskCreatedEvent
    public string Payload { get; private set; } = string.Empty; // JSON
    public DateTime OccurredOn { get; private set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string type, string payload)
    {
        Type = type;
        Payload = payload;
        OccurredOn = DateTime.UtcNow;
    }

    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;
    public void MarkFailed(string error) => Error = error;
}
