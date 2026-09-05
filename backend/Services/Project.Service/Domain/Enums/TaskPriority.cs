namespace Project.Service.Domain.Enums;

/// <summary>
/// TaskPriority - urgency for TaskItem (Low 0, Medium 1 default, High 2, Urgent 3). Stored as int in Tasks table (HasConversion<int>), used for filtering/sorting (?priority=High) and board card badge color (Low gray, Urgent red). Will be filterable in Task 2.5.
/// </summary>
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}
