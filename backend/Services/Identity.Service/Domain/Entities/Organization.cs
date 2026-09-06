using SharedKernel;

namespace Identity.Service.Domain.Entities;

/// <summary>
/// Organization - top-level tenant (e.g., "Acme Corp" Slug acme-corp-xxxx). Owned by a User (OwnerId, e.g., OrgAdmin who created via Register). Contains Workspaces. Created via POST /api/organizations (any authenticated). IsActive flag for soft delete. Used for multi-tenant isolation: Workspace -> Organization.
/// </summary>
public class Organization : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Guid OwnerId { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Organization() { }

    public Organization(string name, string slug, Guid ownerId, string? description = null)
    {
        Name = name;
        Slug = slug.ToLowerInvariant();
        OwnerId = ownerId;
        Description = description;
    }

    public void Update(string name, string? description = null) { Name = name; Description = description; Touch(); }
    public void Deactivate() => IsActive = false;
}
