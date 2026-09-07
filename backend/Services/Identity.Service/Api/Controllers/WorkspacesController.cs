using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Identity.Service.Application.Commands;

namespace Identity.Service.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize]
public class WorkspacesController : ControllerBase
{
    private readonly IMediator _mediator;
    public WorkspacesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetMyWorkspaces([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new GetMyWorkspacesQuery(userId.Value, page, pageSize, search));
        if (result.IsFailure) return BadRequest(new { error = result.Error });
        Response.Headers.Append("X-Total-Count", result.Value.Total.ToString());
        if (Request.Query.ContainsKey("page") || Request.Query.ContainsKey("pageSize") || !string.IsNullOrWhiteSpace(search))
            return Ok(new { items = result.Value.Items, total = result.Value.Total, page, pageSize });
        return Ok(result.Value.Items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkspaceRequest request)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new CreateWorkspaceCommand(request.OrganizationId, request.Name, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetMyWorkspaces), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPost("{id}/invite")]
    public async Task<IActionResult> Invite(Guid id, [FromBody] InviteRequest request)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new InviteMemberCommand(id, request.Email, request.Role, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkspaceRequest request)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new UpdateWorkspaceCommand(id, request.Name, request.Slug, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : result.Error!.Contains("taken", StringComparison.OrdinalIgnoreCase) ? Conflict(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new DeleteWorkspaceCommand(id, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : NotFound(new { error = result.Error });
        return Ok(new { message = "Workspace deleted", id });
    }

    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new GetWorkspaceMembersQuery(id, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("{id}/members/{userId}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, Guid userId, [FromBody] ChangeRoleRequest request)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var result = await _mediator.Send(new ChangeMemberRoleCommand(id, userId, request.Role, callerId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
}

public record CreateWorkspaceRequest(Guid OrganizationId, string Name);
public record UpdateWorkspaceRequest(string Name, string? Slug);
public record InviteRequest(string Email, string Role);
public record ChangeRoleRequest(string Role);
