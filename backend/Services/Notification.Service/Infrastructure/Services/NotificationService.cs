using Microsoft.EntityFrameworkCore;
using Notification.Service.Application.DTOs;
using Notification.Service.Application.Interfaces;
using SharedKernel;
using NotificationEntity = Notification.Service.Domain.Entities.Notification;

namespace Notification.Service.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _db;
    public NotificationService(IApplicationDbContext db) => _db = db;

    public async Task<Result> PersistTaskCreatedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, string title, Guid actorUserId, List<Guid> recipientUserIds, DateTime occurredOnUtc, CancellationToken ct = default)
        => await CreateNotificationsForRecipientsAsync(eventId, projectId, workspaceId, taskId, actorUserId, "TaskCreated", $"{{\"title\":\"{title}\"}}", recipientUserIds, occurredOnUtc, ct);

    public async Task<Result> PersistTaskMovedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, Guid fromListId, Guid toListId, int position, Guid actorUserId, List<Guid> recipientUserIds, DateTime occurredOnUtc, CancellationToken ct = default)
        => await CreateNotificationsForRecipientsAsync(eventId, projectId, workspaceId, taskId, actorUserId, "TaskMoved", $"{{\"from\":\"{fromListId}\",\"to\":\"{toListId}\"}}", recipientUserIds, occurredOnUtc, ct);

    public async Task<Result> PersistTaskCommentedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, Guid commentId, Guid actorUserId, List<Guid> recipientUserIds, DateTime occurredOnUtc, CancellationToken ct = default)
        => await CreateNotificationsForRecipientsAsync(eventId, projectId, workspaceId, taskId, actorUserId, "TaskCommented", $"{{\"commentId\":\"{commentId}\"}}", recipientUserIds, occurredOnUtc, ct);

    private async Task<Result> CreateNotificationsForRecipientsAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, Guid actorUserId, string action, string payloadJson, List<Guid> recipientUserIds, DateTime occurredOnUtc, CancellationToken ct)
    {
        var recipients = (recipientUserIds ?? new List<Guid>()).Distinct().Where(r => r != actorUserId).ToList();
        // Idempotency: skip if any notification for this event already exists for a recipient
        foreach (var recipientId in recipients)
        {
            if (await _db.Notifications.AnyAsync(n => n.EventId == eventId && n.RecipientUserId == recipientId, ct)) continue;
            _db.Notifications.Add(new NotificationEntity(eventId, recipientId, projectId, taskId, actorUserId, action, payloadJson, workspaceId, occurredOnUtc));
        }
        // If fan-out is empty (solo actor), keep one self-notification for audit (visible only to actor)
        if (recipients.Count == 0)
        {
            if (!await _db.Notifications.AnyAsync(n => n.EventId == eventId && n.RecipientUserId == actorUserId, ct))
                _db.Notifications.Add(new NotificationEntity(eventId, actorUserId, projectId, taskId, actorUserId, action, payloadJson, workspaceId, occurredOnUtc));
        }
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<PaginatedNotificationsResult> GetNotificationsAsync(Guid recipientUserId, int page, int pageSize, bool? unreadOnly, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.Notifications.Where(n => n.RecipientUserId == recipientUserId);
        if (unreadOnly == true) q = q.Where(n => !n.IsRead);
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(n => n.IsRead).ThenByDescending(n => n.OccurredOnUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new NotificationDto(n.Id, n.EventId, n.RecipientUserId, n.ProjectId, n.TaskId, n.ActorUserId, n.Action, n.PayloadJson, n.WorkspaceId, n.IsRead, n.OccurredOnUtc, n.CreatedAt))
            .ToListAsync(ct);
        return new PaginatedNotificationsResult(items, total, page, pageSize);
    }

    public async Task<Result> MarkAsReadAsync(Guid notificationId, Guid recipientUserId, CancellationToken ct = default)
    {
        var notif = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == recipientUserId, ct);
        if (notif == null) return Result.Failure("Notification not found");
        notif.MarkRead();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<int>> MarkAllAsReadAsync(Guid recipientUserId, CancellationToken ct = default)
    {
        var unread = await _db.Notifications.Where(n => n.RecipientUserId == recipientUserId && !n.IsRead).ToListAsync(ct);
        foreach (var n in unread) n.MarkRead();
        await _db.SaveChangesAsync(ct);
        return Result<int>.Success(unread.Count);
    }
}
