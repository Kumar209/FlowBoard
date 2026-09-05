using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.Commands;

namespace Project.Service.Api.Controllers;

/// <summary>
/// BoardLists API - thin, no Redis in controller (invalidation via handler pipeline). POST /api/projects/{projectId}/lists.
/// </summary>
[ApiController]
[Authorize]
public class BoardListsController : ControllerBase
{
    private readonly IMediator _mediator;
    public BoardListsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("api/projects/{projectId}/lists")]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateListBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        if (roles.Contains("Viewer")) return StatusCode(403, new { error = "Viewer cannot create lists" });
        var result = await _mediator.Send(new CreateBoardListCommand(projectId, body.Name, userId.Value, roles));
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

public record CreateListBody(string Name);
