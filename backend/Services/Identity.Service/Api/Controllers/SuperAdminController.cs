using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Identity.Service.Application.SuperAdmin.Queries;
using Identity.Service.Application.SuperAdmin.Interfaces;
using SharedKernel;

namespace Identity.Service.Api.Controllers;

[ApiController]
[Route("api/superadmin")]
[Authorize]
public class SuperAdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISuperAdminService _superAdminService;
    public SuperAdminController(IMediator mediator, ISuperAdminService superAdminService) { _mediator = mediator; _superAdminService = superAdminService; }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { error = "Unauthorized" });
        var roles = GetRoles();
        var result = await _mediator.Send(new GetSuperAdminDashboardQuery(userId.Value, roles));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrganizations([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { error = "Unauthorized" });
        var roles = GetRoles();
        var result = await _mediator.Send(new GetSuperAdminOrganizationsQuery(userId.Value, roles, search, page, pageSize));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { error = "Unauthorized" });
        var roles = GetRoles();
        var result = await _mediator.Send(new GetSuperAdminUsersQuery(userId.Value, roles, search, page, pageSize));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { error = "Unauthorized" });
        var roles = GetRoles();
        var result = await _mediator.Send(new GetSuperAdminSubscriptionsQuery(userId.Value, roles));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("organizations/{orgId}/members")]
    public async Task<IActionResult> GetOrgMembers(Guid orgId, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        try
        {
            var result = await _superAdminService.GetOrgMembersAsync(orgId, search, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex) when (ex.Message.Contains("not found")) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("organizations/{orgId}/suspend")]
    public async Task<IActionResult> SuspendOrg(Guid orgId, [FromBody] SuspendRequest req)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        var actor = GetUserId(); if (actor == null) return Unauthorized();
        try { await _superAdminService.SuspendOrganizationAsync(orgId, req.Reason ?? "Suspended by platform", req.Message, actor.Value); return Ok(new { message = "Organization suspended" }); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("organizations/{orgId}/activate")]
    public async Task<IActionResult> ActivateOrg(Guid orgId)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        var actor = GetUserId(); if (actor == null) return Unauthorized();
        try { await _superAdminService.ActivateOrganizationAsync(orgId, actor.Value); return Ok(new { message = "Organization activated" }); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("organizations/{orgId}")]
    public async Task<IActionResult> DeleteOrg(Guid orgId)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        var actor = GetUserId(); if (actor == null) return Unauthorized();
        try { await _superAdminService.DeleteOrganizationAsync(orgId, actor.Value); return Ok(new { message = "Organization deleted" }); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("users/{userId}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid userId, [FromBody] SuspendUserRequest req)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        var actor = GetUserId(); if (actor == null) return Unauthorized();
        try
        {
            var deadline = req.DeadlineAt ?? DateTime.UtcNow.AddDays(req.DeadlineDays ?? 7);
            await _superAdminService.SuspendUserWithGraceAsync(userId, req.OrganizationId, req.Reason ?? "Suspended", req.Message ?? "Account suspension scheduled", deadline, actor.Value);
            return Ok(new { message = "Suspension scheduled", deadline });
        }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("users/{userId}/activate")]
    public async Task<IActionResult> ActivateUser(Guid userId)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        var actor = GetUserId(); if (actor == null) return Unauthorized();
        try { await _superAdminService.ReactivateUserAsync(userId, actor.Value); return Ok(new { message = "User reactivated" }); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        var actor = GetUserId(); if (actor == null) return Unauthorized();
        try { await _superAdminService.DeleteUserAsync(userId, actor.Value); return Ok(new { message = "User deleted" }); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("notices/{orgId}")]
    public async Task<IActionResult> GetNotices(Guid orgId)
    {
        // SuperAdmin can see any org notices; org members can see own org notices - for now superadmin only
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        var notices = await _superAdminService.GetActiveNoticesAsync(orgId);
        return Ok(notices);
    }

    [HttpGet("complaints")]
    public async Task<IActionResult> GetComplaints([FromQuery] Guid? organizationId)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        var list = await _superAdminService.GetComplaintsAsync(organizationId);
        return Ok(list);
    }

    [HttpPost("complaints/{complaintId}/reply")]
    public async Task<IActionResult> ReplyComplaint(Guid complaintId, [FromBody] ReplyRequest req)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        var actor = GetUserId(); if (actor == null) return Unauthorized();
        try { var reply = await _superAdminService.ReplyToComplaintAsync(complaintId, actor.Value, req.Message, true); return Ok(reply); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("complaints/{complaintId}")]
    public async Task<IActionResult> GetComplaintDetail(Guid complaintId)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        try { var detail = await _superAdminService.GetComplaintDetailAsync(complaintId); return Ok(detail); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("complaints/{complaintId}")]
    public async Task<IActionResult> DeleteComplaint(Guid complaintId)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        var actor = GetUserId(); if (actor == null) return Unauthorized();
        try { await _superAdminService.DeleteComplaintAsync(complaintId, actor.Value); return Ok(new { message = "Deleted" }); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("activities")]
    public async Task<IActionResult> GetActivities([FromQuery] string? search, [FromQuery] string? action, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        var result = await _superAdminService.GetPlatformActivitiesAsync(search, action, page, pageSize);
        return Ok(result);
    }

    [HttpGet("system")]
    public async Task<IActionResult> GetSystem()
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        var result = await _superAdminService.GetSystemStatusAsync();
        return Ok(result);
    }

    [HttpGet("flags")]
    public async Task<IActionResult> GetFlags()
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        var flags = await _superAdminService.GetFeatureFlagsAsync();
        return Ok(flags);
    }

    [HttpPut("flags/{key}/toggle")]
    public async Task<IActionResult> ToggleFlag(string key)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        try { var flag = await _superAdminService.ToggleFeatureFlagAsync(key); return Ok(flag); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("flags/organizations/{orgId}")]
    public async Task<IActionResult> GetOrgFlags(Guid orgId)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        try { var flags = await _superAdminService.GetOrganizationFeatureFlagsAsync(orgId); return Ok(flags); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("flags/{key}/organizations/{orgId}/toggle")]
    public async Task<IActionResult> ToggleOrgFlag(string key, Guid orgId)
    {
        if (!Roles.IsSuperAdmin(GetRoles())) return StatusCode(403, new { error = "Forbidden - SuperAdmin only" });
        try { var flag = await _superAdminService.ToggleOrganizationFeatureFlagAsync(orgId, key); return Ok(flag); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("ai-usage")]
    public async Task<IActionResult> GetAiUsage()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { error = "Unauthorized" });
        var roles = GetRoles();
        var result = await _mediator.Send(new GetSuperAdminAiUsageQuery(userId.Value, roles));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }

    private string[] GetRoles()
    {
        return User.FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Concat(User.FindAll("role").Select(c => c.Value))
            .Concat(User.FindAll("roles").Select(c => c.Value))
            .ToArray();
    }
}

public record SuspendRequest(string? Reason, string? Message);
public record SuspendUserRequest(Guid OrganizationId, string? Reason, string? Message, int? DeadlineDays, DateTime? DeadlineAt);
public record ReplyRequest(string Message);
