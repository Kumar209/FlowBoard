using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.Commands;
using Project.Service.Application.Queries;

namespace Project.Service.Api.Controllers;

[ApiController]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly IMediator _mediator;
    public TeamsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("api/projects/{projectId}/teams")]
    public async Task<IActionResult> GetTeams(Guid projectId)
    {
        var result = await _mediator.Send(new GetTeamsQuery(projectId));
        return Ok(result);
    }

    [HttpPost("api/projects/{projectId}/teams")]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateTeamBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new CreateTeamCommand(projectId, body.Name, body.Description, userId.Value));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("api/teams/{teamId}")]
    public async Task<IActionResult> Update(Guid teamId, [FromBody] CreateTeamBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new UpdateTeamCommand(teamId, body.Name, body.Description, userId.Value));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("api/teams/{teamId}")]
    public async Task<IActionResult> Delete(Guid teamId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new DeleteTeamCommand(teamId, userId.Value));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("api/teams/{teamId}/members")]
    public async Task<IActionResult> GetMembers(Guid teamId)
    {
        var result = await _mediator.Send(new GetTeamMembersQuery(teamId));
        return Ok(result);
    }

    [HttpPost("api/teams/{teamId}/members")]
    public async Task<IActionResult> AddMember(Guid teamId, [FromBody] AddMemberBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new AddTeamMemberCommand(teamId, body.UserId, userId.Value));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpDelete("api/teams/{teamId}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(Guid teamId, Guid userId)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var result = await _mediator.Send(new RemoveTeamMemberCommand(teamId, userId, callerId.Value));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
}

public record CreateTeamBody(string Name, string? Description);
public record AddMemberBody(Guid UserId);
