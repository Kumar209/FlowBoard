using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.Commands;
using Project.Service.Application.Queries;

namespace Project.Service.Api.Controllers;

/// <summary>
/// Projects API - thin controllers (MNC-grade: no Redis logic here, caching via CachingBehavior pipeline for ICacheableRequest). YARP routes /api/workspaces/{wid}/projects -> :5002. PM/OrgAdmin create 201 else 403.
/// </summary>
[ApiController]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProjectsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("api/workspaces/{workspaceId}/projects")]
    public async Task<IActionResult> Create(Guid workspaceId, [FromBody] CreateProjectBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var role = GetRoleForWorkspace(workspaceId);
        if (role == null) return StatusCode(403, new { error = "Forbidden - Not a member of this workspace" });
        var allowed = new[] { "OrgAdmin", "ProjectManager", "SuperAdmin" };
        if (!allowed.Contains(role)) return StatusCode(403, new { error = $"Forbidden - Need OrgAdmin/ProjectManager. Your role in this workspace: {role}" });
        var roles = GetRoles();
        var result = await _mediator.Send(new CreateProjectCommand(workspaceId, body.Name, body.Description, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpGet("api/workspaces/{workspaceId}/projects")]
    public async Task<IActionResult> GetByWorkspace(Guid workspaceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetProjectsQuery(workspaceId, page, pageSize));
        Response.Headers.Append("X-Total-Count", result.Total.ToString());
        return Ok(new { items = result.Items, total = result.Total, page, pageSize });
    }

    [HttpGet("api/projects/{projectId}")]
    public async Task<IActionResult> GetOne(Guid projectId)
    {
        var board = await _mediator.Send(new GetBoardQuery(projectId));
        return Ok(board.Project);
    }

    [HttpGet("api/projects/{projectId}/board")]
    public async Task<IActionResult> GetBoard(Guid projectId)
    {
        // MNC-grade: caching handled by CachingBehavior pipeline (ICacheableRequest) - controller is thin, no manual Get/Set
        var board = await _mediator.Send(new GetBoardQuery(projectId));
        return Ok(board);
    }

    [HttpPut("api/projects/{projectId}")]
    public async Task<IActionResult> Update(Guid projectId, [FromBody] UpdateProjectBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        // Need workspaceId for role check - fetch project first
        var board = await _mediator.Send(new GetBoardQuery(projectId));
        if (board?.Project == null) return NotFound(new { error = "Project not found" });
        var role = GetRoleForWorkspace(board.Project.WorkspaceId);
        if (role == null) return StatusCode(403, new { error = "Forbidden - Not a member of this workspace" });
        var allowed = new[] { "OrgAdmin", "ProjectManager", "SuperAdmin" };
        if (!allowed.Contains(role)) return StatusCode(403, new { error = $"Forbidden - Need OrgAdmin/ProjectManager. Your role in this workspace: {role}" });
        var roles = GetRoles();
        var result = await _mediator.Send(new UpdateProjectCommand(projectId, body.Name, body.Description, body.Slug, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("api/projects/{projectId}")]
    public async Task<IActionResult> Delete(Guid projectId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var board = await _mediator.Send(new GetBoardQuery(projectId));
        if (board?.Project == null) return NotFound(new { error = "Project not found" });
        var role = GetRoleForWorkspace(board.Project.WorkspaceId);
        if (role == null) return StatusCode(403, new { error = "Forbidden - Not a member of this workspace" });
        var allowed = new[] { "OrgAdmin", "SuperAdmin" };
        if (!allowed.Contains(role)) return StatusCode(403, new { error = $"Forbidden - Need OrgAdmin/SuperAdmin to delete. Your role: {role}" });
        var roles = GetRoles();
        var result = await _mediator.Send(new DeleteProjectCommand(projectId, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(new { message = "Deleted", projectId });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).Distinct().ToList();
    private string? GetRoleForWorkspace(Guid workspaceId)
    {
        var wids = User.FindAll("workspace_id").Select(c => c.Value).ToList();
        var roles = User.FindAll(ClaimTypes.Role).Concat(User.FindAll("role")).Select(c => c.Value).ToList();
        for (int i = 0; i < wids.Count && i < roles.Count; i++)
            if (Guid.TryParse(wids[i], out var g) && g == workspaceId) return roles[i];
        return null;
    }
}

public record CreateProjectBody(string Name, string? Description);
public record UpdateProjectBody(string Name, string? Description, string? Slug);
