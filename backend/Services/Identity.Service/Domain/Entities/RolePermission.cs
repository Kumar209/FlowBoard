namespace Identity.Service.Domain.Entities;

/// <summary>
/// RolePermission - join for OrganizationWorkspaceRole <-> Permission (many-to-many).
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    public OrganizationWorkspaceRole? Role { get; set; }
    public Permission? Permission { get; set; }

    public RolePermission() { }
    public RolePermission(Guid roleId, Guid permissionId) { RoleId = roleId; PermissionId = permissionId; }
}
