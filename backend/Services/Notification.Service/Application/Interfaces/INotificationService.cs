using Notification.Service.Application.DTOs;
using SharedKernel;

namespace Notification.Service.Application.Interfaces;

public interface INotificationService
{
    Task<Result> PersistTaskCreatedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, string title, Guid actorUserId, DateTime occurredOnUtc, CancellationToken ct = default);
    Task<Result> PersistTaskMovedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, Guid fromListId, Guid toListId, int position, Guid actorUserId, DateTime occurredOnUtc, CancellationToken ct = default);
    Task<Result> PersistTaskCommentedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, Guid commentId, Guid actorUserId, DateTime occurredOnUtc, CancellationToken ct = default);
    Task<PaginatedNotificationsResult> GetNotificationsAsync(Guid recipientUserId, int page, int pageSize, bool? unreadOnly, CancellationToken ct = default);
    Task<Result> MarkAsReadAsync(Guid notificationId, Guid recipientUserId, CancellationToken ct = default);
    Task<Result<int>> MarkAllAsReadAsync(Guid recipientUserId, CancellationToken ct = default);
}
