namespace SharedKernel;

// IAggregateRoot - Marker interface (empty, no methods) to mark aggregate roots (e.g., User, Organization, Project)
// Only aggregate roots are directly loaded/saved via repositories (e.g., IRepository<Project> where T : IAggregateRoot)
// Child entities (e.g., Comment, Task inside Project) are accessed only via their aggregate root, not directly
// This enforces DDD boundary: prevents fetching orphans and keeps business invariants inside the aggregate
public interface IAggregateRoot
{
}
