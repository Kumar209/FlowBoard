using SharedKernel;

namespace Identity.Service.Domain.Entities;

/// <summary>
/// Workspace - team space inside Organization (e.g., "Personal Workspace" Slug personal-xxxx). Created via POST /api/workspaces (OrgAdmin/SuperAdmin, first workspace auto-OrgAdmin). Members linked via WorkspaceMember 6 roles. Used for RBAC tenant isolation + JWT workspace_id claims.
/// </summary>
public class Workspace : BaseEntity, IAggregateRoot
{
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    private Workspace() { }

    public Workspace(Guid organizationId, string name, string slug)
    {
        OrganizationId = organizationId;
        Name = name;
        Slug = slug.ToLowerInvariant();
    }

    public void Update(string name, string slug) { Name = name; if (!string.IsNullOrWhiteSpace(slug)) Slug = slug.ToLowerInvariant(); Touch(); }
}
