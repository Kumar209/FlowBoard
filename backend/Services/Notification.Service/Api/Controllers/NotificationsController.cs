using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notification.Service.Infrastructure.Persistence;
using System.Security.Claims;

namespace Notification.Service.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationDbContext _db;
    public NotificationsController(NotificationDbContext db) => _db = db;

    private Guid GetUserId() => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value, out var g) ? g : Guid.Empty;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? unreadOnly = null)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Show notifications where ActorId != userId? For now show all for user's workspaces? Simplified: all notifications ordered unread first + OccurredOn desc
        var q = _db.Notifications.AsQueryable();
        if (unreadOnly == true) q = q.Where(n => !n.IsRead);

        var total = await q.CountAsync();
        Response.Headers["X-Total-Count"] = total.ToString();

        var items = await q.OrderBy(n => n.IsRead).ThenByDescending(n => n.OccurredOn)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new { n.Id, n.EventId, n.ProjectId, n.TaskId, n.ActorId, n.Action, n.PayloadJson, n.WorkspaceId, n.IsRead, n.OccurredOn, n.CreatedAt })
            .ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        var notif = await _db.Notifications.FindAsync(id);
        if (notif == null) return NotFound(new { error = "Notification not found" });
        notif.MarkRead();
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var unread = await _db.Notifications.Where(n => !n.IsRead).ToListAsync();
        foreach (var n in unread) n.MarkRead();
        await _db.SaveChangesAsync();
        return Ok(new { updated = unread.Count });
    }
}
