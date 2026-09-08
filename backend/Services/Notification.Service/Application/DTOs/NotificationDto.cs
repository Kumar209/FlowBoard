namespace Notification.Service.Application.DTOs;

public record NotificationDto(
    Guid Id,
    Guid EventId,
    Guid RecipientUserId,
    Guid ProjectId,
    string ProjectName,
    Guid? TaskId,
    string TaskTitle,
    Guid ActorUserId,
    string ActorName,
    string Action,
    string PayloadJson,
    Guid WorkspaceId,
    bool IsRead,
    DateTime OccurredOnUtc,
    DateTime CreatedAt);

public record PaginatedNotificationsResult(List<NotificationDto> Items, int Total, int Page, int PageSize);
