using SharedKernel;

namespace Identity.Service.Domain.Entities;

public class ComplaintReply : BaseEntity, IAggregateRoot
{
    public Guid ComplaintId { get; private set; }
    public Complaint Complaint { get; private set; } = null!;
    public Guid AuthorUserId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public bool IsSuperAdminReply { get; private set; }

    private ComplaintReply() { }

    public ComplaintReply(Guid complaintId, Guid authorUserId, string message, bool isSuperAdminReply)
    {
        ComplaintId = complaintId;
        AuthorUserId = authorUserId;
        Message = message;
        IsSuperAdminReply = isSuperAdminReply;
    }
}
