using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.Commands;

namespace Project.Service.Api.Controllers;

/// <summary>
/// Comments API - thin, invalidation via handler (not controller). POST /api/tasks/{taskId}/comments.
/// </summary>
[ApiController]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public CommentsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("api/tasks/{taskId}/comments")]
    public async Task<IActionResult> Add(Guid taskId, [FromBody] AddCommentBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new AddCommentCommand(taskId, body.Content, userId.Value, roles));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).Distinct().ToList();
}

public record AddCommentBody(string Content);
