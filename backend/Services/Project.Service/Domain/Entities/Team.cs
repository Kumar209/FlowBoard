using SharedKernel;

namespace Project.Service.Domain.Entities;

/// <summary>
/// Team - Project-scoped team (Development, QA, Support, DevOps). Project → Teams → TeamMembers.
/// Issue (TaskItem) has optional TeamId to indicate responsible team (per workflow Team is property of Issue, Board filter uses Team).
/// </summary>
public class Team : BaseEntity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public Project? Project { get; private set; }
    public ICollection<TeamMember> Members { get; private set; } = new List<TeamMember>();

    private Team() { }

    public Team(Guid projectId, string name, string? description = null)
    {
        ProjectId = projectId;
        Name = name;
        Description = description;
    }

    public void Update(string name, string? description) { Name = name; Description = description; Touch(); }
}
