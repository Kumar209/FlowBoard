using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Identity.Service.Application.Interfaces;
using Identity.Service.Infrastructure.Services;

namespace Identity.Service.Api.Controllers;

[ApiController]
[Route("api/organizations/{orgId}/workspace-roles")]
[Authorize]
public class OrganizationRolesController : ControllerBase
{
    private readonly IOrganizationRoleService _service;
    public OrganizationRolesController(IOrganizationRoleService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetRoles(Guid orgId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        try { var list = await _service.GetRolesAsync(orgId, userId.Value); return Ok(list); }
        catch (Exception ex) { return Handle(ex); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid orgId, [FromBody] CreateRoleRequest req)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        try { var dto = await _service.CreateRoleAsync(orgId, req.Name, req.Description, userId.Value); return CreatedAtAction(nameof(GetRoles), new { orgId }, dto); }
        catch (Exception ex) { return Handle(ex); }
    }

    [HttpPut("{roleId}")]
    public async Task<IActionResult> Update(Guid orgId, Guid roleId, [FromBody] UpdateRoleRequest req)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        try { var dto = await _service.UpdateRoleAsync(orgId, roleId, req.Name, req.Description, userId.Value); return Ok(dto); }
        catch (Exception ex) { return Handle(ex); }
    }

    [HttpDelete("{roleId}")]
    public async Task<IActionResult> Delete(Guid orgId, Guid roleId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        try { await _service.DeleteRoleAsync(orgId, roleId, userId.Value); return Ok(new { message = "Role deleted", roleId }); }
        catch (Exception ex) { return Handle(ex); }
    }

    [HttpGet("{roleId}/permissions")]
    public async Task<IActionResult> GetRolePermissions(Guid orgId, Guid roleId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        try { var dto = await _service.GetRoleWithPermissionsAsync(orgId, roleId, userId.Value); return Ok(dto); }
        catch (Exception ex) { return Handle(ex); }
    }

    [HttpPut("{roleId}/permissions")]
    public async Task<IActionResult> UpdatePermissions(Guid orgId, Guid roleId, [FromBody] UpdateRolePermissionsRequest req)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        try { var dto = await _service.UpdateRolePermissionsAsync(orgId, roleId, req.PermissionIds ?? new List<Guid>(), userId.Value); return Ok(dto); }
        catch (Exception ex) { return Handle(ex); }
    }

    [HttpGet("/api/permissions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllPermissions()
    {
        try { var list = await _service.GetPermissionsAsync(); return Ok(list); }
        catch (Exception ex) { return Handle(ex); }
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private IActionResult Handle(Exception ex)
    {
        if (ex is ForbiddenException) return StatusCode(403, new { error = ex.Message });
        if (ex is NotFoundException) return NotFound(new { error = ex.Message });
        if (ex is ValidationException) return BadRequest(new { error = ex.Message });
        return BadRequest(new { error = ex.Message });
    }
}

public record CreateRoleRequest(string Name, string? Description);
public record UpdateRoleRequest(string Name, string? Description);
public record UpdateRolePermissionsRequest(List<Guid> PermissionIds);
