using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;
using Identity.Service.Domain.Enums;

namespace Identity.Service.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize]
public class WorkspacesController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IBrevoEmailService _brevo;

    public WorkspacesController(IApplicationDbContext db, IBrevoEmailService brevo)
    {
        _db = db;
        _brevo = brevo;
    }

    // GET /api/workspaces - list workspaces where user is member
    [HttpGet]
    public async Task<IActionResult> GetMyWorkspaces()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var workspaces = await _db.WorkspaceMembers
            .Where(m => m.UserId == userId.Value)
            .Include(m => m.Workspace)
            .Select(m => new { m.Workspace!.Id, m.Workspace.Name, m.Workspace.Slug, m.Workspace.OrganizationId, m.Role })
            .ToListAsync();

        return Ok(workspaces);
    }

    // POST /api/workspaces - create workspace under org (requires OrgAdmin or SuperAdmin of that org)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkspaceRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        if (request.OrganizationId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { error = "OrganizationId and Name required" });

        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == request.OrganizationId);
        if (org == null) return NotFound(new { error = "Organization not found" });

        // Check if user is OrgAdmin or SuperAdmin in any workspace of this org, or owner
        var isAuthorized = org.OwnerId == userId.Value || await _db.WorkspaceMembers
            .Where(m => m.UserId == userId.Value && m.Workspace!.OrganizationId == request.OrganizationId && (m.Role == WorkspaceRole.OrgAdmin || m.Role == WorkspaceRole.SuperAdmin))
            .AnyAsync();

        // Allow first workspace creation if org has no workspaces yet (creator becomes OrgAdmin)
        var hasWorkspaces = await _db.Workspaces.AnyAsync(w => w.OrganizationId == request.OrganizationId);
        if (!hasWorkspaces) isAuthorized = true;

        if (!isAuthorized) return Forbid();

        var slug = request.Name.ToLowerInvariant().Replace(" ", "-") + "-" + Guid.NewGuid().ToString()[..4];
        var workspace = new Workspace(request.OrganizationId, request.Name, slug);
        _db.Workspaces.Add(workspace);
        await _db.SaveChangesAsync();

        // Creator becomes OrgAdmin
        var member = new WorkspaceMember(workspace.Id, userId.Value, WorkspaceRole.OrgAdmin);
        _db.WorkspaceMembers.Add(member);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyWorkspaces), new { id = workspace.Id }, new { workspace.Id, workspace.Name, workspace.Slug });
    }

    // POST /api/workspaces/{id}/invite - invite member/client via Brevo (OrgAdmin only)
    [HttpPost("{id}/invite")]
    public async Task<IActionResult> Invite(Guid id, [FromBody] InviteRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var membership = await _db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == id && m.UserId == userId.Value);
        if (membership == null || (membership.Role != WorkspaceRole.OrgAdmin && membership.Role != WorkspaceRole.SuperAdmin)) return Forbid();

        if (!Enum.TryParse<WorkspaceRole>(request.Role, true, out var role)) return BadRequest(new { error = "Invalid role. Use Member, ProjectManager, Client, Viewer" });
        // Prevent inviting as OrgAdmin/SuperAdmin via this endpoint (only OrgAdmin can change role later)
        if (role == WorkspaceRole.SuperAdmin) return BadRequest(new { error = "Cannot invite as SuperAdmin" });

        var workspace = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == id);
        if (workspace == null) return NotFound(new { error = "Workspace not found" });

        var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant());
        if (targetUser == null) return NotFound(new { error = "User not found - they must register first" });

        var exists = await _db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == id && m.UserId == targetUser.Id);
        if (exists) return BadRequest(new { error = "User already member" });

        var newMember = new WorkspaceMember(id, targetUser.Id, role);
        _db.WorkspaceMembers.Add(newMember);
        await _db.SaveChangesAsync();

        // Send Brevo invite email (best-effort, don't fail invite if email fails)
        var inviter = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
        var inviteLink = $"{Request.Scheme}://{Request.Host}/invite?workspace={id}";
        await _brevo.SendInviteAsync(request.Email, inviteLink, workspace.Name, inviter?.FullName ?? "A teammate");

        return Ok(new { message = "Invited", workspaceId = id, userId = targetUser.Id, role = role.ToString() });
    }

    // PUT /api/workspaces/{id}/members/{userId}/role - change role (OrgAdmin only)
    [HttpPut("{id}/members/{userId}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, Guid userId, [FromBody] ChangeRoleRequest request)
    {
        var callerId = GetUserId();
        if (callerId == null) return Unauthorized();

        var callerMembership = await _db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == id && m.UserId == callerId.Value);
        if (callerMembership == null || (callerMembership.Role != WorkspaceRole.OrgAdmin && callerMembership.Role != WorkspaceRole.SuperAdmin)) return Forbid();

        if (!Enum.TryParse<WorkspaceRole>(request.Role, true, out var newRole)) return BadRequest(new { error = "Invalid role" });

        var target = await _db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == id && m.UserId == userId);
        if (target == null) return NotFound(new { error = "Member not found" });

        target.Role = newRole;
        await _db.SaveChangesAsync();

        return Ok(new { workspaceId = id, userId = userId, role = newRole.ToString() });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
}

public record CreateWorkspaceRequest(Guid OrganizationId, string Name);
public record InviteRequest(string Email, string Role);
public record ChangeRoleRequest(string Role);
