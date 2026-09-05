namespace SharedKernel;

/// <summary>
/// IAggregateRoot - marker interface (no methods) to flag DDD aggregate roots (User, Organization, Project, TaskItem). Only aggregates are loaded/saved via repositories (IRepository<T> where T: IAggregateRoot); child entities (Comment, SubTask) are accessed via parent. Enforces DDD boundary and keeps invariants inside aggregate.
/// </summary>
public interface IAggregateRoot
{
}
