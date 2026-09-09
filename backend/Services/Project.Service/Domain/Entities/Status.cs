using SharedKernel;

namespace Project.Service.Domain.Entities;

/// <summary>
/// Status - project-level workflow status (To Do, In Progress, In Review, Done).
/// Created explicitly via Project Settings → Statuses (no auto). Used by Issues (StatusId) and BoardColumns via BoardColumnStatus mapping.
/// </summary>
public class Status : BaseEntity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private Status() { }

    public Status(Guid projectId, string name)
    {
        ProjectId = projectId;
        Name = name;
    }

    public void Rename(string name) { Name = name; Touch(); }
}
