using SharedKernel;

namespace Notification.Service.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid EventId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? TaskId { get; private set; }
    public Guid ActorId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = "{}";
    public string WorkspaceId { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime OccurredOn { get; private set; }

    private Notification() { }

    public Notification(Guid eventId, Guid projectId, Guid? taskId, Guid actorId, string action, string payloadJson, string workspaceId, DateTime occurredOn)
    {
        EventId = eventId;
        ProjectId = projectId;
        TaskId = taskId;
        ActorId = actorId;
        Action = action;
        PayloadJson = payloadJson;
        WorkspaceId = workspaceId;
        OccurredOn = occurredOn;
    }

    public void MarkRead() => IsRead = true;
}
