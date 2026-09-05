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

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).Distinct().ToList();
}

public record CreateProjectBody(string Name, string? Description);
