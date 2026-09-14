using SharedKernel;

namespace Identity.Service.Domain.Entities;

public class Complaint : BaseEntity, IAggregateRoot
{
    public Guid OrganizationId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Open"; // Open, Replied, Resolved

    private Complaint() { }

    public Complaint(Guid organizationId, Guid createdByUserId, string subject, string message)
    {
        OrganizationId = organizationId;
        CreatedByUserId = createdByUserId;
        Subject = subject;
        Message = message;
        Status = "Open";
    }

    public void MarkReplied() { Status = "Replied"; Touch(); }
    public void Resolve() { Status = "Resolved"; Touch(); }
}
