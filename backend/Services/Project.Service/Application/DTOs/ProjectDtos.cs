namespace Project.Service.Application.DTOs;

/// <summary>
/// ProjectDtos - response shapes for Project CQRS. Used by handlers to return clean DTOs instead of entities (avoids EF navigation serialization). Key is prefix like FB-3 for search (Task 2.5).
/// </summary>
public record ProjectDto(Guid Id, Guid WorkspaceId, string Name, string Key, string? Description, Guid OwnerId, DateTime CreatedAt);
public record BoardListDto(Guid Id, Guid ProjectId, string Name, int Position);
public record BoardInfoDto(Guid Id, Guid ProjectId, string Name, string Type, string? Description, int Position, DateTime CreatedAt, string? FilterJson = null);
public record SprintDto(Guid Id, Guid ProjectId, Guid? BoardId, string Name, DateTime StartDate, DateTime EndDate, string Status, DateTime CreatedAt);
public record ProjectEnvironmentDto(Guid Id, Guid ProjectId, string Name, string Url, string? Description, string Status, DateTime CreatedAt);
public record TaskDto(Guid Id, Guid ProjectId, Guid ListId, string Title, string? Description, string Priority, string? LabelsJson, Guid? AssigneeId, int Position, DateTime CreatedAt, DateTime? DueDate = null, string IssueType = "Task", string? Epic = null, int? StoryPoints = null, DateTime? StartDate = null, string? Environment = null, Guid? ParentIssueId = null, Guid? SprintId = null, string? WatchersJson = null, string? LinkedIssuesJson = null, int? TimeEstimated = null, int? TimeSpent = null, int? TimeRemaining = null, Guid? TeamId = null, string Status = "To Do");
public record TeamDto(Guid Id, Guid ProjectId, string Name, string? Description, DateTime CreatedAt, int MembersCount);
public record TeamMemberDto(Guid Id, Guid TeamId, Guid UserId, DateTime JoinedAt);
public record BoardFilterDto(List<Guid>? TeamIds = null);
public record CommentDto(Guid Id, Guid TaskId, Guid AuthorId, string Content, DateTime CreatedAt, string? AuthorName = null, string? AuthorAvatarUrl = null);
public record ActivityDto(Guid Id, Guid ProjectId, Guid? TaskId, Guid ActorId, string Action, string PayloadJson, DateTime OccurredAt);
public record SubTaskDto(Guid Id, Guid TaskId, string Title, bool IsCompleted, DateTime CreatedAt);
public record PaginatedResult<T>(List<T> Items, int Total, int Page, int PageSize);
public record TaskDetailDto(TaskDto Task, List<SubTaskDto> SubTasks, List<CommentDto> Comments);
