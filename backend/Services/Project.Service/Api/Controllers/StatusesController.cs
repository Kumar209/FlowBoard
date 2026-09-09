using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.Interfaces;

namespace Project.Service.Api.Controllers;

[ApiController]
[Authorize]
public class StatusesController : ControllerBase
{
    private readonly IStatusService _service;
    public StatusesController(IStatusService service) => _service = service;

    [HttpGet("api/projects/{projectId}/statuses")]
    public async Task<IActionResult> Get(Guid projectId)
    {
        var list = await _service.GetStatusesAsync(projectId);
        return Ok(list);
    }

    [HttpPost("api/projects/{projectId}/statuses")]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateStatusBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _service.CreateStatusAsync(projectId, body.Name, userId.Value, roles);
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("api/statuses/{statusId}")]
    public async Task<IActionResult> Update(Guid statusId, [FromBody] UpdateStatusBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _service.UpdateStatusAsync(statusId, body.Name, userId.Value, roles);
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("api/statuses/{statusId}")]
    public async Task<IActionResult> Delete(Guid statusId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _service.DeleteStatusAsync(statusId, userId.Value, roles);
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).Distinct().ToList();
}

public record CreateStatusBody(string Name);
public record UpdateStatusBody(string Name);
