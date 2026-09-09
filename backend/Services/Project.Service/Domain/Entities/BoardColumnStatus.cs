namespace Project.Service.Domain.Entities;

/// <summary>
/// BoardColumnStatus - many-to-many mapping: one column can show multiple statuses (e.g., Development → In Progress + In Review), one status can be in multiple columns across boards.
/// </summary>
public class BoardColumnStatus
{
    public Guid ColumnId { get; set; }
    public Guid StatusId { get; set; }

    public BoardList? Column { get; set; }
    public Status? Status { get; set; }

    public BoardColumnStatus() { }
    public BoardColumnStatus(Guid columnId, Guid statusId)
    {
        ColumnId = columnId;
        StatusId = statusId;
    }
}
