using SharedKernel;

namespace Project.Service.Domain.Entities;

/// <summary>
/// BoardList - column inside a Project board (e.g., "To Do", "In Progress", "Done"). Ordered by Position (0..n) for Kanban drag-drop (CDK). Belongs to one Project (ProjectId FK, index ProjectId+Position). Holds ordered TaskItems. Move() reorders, Rename() updates name. Will be rendered as cdkDropList in Angular.
/// </summary>
public class BoardList : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid? BoardId { get; private set; } // Column belongs to Board (Enterprise: Board → Columns)
    public string Name { get; private set; } = string.Empty;
    public int Position { get; private set; }

    public Project? Project { get; private set; }
    public Board? Board { get; private set; }
    public ICollection<TaskItem> Tasks { get; private set; } = new List<TaskItem>();

    private BoardList() { }

    public BoardList(Guid projectId, string name, int position, Guid? boardId = null)
    {
        ProjectId = projectId;
        BoardId = boardId;
        Name = name;
        Position = position;
    }

    public void Move(int newPosition)
    {
        Position = newPosition;
        Touch();
    }

    public void Rename(string name)
    {
        Name = name;
        Touch();
    }

    public void SetBoard(Guid boardId)
    {
        BoardId = boardId;
        Touch();
    }
}
