using SharedKernel;

namespace Identity.Service.Domain.Entities;

/// <summary>
/// OrganizationWorkspaceRole - custom per-organization workspace role definition (e.g., Developer, QA Viewer).
/// Created by OrgAdmin via Main Sidebar Roles CRUD. Used as CustomRoleId in WorkspaceMembers.
/// </summary>
public class OrganizationWorkspaceRole : BaseEntity, IAggregateRoot
{
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid CreatedBy { get; private set; }

    private OrganizationWorkspaceRole() { }

    public OrganizationWorkspaceRole(Guid organizationId, string name, string? description, Guid createdBy)
    {
        OrganizationId = organizationId;
        Name = name;
        Description = description;
        CreatedBy = createdBy;
    }

    public void Update(string name, string? description) { Name = name; Description = description; Touch(); }
}
