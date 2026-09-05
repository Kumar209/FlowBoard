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

    public Project? Project { get; private set; }
    public BoardList? List { get; private set; }
    public ICollection<SubTask> SubTasks { get; private set; } = new List<SubTask>();
    public ICollection<Comment> Comments { get; private set; } = new List<Comment>();

    private TaskItem() { }

    public TaskItem(Guid projectId, Guid listId, string title, Guid createdById, int position, TaskPriority priority = TaskPriority.Medium, Guid? assigneeId = null, string? description = null, string? labelsJson = null, DateTime? dueDate = null)
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
    }

    public void MoveToList(Guid newListId, int newPosition)
    {
        ListId = newListId;
        Position = newPosition;
        Touch();
    }

    public void Update(string title, string? description, TaskPriority priority, string? labelsJson, Guid? assigneeId, DateTime? dueDate)
    {
        Title = title;
        Description = description;
        Priority = priority;
        LabelsJson = labelsJson;
        AssigneeId = assigneeId;
        DueDate = dueDate;
        Touch();
    }

    public void Reorder(int newPosition)
    {
        Position = newPosition;
        Touch();
    }
}
