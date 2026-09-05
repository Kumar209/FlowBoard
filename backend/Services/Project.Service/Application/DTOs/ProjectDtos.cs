namespace Project.Service.Application.DTOs;

/// <summary>
/// ProjectDtos - response shapes for Project CQRS. Used by handlers to return clean DTOs instead of entities (avoids EF navigation serialization). Key is prefix like FB-3 for search (Task 2.5).
/// </summary>
public record ProjectDto(Guid Id, Guid WorkspaceId, string Name, string Key, string? Description, Guid OwnerId, DateTime CreatedAt);
public record BoardListDto(Guid Id, Guid ProjectId, string Name, int Position);
public record TaskDto(Guid Id, Guid ProjectId, Guid ListId, string Title, string? Description, string Priority, string? LabelsJson, Guid? AssigneeId, int Position, DateTime CreatedAt);
public record CommentDto(Guid Id, Guid TaskId, Guid AuthorId, string Content, DateTime CreatedAt);
public record ActivityDto(Guid Id, Guid ProjectId, Guid? TaskId, Guid ActorId, string Action, string PayloadJson, DateTime OccurredAt);
