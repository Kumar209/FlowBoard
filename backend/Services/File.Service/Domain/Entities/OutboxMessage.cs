using SharedKernel;

namespace File.Service.Domain.Entities;

public class OutboxMessage : BaseEntity
{
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredOn { get; private set; }
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
