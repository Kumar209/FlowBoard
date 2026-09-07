namespace Notification.Service.Application.DTOs;

public record NotificationDto(
    Guid Id,
    Guid EventId,
    Guid RecipientUserId,
    Guid ProjectId,
    Guid? TaskId,
    Guid ActorUserId,
    string Action,
    string PayloadJson,
    Guid WorkspaceId,
    bool IsRead,
    DateTime OccurredOnUtc,
    DateTime CreatedAt);

public record PaginatedNotificationsResult(List<NotificationDto> Items, int Total, int Page, int PageSize);
