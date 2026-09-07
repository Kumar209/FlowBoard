using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.Service.Application.Commands;
using Notification.Service.Application.Queries;
using System.Security.Claims;

namespace Notification.Service.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public NotificationsController(IMediator mediator) => _mediator = mediator;

    private Guid GetUserId() => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value, out var g) ? g : Guid.Empty;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? unreadOnly = null)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        var result = await _mediator.Send(new GetNotificationsQuery(userId, page, pageSize, unreadOnly));
        Response.Headers["X-Total-Count"] = result.Total.ToString();
        return Ok(result);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        var result = await _mediator.Send(new MarkNotificationReadCommand(id, userId));
        return result.IsSuccess ? Ok(new { success = true }) : NotFound(new { error = result.Error });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        var result = await _mediator.Send(new MarkAllNotificationsReadCommand(userId));
        return result.IsSuccess ? Ok(new { updated = result.Value }) : BadRequest(new { error = result.Error });
    }
}
