using SharedKernel;

namespace Project.Service.Domain.Entities;

/// <summary>
/// Comment - discussion thread on a TaskItem. AuthorId is UserId from Identity (JWT sub claim). Content 5000 chars, edited via Edit(). Belongs to Task (TaskId FK indexed). Client role can view/comment assigned tasks (Task 1.5 verified Client POST /tasks 403 but comment 201 allowed). Will publish TaskCommented domain event.
/// </summary>
public class Comment : BaseEntity
{
    public Guid TaskId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; } = string.Empty;

    public TaskItem? Task { get; private set; }

    private Comment() { }

    public Comment(Guid taskId, Guid authorId, string content)
    {
        TaskId = taskId;
        AuthorId = authorId;
        Content = content;
    }

    public void Edit(string content) { Content = content; Touch(); }
}
