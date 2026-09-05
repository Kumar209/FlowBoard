using Identity.Service.Domain.Enums;

namespace Identity.Service.Domain.Entities;

/// <summary>
/// WorkspaceMember - join table with composite PK WorkspaceId+UserId, Role 6 values (Member 0, ProjectManager 1 can create projects, OrgAdmin 2, Client 3 external view+comment, Viewer 4, SuperAdmin 5), JoinedAt. Enforces one membership per workspace, drives JWT Role/workspace_id claims + controller [Authorize] checks (Invite requires OrgAdmin, CreateProject requires PM/OrgAdmin).
/// </summary>
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
