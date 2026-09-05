using SharedKernel;

namespace Project.Service.Domain.Entities;

/// <summary>
/// SubTask - checklist item inside a TaskItem (e.g., "Write tests"). Belongs to one Task (TaskId FK). Title 300, IsCompleted flag toggled via Toggle(). Used for progress bar (completed/total) on task cards. Cascade delete when parent Task deleted.
/// </summary>
public class SubTask : BaseEntity
{
    public Guid TaskId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public bool IsCompleted { get; private set; }

    public TaskItem? Task { get; private set; }

    private SubTask() { }

    public SubTask(Guid taskId, string title)
    {
        TaskId = taskId;
        Title = title;
        IsCompleted = false;
    }

    public void Toggle() => IsCompleted = !IsCompleted;
    public void Rename(string title) { Title = title; Touch(); }
}
