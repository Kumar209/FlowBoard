using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.Commands;

namespace Project.Service.Api.Controllers;

/// <summary>
/// Comments API - thin, invalidation via handler (not controller). POST/GET/PUT/DELETE comments.
/// </summary>
[ApiController]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public CommentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("api/tasks/{taskId}/comments")]
    public async Task<IActionResult> Get(Guid taskId)
    {
        var list = await _mediator.Send(new Project.Service.Application.Queries.GetCommentsQuery(taskId));
        return Ok(list);
    }

    [HttpPost("api/tasks/{taskId}/comments")]
    public async Task<IActionResult> Add(Guid taskId, [FromBody] AddCommentBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        if (User.FindAll(ClaimTypes.Role).Any(c => c.Value == "Viewer") || User.FindAll("role").Any(c => c.Value == "Viewer"))
            return StatusCode(403, new { error = "Viewer cannot comment" });
        var roles = GetRoles();
        var result = await _mediator.Send(new AddCommentCommand(taskId, body.Content, userId.Value, roles));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("api/comments/{commentId}")]
    public async Task<IActionResult> Update(Guid commentId, [FromBody] AddCommentBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new UpdateCommentCommand(commentId, body.Content, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("api/comments/{commentId}")]
    public async Task<IActionResult> Delete(Guid commentId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new DeleteCommentCommand(commentId, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(new { message = "Deleted", commentId });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).Distinct().ToList();
}

public record AddCommentBody(string Content);
