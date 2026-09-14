using SharedKernel;

namespace Identity.Service.Domain.Entities;

/// <summary>
/// Join table WorkspaceId+UserId with Role. Drives JWT claims and authorization checks.
/// </summary>
public class WorkspaceMember
{
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public int Role { get; set; } = Roles.MemberValue;
    public Guid? CustomRoleId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation (optional for EF)
    public Workspace? Workspace { get; set; }
    public User? User { get; set; }
    public OrganizationWorkspaceRole? CustomRole { get; set; }

    public WorkspaceMember() { }

    public WorkspaceMember(Guid workspaceId, Guid userId, int role, Guid? customRoleId = null)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        Role = role;
        CustomRoleId = customRoleId;
    }
}
