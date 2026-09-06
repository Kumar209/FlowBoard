using SharedKernel;

namespace Project.Service.Domain.Entities;

/// <summary>
/// Sprint - Time-boxed iteration per Board. Board → Sprint → Columns. Sprint is filter/context for Board view.
/// Sprint groups Issues for a time period. Issue has SprintId (nullable), EpicId, BoardId via Column.
/// Future: PROJECT → SPRINTS, Sprint → Issue assignments.
/// </summary>
public class Sprint : BaseEntity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public Guid? BoardId { get; private set; } // Optional: legacy board link, now project-owned (new AI: Project owns Sprints, Board filters)
    public string Name { get; private set; } = string.Empty; // Sprint 24
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string Status { get; private set; } = "Planned"; // Planned, Active, Completed

    public Project? Project { get; private set; }
    public Board? Board { get; private set; }

    private Sprint() { }

    public Sprint(Guid projectId, Guid? boardId, string name, DateTime startDate, DateTime endDate, string status = "Planned")
    {
        ProjectId = projectId;
        BoardId = boardId;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
    }

    public void Update(string name, DateTime startDate, DateTime endDate) { Name = name; StartDate = startDate; EndDate = endDate; Touch(); }
    public void SetStatus(string status) { Status = status; Touch(); }
}
