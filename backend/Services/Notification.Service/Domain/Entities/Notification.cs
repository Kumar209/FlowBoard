using SharedKernel;

namespace Notification.Service.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid EventId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? TaskId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = "{}";
    public Guid WorkspaceId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime OccurredOnUtc { get; private set; }

    private Notification() { }

    public Notification(Guid eventId, Guid recipientUserId, Guid projectId, Guid? taskId, Guid actorUserId, string action, string payloadJson, Guid workspaceId, DateTime occurredOnUtc)
    {
        EventId = eventId;
        RecipientUserId = recipientUserId;
        ProjectId = projectId;
        TaskId = taskId;
        ActorUserId = actorUserId;
        Action = action;
        PayloadJson = payloadJson;
        WorkspaceId = workspaceId;
        OccurredOnUtc = occurredOnUtc;
    }

    public void MarkRead() => IsRead = true;
}
