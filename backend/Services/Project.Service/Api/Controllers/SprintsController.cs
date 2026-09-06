using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.Commands;
using Project.Service.Application.Queries;

namespace Project.Service.Api.Controllers;

[ApiController]
[Authorize]
public class SprintsController : ControllerBase
{
    private readonly IMediator _mediator;
    public SprintsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("api/projects/{projectId}/sprints")]
    public async Task<IActionResult> Get(Guid projectId, [FromQuery] Guid? boardId)
    {
        var list = await _mediator.Send(new GetSprintsQuery(projectId, boardId));
        return Ok(list);
    }

    [HttpPost("api/projects/{projectId}/sprints")]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateSprintBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new CreateSprintCommand(projectId, body.BoardId, body.Name, body.StartDate, body.EndDate, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("api/sprints/{sprintId}")]
    public async Task<IActionResult> Update(Guid sprintId, [FromBody] UpdateSprintBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new UpdateSprintCommand(sprintId, body.Name, body.StartDate, body.EndDate, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("api/sprints/{sprintId}")]
    public async Task<IActionResult> Delete(Guid sprintId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new DeleteSprintCommand(sprintId, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(new { message = "Deleted", sprintId });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).Distinct().ToList();
}
public record CreateSprintBody(Guid? BoardId, string Name, DateTime StartDate, DateTime EndDate);
public record UpdateSprintBody(string Name, DateTime StartDate, DateTime EndDate);
