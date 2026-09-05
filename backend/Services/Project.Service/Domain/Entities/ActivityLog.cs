using SharedKernel;

namespace Project.Service.Domain.Entities;

/// <summary>
/// ActivityLog - audit timeline for Project/Task actions (e.g., Created, Moved, Commented, Updated). Stores ProjectId, optional TaskId, ActorId (who did it), Action string, PayloadJson (diff), OccurredAt indexed for burndown charts (Task 4.4 ng-apexcharts) and GET /api/projects/{id}/activities pagination. Retention via Task 2.5 filtering.
/// </summary>
public class ActivityLog : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid? TaskId { get; private set; }
    public Guid ActorId { get; private set; }
    public string Action { get; private set; } = string.Empty; // Created, Moved, Commented, Updated
    public string PayloadJson { get; private set; } = "{}";
    public DateTime OccurredAt { get; private set; } = DateTime.UtcNow;

    public Project? Project { get; private set; }
    public TaskItem? Task { get; private set; }

    private ActivityLog() { }

    public ActivityLog(Guid projectId, Guid? taskId, Guid actorId, string action, string payloadJson)
    {
        ProjectId = projectId;
        TaskId = taskId;
        ActorId = actorId;
        Action = action;
        PayloadJson = payloadJson;
        OccurredAt = DateTime.UtcNow;
    }
}
