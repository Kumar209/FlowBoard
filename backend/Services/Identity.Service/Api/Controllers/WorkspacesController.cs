using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Identity.Service.Application.Commands;
using Identity.Service.Application.Interfaces;
using SharedKernel;

namespace Identity.Service.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize]
public class WorkspacesController : ControllerBase
{
    private readonly IMediator _mediator;
    public WorkspacesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetMyWorkspaces([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new GetMyWorkspacesQuery(userId.Value, page, pageSize, search));
        if (result.IsFailure) return BadRequest(new { error = result.Error });
        Response.Headers.Append("X-Total-Count", result.Value.Total.ToString());
        if (Request.Query.ContainsKey("page") || Request.Query.ContainsKey("pageSize") || !string.IsNullOrWhiteSpace(search))
            return Ok(new { items = result.Value.Items, total = result.Value.Total, page, pageSize });
        return Ok(result.Value.Items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkspaceRequest request)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new CreateWorkspaceCommand(request.OrganizationId, request.Name, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetMyWorkspaces), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPost("{id}/invite")]
    public async Task<IActionResult> Invite(Guid id, [FromBody] InviteRequest request)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new InviteMemberCommand(id, request.Email, request.Role, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkspaceRequest request)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new UpdateWorkspaceCommand(id, request.Name, request.Slug, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : result.Error!.Contains("taken", StringComparison.OrdinalIgnoreCase) ? Conflict(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new DeleteWorkspaceCommand(id, userId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : NotFound(new { error = result.Error });
        return Ok(new { message = "Workspace deleted", id });
    }

    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembers(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new GetWorkspaceMembersQuery(id, userId.Value, page, pageSize, search));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        // If paginated request (search or page !=1), return wrapper with items/total, else return array for backward compat
        if (!string.IsNullOrWhiteSpace(search) || page != 1 || pageSize != 20)
        {
            Response.Headers.Append("X-Total-Count", result.Value.Total.ToString());
            return Ok(new { items = result.Value.Items, total = result.Value.Total, page, pageSize });
        }
        return Ok(result.Value.Items);
    }

    [HttpPut("{id}/members/{userId}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, Guid userId, [FromBody] ChangeRoleRequest request)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var result = await _mediator.Send(new ChangeMemberRoleCommand(id, userId, request.Role, callerId.Value));
        if (result.IsFailure) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("{id}/my-permissions")]
    public async Task<IActionResult> MyPermissions(Guid id)
    {
        var callerId = GetUserId(); if (callerId == null) return Unauthorized();
        var db = HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
        var ws = await db.Workspaces.FirstOrDefaultAsync(w => w.Id == id);
        if (ws == null) return NotFound(new { error = "Workspace not found" });
        var isSuper = await db.WorkspaceMembers.AnyAsync(m => m.UserId == callerId.Value && m.Role == Roles.SuperAdminValue);
        if (isSuper) { var all = await db.Permissions.Select(p => p.Key).ToListAsync(); return Ok(new { permissions = all }); }
        var orgMember = await db.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == ws.OrganizationId && m.UserId == callerId.Value);
        var isOwner = await db.Organizations.AnyAsync(o => o.Id == ws.OrganizationId && o.OwnerId == callerId.Value);
        var wsMember = await db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == id && m.UserId == callerId.Value);
        if (wsMember == null && orgMember == null && !isOwner) return StatusCode(403, new { error = "Not a member" });
        // Fixed role implicit permissions
        int? fixedRole = wsMember?.Role ?? orgMember?.Role;
        // If user is OrgAdmin via orgMember or owner, they have all
        if (isOwner || orgMember?.Role == Roles.OrgAdminValue || wsMember?.Role == Roles.OrgAdminValue)
        {
            var all = await db.Permissions.Select(p => p.Key).ToListAsync();
            return Ok(new { permissions = all, role = Roles.OrgAdmin, customRoleId = wsMember?.CustomRoleId });
        }
        if (wsMember?.CustomRoleId != null && wsMember.CustomRoleId != Guid.Empty)
        {
            var permIds = await db.RolePermissions.Where(rp => rp.RoleId == wsMember.CustomRoleId.Value).Select(rp => rp.PermissionId).ToListAsync();
            var keys = await db.Permissions.Where(p => permIds.Contains(p.Id)).Select(p => p.Key).ToListAsync();
            return Ok(new { permissions = keys, role = wsMember.Role, customRoleId = wsMember.CustomRoleId, customRoleName = (await db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.Id == wsMember.CustomRoleId.Value))?.Name });
        }
        // Fixed role mapping
        List<string> keysFixed = new();
        if (fixedRole == Roles.MemberValue) keysFixed = new List<string> { "org:view","workspace:view","project:view","board:view","status:view","task:view","task:create","task:update","task:move","task:assign","comment:view","comment:create","activity:view:project","attachment:view","attachment:create" };
        else if (fixedRole == Roles.ClientValue) keysFixed = new List<string> { "org:view","workspace:view","project:view","board:view","status:view","task:view","comment:view","comment:create","attachment:view" };
        else if (fixedRole == null) keysFixed = new List<string> { "org:view" };
        else keysFixed = await db.Permissions.Select(p => p.Key).ToListAsync();
        return Ok(new { permissions = keysFixed, role = fixedRole, customRoleId = wsMember?.CustomRoleId });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
}

public record CreateWorkspaceRequest(Guid OrganizationId, string Name);
public record UpdateWorkspaceRequest(string Name, string? Slug);
public record InviteRequest(string Email, string Role);
public record ChangeRoleRequest(string Role);
