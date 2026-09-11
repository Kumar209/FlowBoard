using SharedKernel;

namespace Project.Service.Domain.Entities;

public class ProjectMember : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = Roles.Member;
    public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;
    public Project? Project { get; private set; }

    private ProjectMember() { }

    public ProjectMember(Guid projectId, Guid userId, string role = Roles.Member)
    {
        ProjectId = projectId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
    }

    public void UpdateRole(string role) => Role = role;
}
