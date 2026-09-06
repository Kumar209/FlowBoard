using SharedKernel;
using Project.Service.Domain.Enums;

namespace Project.Service.Domain.Entities;

/// <summary>
/// TaskItem - core work card on a BoardList (named TaskItem to avoid clash with System.Threading.Tasks.Task). Title 300 chars, Description 5000, Priority (Low/Medium/High/Urgent), LabelsJson (JSON array e.g. ["bug","frontend"]), AssigneeId, Position for ordering inside list, DueDate. Belongs to Project + List (FKs indexed ListId+Position, AssigneeId). Methods MoveToList(newListId,newPos) for drag-drop with Redis lock, Update() for edits, Reorder(). Navigation: SubTasks, Comments, ActivityLogs.
/// </summary>
public class TaskItem : BaseEntity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public Guid ListId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskPriority Priority { get; private set; } = TaskPriority.Medium;
    public string? LabelsJson { get; private set; } // JSON array ["bug","frontend"]
    public Guid? AssigneeId { get; private set; }
    public int Position { get; private set; }
    public Guid CreatedById { get; private set; }
    public DateTime? DueDate { get; private set; }
    // Enterprise extensions
    public string IssueType { get; private set; } = "Task"; // Task, Bug, Story, Feature, Sub-task
    public string? Epic { get; private set; } // e.g., Authentication
    public int? StoryPoints { get; private set; } // e.g., 5
    public DateTime? StartDate { get; private set; }
    public string? Environment { get; private set; } // Development, QA, Staging, Production
    public Guid? ParentIssueId { get; private set; }
    public Guid? SprintId { get; private set; }
    public Guid? TeamId { get; private set; }
    public string Status { get; private set; } = "To Do"; // Visual column = Issue Status (workflow: Board column is visual of Status)
    public string? WatchersJson { get; private set; } // JSON array of userIds
    public string? LinkedIssuesJson { get; private set; } // JSON array of {id, relation}
    public int? TimeEstimated { get; private set; } // hours
    public int? TimeSpent { get; private set; }
    public int? TimeRemaining { get; private set; }

    public Project? Project { get; private set; }
    public BoardList? List { get; private set; }
    public ICollection<SubTask> SubTasks { get; private set; } = new List<SubTask>();
    public ICollection<Comment> Comments { get; private set; } = new List<Comment>();

    private TaskItem() { }

    public TaskItem(Guid projectId, Guid listId, string title, Guid createdById, int position, TaskPriority priority = TaskPriority.Medium, Guid? assigneeId = null, string? description = null, string? labelsJson = null, DateTime? dueDate = null, string issueType = "Task", string? epic = null, int? storyPoints = null, DateTime? startDate = null, string? environment = null, Guid? parentIssueId = null, Guid? sprintId = null, Guid? teamId = null, string status = "To Do")
    {
        ProjectId = projectId;
        ListId = listId;
        Title = title;
        CreatedById = createdById;
        Position = position;
        Priority = priority;
        AssigneeId = assigneeId;
        Description = description;
        LabelsJson = labelsJson;
        DueDate = dueDate;
        IssueType = issueType;
        Epic = epic;
        StoryPoints = storyPoints;
        StartDate = startDate;
        Environment = environment;
        ParentIssueId = parentIssueId;
        SprintId = sprintId;
        TeamId = teamId;
        Status = status;
    }

    public void MoveToList(Guid newListId, int newPosition, string? newStatus = null)
    {
        ListId = newListId;
        Position = newPosition;
        if (newStatus != null) Status = newStatus;
        Touch();
    }

    public void Update(string title, string? description, TaskPriority priority, string? labelsJson, Guid? assigneeId, DateTime? dueDate, string? issueType = null, string? epic = null, int? storyPoints = null, DateTime? startDate = null, string? environment = null, Guid? parentIssueId = null, Guid? sprintId = null, string? watchersJson = null, string? linkedIssuesJson = null, int? timeEstimated = null, int? timeSpent = null, int? timeRemaining = null, Guid? teamId = null, string? status = null)
    {
        Title = title;
        Description = description;
        Priority = priority;
        LabelsJson = labelsJson;
        AssigneeId = assigneeId;
        DueDate = dueDate;
        if (issueType != null) IssueType = issueType;
        Epic = epic;
        StoryPoints = storyPoints;
        StartDate = startDate;
        Environment = environment;
        ParentIssueId = parentIssueId;
        SprintId = sprintId;
        WatchersJson = watchersJson;
        LinkedIssuesJson = linkedIssuesJson;
        TimeEstimated = timeEstimated;
        TimeSpent = timeSpent;
        TimeRemaining = timeRemaining;
        TeamId = teamId;
        if (status != null) Status = status;
        Touch();
    }

    public void AssignSprint(Guid? sprintId) { SprintId = sprintId; Touch(); }
    public void AssignTeam(Guid? teamId) { TeamId = teamId; Touch(); }
    public void AssignAssignee(Guid? assigneeId) { AssigneeId = assigneeId; Touch(); }

    public void Reorder(int newPosition)
    {
        Position = newPosition;
        Touch();
    }
}
