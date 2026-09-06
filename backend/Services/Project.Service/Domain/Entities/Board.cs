using SharedKernel;

namespace Project.Service.Domain.Entities;

/// <summary>
/// Board - Enterprise: Project has multiple Boards (Engineering/QA/Support) as different views/configurations of same issues.
/// Board owns Columns (BoardList), Swimlanes, Settings. Issues (TaskItem) belong to Project, but are organized via Board + Sprint + Column.
/// This is UI hierarchy, not DB hierarchy: PROJECT → BOARDS, SPRINTS, ISSUES, EPICS, WORKFLOWS. BOARD → Columns, BoardFilter.
/// For backward compat, existing BoardList.ProjectId remains, but new Boards will have ProjectId and BoardList will get BoardId.
/// </summary>
public class Board : BaseEntity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty; // Engineering, QA, Support
    public string Type { get; private set; } = "Kanban"; // Scrum / Kanban
    public string? Description { get; private set; }
    public int Position { get; private set; }
    public string? FilterJson { get; private set; } // JSON {"teamIds":["guid"],"label":"..."} Board = view filter

    public Project? Project { get; private set; }
    public ICollection<BoardList> Columns { get; private set; } = new List<BoardList>();

    private Board() { }

    public Board(Guid projectId, string name, string type = "Kanban", string? description = null, int position = 0, string? filterJson = null)
    {
        ProjectId = projectId;
        Name = name;
        Type = type;
        Description = description;
        Position = position;
        FilterJson = filterJson;
    }

    public void Rename(string name) { Name = name; Touch(); }
    public void UpdateType(string type) { Type = type; Touch(); }
    public void SetFilter(string? filterJson) { FilterJson = filterJson; Touch(); }
}
