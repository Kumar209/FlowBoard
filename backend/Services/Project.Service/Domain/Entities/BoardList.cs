using SharedKernel;

namespace Project.Service.Domain.Entities;

/// <summary>
/// BoardList - column inside a Project board (e.g., "To Do", "In Progress", "Done"). Ordered by Position (0..n) for Kanban drag-drop (CDK). Belongs to one Project (ProjectId FK, index ProjectId+Position). Holds ordered TaskItems. Move() reorders, Rename() updates name. Will be rendered as cdkDropList in Angular.
/// </summary>
public class BoardList : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Position { get; private set; }

    public Project? Project { get; private set; }
    public ICollection<TaskItem> Tasks { get; private set; } = new List<TaskItem>();

    private BoardList() { }

    public BoardList(Guid projectId, string name, int position)
    {
        ProjectId = projectId;
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
}
