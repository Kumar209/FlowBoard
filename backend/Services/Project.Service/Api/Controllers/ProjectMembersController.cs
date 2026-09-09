using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.Commands;

namespace Project.Service.Api.Controllers;

[ApiController]
[Authorize]
public class ProjectMembersController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProjectMembersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("api/projects/{projectId}/members")]
    public async Task<IActionResult> Get(Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(new GetProjectMembersQuery(projectId, page, pageSize, search));
        Response.Headers["X-Total-Count"] = result.Total.ToString();
        return Ok(result);
    }

    [HttpGet("api/projects/{projectId}/assignee-candidates")]
    public async Task<IActionResult> GetAssigneeCandidates(Guid projectId)
    {
        var result = await _mediator.Send(new Project.Service.Application.Queries.GetAssigneeCandidatesQuery(projectId));
        return Ok(result);
    }

    [HttpPost("api/projects/{projectId}/members")]
    public async Task<IActionResult> Add(Guid projectId, [FromBody] AddProjectMemberBody body)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new AddProjectMemberCommand(projectId, body.UserId, body.Role ?? "Member", callerId.Value, roles));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpDelete("api/projects/{projectId}/members/{userId}")]
    public async Task<IActionResult> Remove(Guid projectId, Guid userId)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new RemoveProjectMemberCommand(projectId, userId, callerId.Value, roles));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
}

public record AddProjectMemberBody(Guid UserId, string? Role);
