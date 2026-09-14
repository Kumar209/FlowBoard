using SharedKernel;

namespace Identity.Service.Domain.Entities;

public class PendingUserSuspension : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public DateTime DeadlineAt { get; private set; }
    public string Status { get; private set; } = "Pending"; // Pending, Cancelled, Executed
    public Guid CreatedBy { get; private set; }

    private PendingUserSuspension() { }

    public PendingUserSuspension(Guid userId, Guid organizationId, string reason, string message, DateTime deadlineAt, Guid createdBy)
    {
        UserId = userId;
        OrganizationId = organizationId;
        Reason = reason;
        Message = message;
        DeadlineAt = deadlineAt;
        CreatedBy = createdBy;
        Status = "Pending";
    }

    public void Cancel() { Status = "Cancelled"; Touch(); }
    public void Execute() { Status = "Executed"; Touch(); }
}
