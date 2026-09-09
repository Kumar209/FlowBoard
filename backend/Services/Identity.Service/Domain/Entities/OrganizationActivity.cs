using SharedKernel;

namespace Identity.Service.Domain.Entities;

/// <summary>
/// OrganizationActivity - org-level audit (e.g., OrgCreated, MemberAdded, RoleCreated, RolePermissionUpdated).
/// Stored in [identity].[OrganizationActivities], separate from [project].[ActivityLogs] (project-level).
/// </summary>
public class OrganizationActivity : BaseEntity, IAggregateRoot
{
    public Guid OrganizationId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? PayloadJson { get; private set; }
    public DateTime OccurredOn { get; private set; }

    private OrganizationActivity() { }

    public OrganizationActivity(Guid organizationId, Guid actorUserId, string action, string? payloadJson = null)
    {
        OrganizationId = organizationId;
        ActorUserId = actorUserId;
        Action = action;
        PayloadJson = payloadJson;
        OccurredOn = DateTime.UtcNow;
    }
}
