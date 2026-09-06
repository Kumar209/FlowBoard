using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.Commands;
using Project.Service.Application.Queries;

namespace Project.Service.Api.Controllers;

[ApiController]
[Authorize]
public class EnvironmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public EnvironmentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("api/projects/{projectId}/environments")]
    public async Task<IActionResult> Get(Guid projectId)
    {
        var list = await _mediator.Send(new GetEnvironmentsQuery(projectId));
        return Ok(list);
    }

    [HttpPost("api/projects/{projectId}/environments")]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateEnvBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new CreateEnvironmentCommand(projectId, body.Name, body.Url, body.Description, body.Status ?? "Active", userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("api/environments/{environmentId}")]
    public async Task<IActionResult> Update(Guid environmentId, [FromBody] CreateEnvBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new UpdateEnvironmentCommand(environmentId, body.Name, body.Url, body.Description, body.Status ?? "Active", userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("api/environments/{environmentId}")]
    public async Task<IActionResult> Delete(Guid environmentId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new DeleteEnvironmentCommand(environmentId, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(new { message = "Deleted", environmentId });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).Distinct().ToList();
}
public record CreateEnvBody(string Name, string Url, string? Description, string? Status);
