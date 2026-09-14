using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Identity.Service.Application.Commands;
using Identity.Service.Application.Queries;
using Identity.Service.Application.SuperAdmin.Interfaces;
using Identity.Service.Infrastructure.Services;
using SharedKernel;

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

    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetStats(Guid id)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new GetOrgStatsQuery(id, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("{id}/chart-data")]
    public async Task<IActionResult> GetChartData(Guid id)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new GetOrgChartDataQuery(id, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
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

    [HttpGet("{id}/notices")]
    public async Task<IActionResult> GetNotices(Guid id)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var svc = HttpContext.RequestServices.GetRequiredService<ISuperAdminService>();
        // Check membership or superadmin
        var isSuper = Roles.IsSuperAdmin(GetRoles());
        if (!isSuper)
        {
            var db = HttpContext.RequestServices.GetRequiredService<Identity.Service.Application.Interfaces.IApplicationDbContext>();
            var isMember = await db.OrganizationMembers.AnyAsync(m => m.OrganizationId == id && m.UserId == callerId.Value) || await db.Organizations.AnyAsync(o => o.Id == id && o.OwnerId == callerId.Value) || await db.WorkspaceMembers.Join(db.Workspaces.Where(w => w.OrganizationId == id), wm => wm.WorkspaceId, w => w.Id, (wm, w) => wm).AnyAsync(x => x.UserId == callerId.Value);
            if (!isMember) return StatusCode(403, new { error = "Forbidden - Not in organization" });
        }
        var notices = await svc.GetActiveNoticesAsync(id);
        return Ok(notices);
    }

    [HttpGet("{id}/complaints")]
    public async Task<IActionResult> GetComplaints(Guid id)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var isSuper = Roles.IsSuperAdmin(GetRoles());
        if (!isSuper)
        {
            var db = HttpContext.RequestServices.GetRequiredService<Identity.Service.Application.Interfaces.IApplicationDbContext>();
            var isMember = await db.OrganizationMembers.AnyAsync(m => m.OrganizationId == id && m.UserId == callerId.Value) || await db.Organizations.AnyAsync(o => o.Id == id && o.OwnerId == callerId.Value);
            if (!isMember) return StatusCode(403, new { error = "Forbidden" });
        }
        var svc = HttpContext.RequestServices.GetRequiredService<ISuperAdminService>();
        var list = await svc.GetComplaintsAsync(id);
        return Ok(list);
    }

    [HttpPost("{id}/complaints")]
    public async Task<IActionResult> CreateComplaint(Guid id, [FromBody] CreateComplaintRequest req)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var svc = HttpContext.RequestServices.GetRequiredService<ISuperAdminService>();
        try
        {
            var c = await svc.CreateComplaintAsync(id, callerId.Value, req.Subject, req.Message);
            return Ok(c);
        }
        catch (Exception ex) { if (ex is ForbiddenException || ex is NotFoundException || ex is ValidationException) return BadRequest(new { error = ex.Message }); return BadRequest(new { error = "Something went wrong \u2014 please try again." }); }
    }

    [HttpPost("{id}/complaints/{complaintId}/reply")]
    public async Task<IActionResult> ReplyComplaint(Guid id, Guid complaintId, [FromBody] ReplyComplaintRequest req)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var isSuper = Roles.IsSuperAdmin(GetRoles());
        var svc = HttpContext.RequestServices.GetRequiredService<ISuperAdminService>();
        try
        {
            var r = await svc.ReplyToComplaintAsync(complaintId, callerId.Value, req.Message, isSuper);
            return Ok(r);
        }
        catch (Exception ex) { if (ex is ForbiddenException || ex is NotFoundException || ex is ValidationException) return BadRequest(new { error = ex.Message }); return BadRequest(new { error = "Something went wrong \u2014 please try again." }); }
    }

    [HttpGet("{id}/complaints/{complaintId}")]
    public async Task<IActionResult> GetComplaintDetail(Guid id, Guid complaintId)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var isSuper = Roles.IsSuperAdmin(GetRoles());
        if (!isSuper)
        {
            var db = HttpContext.RequestServices.GetRequiredService<Identity.Service.Application.Interfaces.IApplicationDbContext>();
            var isMember = await db.OrganizationMembers.AnyAsync(m => m.OrganizationId == id && m.UserId == callerId.Value) || await db.Organizations.AnyAsync(o => o.Id == id && o.OwnerId == callerId.Value);
            if (!isMember) return StatusCode(403, new { error = "Forbidden" });
        }
        var svc = HttpContext.RequestServices.GetRequiredService<ISuperAdminService>();
        try
        {
            var detail = await svc.GetComplaintDetailAsync(complaintId);
            if (detail.Complaint.OrganizationId != id) return NotFound(new { error = "Complaint not in organization" });
            return Ok(detail);
        }
        catch (Exception ex) { if (ex is ForbiddenException || ex is NotFoundException || ex is ValidationException) return BadRequest(new { error = ex.Message }); return BadRequest(new { error = "Something went wrong \u2014 please try again." }); }
    }

    [HttpDelete("{id}/complaints/{complaintId}")]
    public async Task<IActionResult> DeleteComplaint(Guid id, Guid complaintId)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var isSuper = Roles.IsSuperAdmin(GetRoles());
        if (!isSuper)
        {
            var db = HttpContext.RequestServices.GetRequiredService<Identity.Service.Application.Interfaces.IApplicationDbContext>();
            var isMember = await db.OrganizationMembers.AnyAsync(m => m.OrganizationId == id && m.UserId == callerId.Value) || await db.Organizations.AnyAsync(o => o.Id == id && o.OwnerId == callerId.Value);
            if (!isMember) return StatusCode(403, new { error = "Forbidden" });
        }
        var svc = HttpContext.RequestServices.GetRequiredService<ISuperAdminService>();
        try { await svc.DeleteComplaintAsync(complaintId, callerId.Value); return Ok(new { message = "Deleted" }); }
        catch (Exception ex) { if (ex is ForbiddenException || ex is NotFoundException || ex is ValidationException) return BadRequest(new { error = ex.Message }); return BadRequest(new { error = "Something went wrong \u2014 please try again." }); }
    }

    [HttpGet("{id}/my-permissions")]
    public async Task<IActionResult> MyPermissions(Guid id)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var db = HttpContext.RequestServices.GetRequiredService<Identity.Service.Application.Interfaces.IApplicationDbContext>();
        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == id);
        if (org == null) return NotFound(new { error = "Organization not found" });
        var isSuper = await db.WorkspaceMembers.AnyAsync(m => m.UserId == callerId.Value && m.Role == Roles.SuperAdminValue);
        if (isSuper) { var all = await db.Permissions.Select(p => p.Key).ToListAsync(); return Ok(new { permissions = all }); }
        var isOwner = org.OwnerId == callerId.Value;
        var orgMember = await db.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == id && m.UserId == callerId.Value);
        if (isOwner || orgMember?.Role == Roles.OrgAdminValue)
        {
            var all = await db.Permissions.Select(p => p.Key).ToListAsync();
            return Ok(new { permissions = all });
        }
        // Collect custom role permissions from all workspaces in org where user has CustomRoleId
        var wsIds = await db.Workspaces.Where(w => w.OrganizationId == id).Select(w => w.Id).ToListAsync();
        var customRoleIds = await db.WorkspaceMembers.Where(m => wsIds.Contains(m.WorkspaceId) && m.UserId == callerId.Value && m.CustomRoleId != null).Select(m => m.CustomRoleId!.Value).Distinct().ToListAsync();
        HashSet<string> perms = new();
        if (customRoleIds.Any())
        {
            var permIds = await db.RolePermissions.Where(rp => customRoleIds.Contains(rp.RoleId)).Select(rp => rp.PermissionId).Distinct().ToListAsync();
            var keys = await db.Permissions.Where(p => permIds.Contains(p.Id)).Select(p => p.Key).ToListAsync();
            foreach (var k in keys) perms.Add(k);
        }
        // Add fixed Member/Client base perms
        int? fixedRole = orgMember?.Role;
        if (fixedRole == Roles.MemberValue) foreach (var k in new[] { "org:view","workspace:view","project:view","board:view","status:view","task:view","task:create","task:update","task:move","task:assign","comment:view","comment:create","activity:view:project","attachment:view","attachment:create" }) perms.Add(k);
        else if (fixedRole == Roles.ClientValue) foreach (var k in new[] { "org:view","workspace:view","project:view","board:view","status:view","task:view","comment:view","comment:create","attachment:view" }) perms.Add(k);
        else if (fixedRole == null) perms.Add("org:view");
        return Ok(new { permissions = perms.ToList() });
    }

    private string[] GetRoles() => User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).ToArray();

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
public record CreateComplaintRequest(string Subject, string Message);
public record ReplyComplaintRequest(string Message);
