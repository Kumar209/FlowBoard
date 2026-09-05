namespace SharedKernel;

/// <summary>
/// BaseEntity - DDD base for all aggregates/entities. Provides Id (Guid New), CreatedAt/UpdatedAt (UTC), DomainEvents collection for transactional outbox, Touch() to bump UpdatedAt, Add/ClearDomainEvents for publishing. Every Project file inherits this to get consistent auditing + event sourcing.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
    protected void Touch() => UpdatedAt = DateTime.UtcNow;
}
