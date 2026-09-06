using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.Commands;
using Project.Service.Application.Queries;

namespace Project.Service.Api.Controllers;

/// <summary>
/// SubTasks API - checklist inside Task. Jira-style via Task Detail modal.
/// </summary>
[ApiController]
[Authorize]
public class SubTasksController : ControllerBase
{
    private readonly IMediator _mediator;
    public SubTasksController(IMediator mediator) => _mediator = mediator;

    [HttpGet("api/tasks/{taskId}/subtasks")]
    public async Task<IActionResult> Get(Guid taskId)
    {
        var list = await _mediator.Send(new GetSubTasksQuery(taskId));
        return Ok(list);
    }

    [HttpPost("api/tasks/{taskId}/subtasks")]
    public async Task<IActionResult> Create(Guid taskId, [FromBody] SubTaskBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new CreateSubTaskCommand(taskId, body.Title, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("api/subtasks/{subTaskId}")]
    public async Task<IActionResult> Update(Guid subTaskId, [FromBody] SubTaskBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new UpdateSubTaskCommand(subTaskId, body.Title, userId.Value));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("api/subtasks/{subTaskId}/toggle")]
    public async Task<IActionResult> Toggle(Guid subTaskId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new ToggleSubTaskCommand(subTaskId, userId.Value));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("api/subtasks/{subTaskId}")]
    public async Task<IActionResult> Delete(Guid subTaskId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new DeleteSubTaskCommand(subTaskId, userId.Value));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Deleted", subTaskId });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).Distinct().ToList();
}

public record SubTaskBody(string Title);
