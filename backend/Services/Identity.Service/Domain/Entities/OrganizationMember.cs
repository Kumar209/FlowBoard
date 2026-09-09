using SharedKernel;

namespace Identity.Service.Domain.Entities;

/// <summary>
/// OrganizationMember - explicit org-level membership (MNC company-centric).
/// Replaces derived via WorkspaceMembers join. Determines who belongs to org and org-level role (Member/OrgAdmin/Client).
/// SuperAdmin is global via Users.IsSuperAdmin, not stored here.
/// </summary>
public class OrganizationMember : BaseEntity, IAggregateRoot
{
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public int Role { get; private set; } // 0=Member, 2=OrgAdmin, 3=Client (OrganizationRole)

    private OrganizationMember() { }

    public OrganizationMember(Guid organizationId, Guid userId, int role)
    {
        OrganizationId = organizationId;
        UserId = userId;
        Role = role;
    }

    public void UpdateRole(int role) { Role = role; Touch(); }
}
