using Identity.Service.Domain.Enums;

namespace Identity.Service.Domain.Entities;

// Composite key: WorkspaceId + UserId, Role determines permissions (6 roles, PM can create projects)
public class WorkspaceMember
{
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public WorkspaceRole Role { get; set; } = WorkspaceRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation (optional for EF)
    public Workspace? Workspace { get; set; }
    public User? User { get; set; }

    public WorkspaceMember() { }

    public WorkspaceMember(Guid workspaceId, Guid userId, WorkspaceRole role)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        Role = role;
    }
}
