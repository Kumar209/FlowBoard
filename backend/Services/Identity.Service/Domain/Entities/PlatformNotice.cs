using SharedKernel;

namespace Identity.Service.Domain.Entities;

public class PlatformNotice : BaseEntity, IAggregateRoot
{
    public Guid OrganizationId { get; private set; }
    public Guid? TargetUserId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string Type { get; private set; } = "SuspensionWarning"; // SuspensionWarning, Info
    public DateTime? DeadlineAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CreatedBy { get; private set; }

    private PlatformNotice() { }

    public PlatformNotice(Guid organizationId, string message, string type, Guid createdBy, DateTime? deadlineAt = null, Guid? targetUserId = null)
    {
        OrganizationId = organizationId;
        TargetUserId = targetUserId;
        Message = message;
        Type = type;
        CreatedBy = createdBy;
        DeadlineAt = deadlineAt;
        IsActive = true;
    }

    public void Dismiss() => IsActive = false;
}
