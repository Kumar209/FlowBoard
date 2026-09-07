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

    public async Task<Result> PersistTaskCreatedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, string title, Guid actorUserId, DateTime occurredOnUtc, CancellationToken ct = default)
    {
        // Idempotency per recipient — one notification per event per workspace member? Simplified: one per eventId per system (global) but filtered by recipient on read via WorkspaceId.
        // MNC: create per recipient would fan-out N writes. For FlowBoard, create one row per event with RecipientUserId = Guid.Empty (broadcast) and filter by Workspace membership on read.
        // To fix Global Leak/Wipeout cleanly without N writes, we store one row per event with WorkspaceId and recipient = actor's workspace? Alternative: store one row per event with RecipientUserId = Guid.Empty and filter by workspace membership via Project's WorkspaceId — but we avoid query to [project]. Use WorkspaceId in query filter: recipients are workspace members, but we don't have membership table here (it's in [identity]). So we denormalize: create one row per recipient by fan-out in service if we have membership list via HTTP or event payload contains Recipients.
        // Simplest MNC-grade for now: store one row per event with RecipientUserId = workspaceId-derived broadcast, and Get will filter by WorkspaceId membership via caller's workspaces (not stored). But spec wants per-user filter: we store RecipientUserId = Guid.Empty and treat as broadcast visible to workspace members; Get filters where WorkspaceId in caller's workspaces (requires cross-service call, not ideal).
        // Pragmatic fix for Global Leak: add RecipientUserId to Notification and create one row per event with RecipientUserId = Guid.Empty broadcast + filter by WorkspaceId? First step: store per-event broadcast and enforce RecipientUserId scoping via WorkspaceId + Actor exclusion.
        // For strict per-user isolation, we would fan-out to N recipients here via internal HTTP to Identity.Service /workspaces/{id}/members. For FlowBoard minimal, we store one row per event with RecipientUserId = Guid.Empty and filter Get by WorkspaceId (caller's workspace list would be passed). Simpler for now: store one row with RecipientUserId = workspaceId (as Guid) and filter Get by RecipientUserId == callerId OR WorkspaceId == caller's workspace? This still leaks.
        // Correct MNC independent: Event should carry List<RecipientUserId> from Project.Service (which knows workspace members). ProjectService before publish can query [identity].WorkspaceMembers to get recipients and include in TaskCreatedEvent.RecipientIds. Then Notification persists N rows (one per recipient) with IsRead per user — solves Global Leak/Wipeout perfectly without cross-DB query.
        // For incremental fix without changing ProjectService publish today: store per-event with RecipientUserId = Guid.Empty and change Get to filter by WorkspaceId equality + IsRead per user not supported. So we store per-recipient via expanding here: if RecipientUserId == Guid.Empty, we treat as broadcast and Get will filter where WorkspaceId == query's workspace filter? But Get currently has no workspace filter.
        // Decision: For MNC-grade immediate fix, store one notification per event with RecipientUserId = Guid.Empty (broadcast) but change Get to require RecipientUserId filter correctly and Wipeout fixed. To truly fix leak, need RecipientUserId per row. We will implement per-recipient fan-out by creating one row per event with RecipientUserId = actorUserId's workspace members lookup via [identity] direct SQL (same DB, different schema — allowed as read, not write) — this is still independent read, not write coupling.
        // Implementation: Query [identity].WorkspaceMembers where WorkspaceId = workspaceId to get recipient ids, then create N rows.

        if (await _db.Notifications.AnyAsync(n => n.EventId == eventId && n.RecipientUserId == Guid.Empty, ct))
            return Result.Success(); // legacy duplicate check

        // Fan-out to all workspace members (read from [identity] schema, same physical DB flowboard)
        List<Guid> recipientIds = new();
        try
        {
            recipientIds = await _db.Database.SqlQueryRaw<Guid>("SELECT UserId FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0}", workspaceId).ToListAsync(ct);
        }
        catch { recipientIds = new List<Guid> { actorUserId }; }

        if (recipientIds.Count == 0) recipientIds.Add(actorUserId);

        foreach (var recipientId in recipientIds.Distinct())
        {
            if (recipientId == actorUserId) continue; // optional: don't notify actor themselves
            if (await _db.Notifications.AnyAsync(n => n.EventId == eventId && n.RecipientUserId == recipientId, ct)) continue;
            var notif = new NotificationEntity(eventId, recipientId, projectId, taskId, actorUserId, "TaskCreated", $"{{\"title\":\"{title}\"}}", workspaceId, occurredOnUtc);
            _db.Notifications.Add(notif);
        }

        // If fan-out produced 0 (actor was only member), keep one broadcast for audit
        if (recipientIds.Distinct().Count(r => r != actorUserId) == 0)
        {
            var broadcast = new NotificationEntity(eventId, actorUserId, projectId, taskId, actorUserId, "TaskCreated", $"{{\"title\":\"{title}\"}}", workspaceId, occurredOnUtc);
            _db.Notifications.Add(broadcast);
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> PersistTaskMovedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, Guid fromListId, Guid toListId, int position, Guid actorUserId, DateTime occurredOnUtc, CancellationToken ct = default)
    {
        List<Guid> recipientIds = new();
        try { recipientIds = await _db.Database.SqlQueryRaw<Guid>("SELECT UserId FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0}", workspaceId).ToListAsync(ct); } catch { recipientIds = new List<Guid> { actorUserId }; }
        if (recipientIds.Count == 0) recipientIds.Add(actorUserId);
        foreach (var recipientId in recipientIds.Distinct())
        {
            if (recipientId == actorUserId) continue;
            if (await _db.Notifications.AnyAsync(n => n.EventId == eventId && n.RecipientUserId == recipientId, ct)) continue;
            var notif = new NotificationEntity(eventId, recipientId, projectId, taskId, actorUserId, "TaskMoved", $"{{\"from\":\"{fromListId}\",\"to\":\"{toListId}\"}}", workspaceId, occurredOnUtc);
            _db.Notifications.Add(notif);
        }
        if (recipientIds.Distinct().Count(r => r != actorUserId) == 0)
        {
            _db.Notifications.Add(new NotificationEntity(eventId, actorUserId, projectId, taskId, actorUserId, "TaskMoved", $"{{\"from\":\"{fromListId}\",\"to\":\"{toListId}\"}}", workspaceId, occurredOnUtc));
        }
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> PersistTaskCommentedAsync(Guid eventId, Guid projectId, Guid workspaceId, Guid taskId, Guid commentId, Guid actorUserId, DateTime occurredOnUtc, CancellationToken ct = default)
    {
        List<Guid> recipientIds = new();
        try { recipientIds = await _db.Database.SqlQueryRaw<Guid>("SELECT UserId FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0}", workspaceId).ToListAsync(ct); } catch { recipientIds = new List<Guid> { actorUserId }; }
        if (recipientIds.Count == 0) recipientIds.Add(actorUserId);
        foreach (var recipientId in recipientIds.Distinct())
        {
            if (recipientId == actorUserId) continue;
            if (await _db.Notifications.AnyAsync(n => n.EventId == eventId && n.RecipientUserId == recipientId, ct)) continue;
            var notif = new NotificationEntity(eventId, recipientId, projectId, taskId, actorUserId, "TaskCommented", $"{{\"commentId\":\"{commentId}\"}}", workspaceId, occurredOnUtc);
            _db.Notifications.Add(notif);
        }
        if (recipientIds.Distinct().Count(r => r != actorUserId) == 0)
        {
            _db.Notifications.Add(new NotificationEntity(eventId, actorUserId, projectId, taskId, actorUserId, "TaskCommented", $"{{\"commentId\":\"{commentId}\"}}", workspaceId, occurredOnUtc));
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
