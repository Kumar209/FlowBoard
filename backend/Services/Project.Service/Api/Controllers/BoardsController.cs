using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.Commands;
using Project.Service.Application.Queries;

namespace Project.Service.Api.Controllers;

[ApiController]
[Authorize]
public class BoardsController : ControllerBase
{
    private readonly IMediator _mediator;
    public BoardsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("api/projects/{projectId}/boards")]
    public async Task<IActionResult> Get(Guid projectId)
    {
        var list = await _mediator.Send(new GetBoardsQuery(projectId));
        return Ok(list);
    }

    [HttpPost("api/projects/{projectId}/boards")]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateBoardBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new CreateBoardCommand(projectId, body.Name, body.Type ?? "Kanban", body.Description, userId.Value, roles, body.FilterJson));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("api/boards/{boardId}")]
    public async Task<IActionResult> Update(Guid boardId, [FromBody] CreateBoardBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new UpdateBoardCommand(boardId, body.Name, body.Type ?? "Kanban", userId.Value, roles, body.FilterJson));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("api/boards/{boardId}")]
    public async Task<IActionResult> Delete(Guid boardId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new DeleteBoardCommand(boardId, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(new { message = "Deleted", boardId });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).Distinct().ToList();
}
public record CreateBoardBody(string Name, string? Type, string? Description, string? FilterJson = null);
