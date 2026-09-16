using Notification.Service.Application.DTOs;
using SharedKernel;

namespace Notification.Service.Application.Interfaces;

public interface INotificationService
{
    Task<Result> PersistTaskCreatedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, string title, Guid actorUserId, List<Guid> recipientUserIds, DateTime occurredOnUtc, CancellationToken ct = default);
    Task<Result> PersistTaskMovedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, Guid fromListId, string fromListName, Guid toListId, string toListName, string boardName, string sprintName, string taskTitle, string projectName, string actorName, string actorRole, int position, Guid actorUserId, List<Guid> recipientUserIds, DateTime occurredOnUtc, CancellationToken ct = default);
    Task<Result> PersistTaskCommentedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, Guid commentId, Guid actorUserId, List<Guid> recipientUserIds, DateTime occurredOnUtc, CancellationToken ct = default);
    Task<Result> PersistTaskAssignedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, Guid assigneeId, Guid actorUserId, List<Guid> recipientUserIds, DateTime occurredOnUtc, CancellationToken ct = default);
    Task<Result> PersistTaskDeletedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, string taskTitle, Guid actorUserId, List<Guid> recipientUserIds, DateTime occurredOnUtc, CancellationToken ct = default);
    Task<Result> PersistProjectMemberAddedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid userId, Guid actorUserId, List<Guid> recipientUserIds, DateTime occurredOnUtc, CancellationToken ct = default);
    Task<Result> PersistComplaintCreatedAsync(Guid eventId, Guid organizationId, Guid complaintId, string subject, Guid actorUserId, List<Guid> recipientUserIds, DateTime occurredOnUtc, CancellationToken ct = default);
    Task<PaginatedNotificationsResult> GetNotificationsAsync(Guid recipientUserId, int page, int pageSize, bool? unreadOnly, CancellationToken ct = default);
    Task<Result> MarkAsReadAsync(Guid notificationId, Guid recipientUserId, CancellationToken ct = default);
    Task<Result<int>> MarkAllAsReadAsync(Guid recipientUserId, CancellationToken ct = default);
}
