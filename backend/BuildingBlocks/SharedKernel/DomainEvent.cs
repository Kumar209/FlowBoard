namespace SharedKernel;

public abstract record DomainEvent(DateTime OccurredOnUtc)
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType => GetType().Name;
}
