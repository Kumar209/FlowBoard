namespace SharedKernel;

/// <summary>
/// DomainEvent - base record for in-process domain events (e.g., UserRegistered, TaskCreated). Carries EventId (new Guid) + EventType (GetType().Name) + OccurredOnUtc. Added via BaseEntity.AddDomainEvent() and dispatched after SaveChanges. Sub-records live in each aggregate.
/// </summary>
public abstract record DomainEvent(DateTime OccurredOnUtc)
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType => GetType().Name;
}
