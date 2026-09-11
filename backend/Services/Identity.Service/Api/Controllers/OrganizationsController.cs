using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Identity.Service.Application.Commands;
using Identity.Service.Infrastructure.Services;

namespace Identity.Service.Api.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetMyOrganizations()
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new GetMyOrganizationsQuery(userId.Value));
        if (result.IsFailure) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrgRequest request)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new CreateOrganizationCommand(request.Name, request.Description, userId.Value));
        if (result.IsFailure) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetMyOrganizations), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrgRequest request)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new UpdateOrganizationCommand(id, request.Name, request.Description, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new DeleteOrganizationCommand(id, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(new { message = "Organization deleted", id });
    }

    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetOrgMembers(Guid id)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new GetOrgMembersQuery(id, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("{id}/employees")]
    public async Task<IActionResult> CreateEmployee(Guid id, [FromBody] CreateEmployeeRequest req)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        // Support both legacy single Role + WorkspaceIds and new WorkspaceRoles per workspace
        if (req.WorkspaceRoles != null && req.WorkspaceRoles.Any())
        {
            var result2 = await _mediator.Send(new CreateEmployeeWithRolesCommand(id, req.FullName, req.Email, req.Password, req.WorkspaceRoles, userId.Value, req.Role));
            if (result2.IsFailure) return result2.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result2.Error }) : BadRequest(new { error = result2.Error });
            return StatusCode(201, result2.Value);
        }
        var workspaceIds = req.WorkspaceIds ?? (req.WorkspaceId.HasValue ? new List<Guid> { req.WorkspaceId.Value } : null);
        var result = await _mediator.Send(new CreateEmployeeCommand(id, req.FullName, req.Email, req.Password, req.Role, workspaceIds, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpPut("{id}/employees/{userId}")]
    public async Task<IActionResult> UpdateEmployee(Guid id, Guid userId, [FromBody] UpdateEmployeeRequest req)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        if (req.WorkspaceRoles != null && req.WorkspaceRoles.Any())
        {
            var result2 = await _mediator.Send(new UpdateEmployeeWithRolesCommand(id, userId, req.FullName, req.Email, req.WorkspaceRoles, callerId.Value, req.Role));
            if (result2.IsFailure) return result2.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result2.Error }) : result2.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result2.Error }) : BadRequest(new { error = result2.Error });
            return Ok(result2.Value);
        }
        var workspaceIds = req.WorkspaceIds ?? (req.WorkspaceId.HasValue ? new List<Guid> { req.WorkspaceId.Value } : null);
        var result = await _mediator.Send(new UpdateEmployeeCommand(id, userId, req.FullName, req.Email, req.Role, workspaceIds, callerId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("{id}/employees/{userId}")]
    public async Task<IActionResult> DeleteEmployee(Guid id, Guid userId)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var result = await _mediator.Send(new DeleteEmployeeCommand(id, userId, callerId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(new { message = "Removed" });
    }

    [HttpGet("{id}/activities")]
    public async Task<IActionResult> GetActivities(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool includeProjects = false)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var svc = HttpContext.RequestServices.GetRequiredService<Identity.Service.Application.Interfaces.IOrganizationActivityService>();
        try
        {
            var (items, total) = await svc.GetActivitiesAsync(id, page, pageSize, callerId.Value, includeProjects);
            return Ok(new { items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            if (ex is ForbiddenException) return StatusCode(403, new { error = ex.Message });
            if (ex is NotFoundException) return NotFound(new { error = ex.Message });
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
}

public record CreateOrgRequest(string Name, string? Description = null);
public record UpdateOrgRequest(string Name, string? Description);
public record CreateEmployeeRequest(string FullName, string Email, string Password, string Role, Guid? WorkspaceId = null, List<Guid>? WorkspaceIds = null, List<Identity.Service.Application.Interfaces.WorkspaceRoleAssignment>? WorkspaceRoles = null);
public record UpdateEmployeeRequest(string? FullName = null, string? Email = null, string? Role = null, Guid? WorkspaceId = null, List<Guid>? WorkspaceIds = null, List<Identity.Service.Application.Interfaces.WorkspaceRoleAssignment>? WorkspaceRoles = null);
