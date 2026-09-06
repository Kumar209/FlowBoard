using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;
using Identity.Service.Domain.Enums;

namespace Identity.Service.Api.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public OrganizationsController(IApplicationDbContext db) => _db = db;

    // GET /api/organizations - list orgs where user is member of any workspace in org
    [HttpGet]
    public async Task<IActionResult> GetMyOrganizations()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var workspaceOrgIds = await _db.WorkspaceMembers
            .Where(m => m.UserId == userId.Value)
            .Select(m => m.Workspace)
            .Where(w => w != null)
            .Select(w => w!.OrganizationId)
            .Distinct()
            .ToListAsync();

        var orgs = await _db.Organizations
            .Where(o => workspaceOrgIds.Contains(o.Id) || o.OwnerId == userId.Value)
            .Select(o => new { o.Id, o.Name, o.Slug, o.OwnerId, o.Description, o.CreatedAt })
            .ToListAsync();

        return Ok(orgs);
    }

    // POST /api/organizations - any authenticated user can create org (becomes Owner)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrgRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { error = "Name required" });

        var slug = request.Name.ToLowerInvariant().Replace(" ", "-") + "-" + Guid.NewGuid().ToString()[..6];
        var org = new Organization(request.Name, slug, userId.Value, request.Description);
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyOrganizations), new { id = org.Id }, new { org.Id, org.Name, org.Slug, org.Description });
    }

    // PUT /api/organizations/{id} - OrgAdmin/Owner can update name/description
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrgRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == id);
        if (org == null) return NotFound(new { error = "Organization not found" });
        // Only Owner or OrgAdmin in any workspace of this org can update
        var isOwner = org.OwnerId == userId.Value;
        var isOrgAdmin = await _db.WorkspaceMembers
            .Where(m => m.UserId == userId.Value)
            .Join(_db.Workspaces.Where(w => w.OrganizationId == id), m => m.WorkspaceId, w => w.Id, (m,w) => m)
            .AnyAsync(m => m.Role == WorkspaceRole.OrgAdmin || m.Role == WorkspaceRole.SuperAdmin);
        if (!isOwner && !isOrgAdmin) return StatusCode(403, new { error = "Forbidden - Need OrgAdmin" });
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { error = "Name required" });
        org.Update(request.Name, request.Description);
        await _db.SaveChangesAsync();
        return Ok(new { org.Id, org.Name, org.Slug, org.Description });
    }

    // GET /api/organizations/{id}/members - list all employees in org (all workspace members deduped)
    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetOrgMembers(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var orgExists = await _db.Organizations.AnyAsync(o => o.Id == id);
        if (!orgExists) return NotFound(new { error = "Organization not found" });
        // Get all workspaceIds for this org
        var workspaceIds = await _db.Workspaces.Where(w => w.OrganizationId == id).Select(w => w.Id).ToListAsync();
        var members = await _db.WorkspaceMembers
            .Where(m => workspaceIds.Contains(m.WorkspaceId))
            .Join(_db.Users, m => m.UserId, u => u.Id, (m,u) => new { m.WorkspaceId, m.UserId, m.Role, m.JoinedAt, u.FullName, u.Email, u.AvatarUrl })
            .ToListAsync();
        // Dedupe by UserId - keep first role
        var deduped = members.GroupBy(x => x.UserId).Select(g => g.First()).Select(x => new {
            userId = x.UserId,
            fullName = x.FullName,
            email = x.Email,
            avatarUrl = x.AvatarUrl,
            role = x.Role.ToString(),
            roleInt = (int)x.Role,
            workspaceId = x.WorkspaceId,
            joinedAt = x.JoinedAt
        }).ToList();
        return Ok(deduped);
    }

    // POST /api/organizations/{id}/employees - OrgAdmin creates employee directly (not invite via Brevo)
    [HttpPost("{id}/employees")]
    public async Task<IActionResult> CreateEmployee(Guid id, [FromBody] CreateEmployeeRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == id);
        if (org == null) return NotFound(new { error = "Organization not found" });
        var isOwner = org.OwnerId == userId.Value;
        var isOrgAdmin = await _db.WorkspaceMembers
            .Where(m => m.UserId == userId.Value)
            .Join(_db.Workspaces.Where(w => w.OrganizationId == id), m => m.WorkspaceId, w => w.Id, (m,w) => m)
            .AnyAsync(m => m.Role == WorkspaceRole.OrgAdmin || m.Role == WorkspaceRole.SuperAdmin);
        if (!isOwner && !isOrgAdmin) return StatusCode(403, new { error = "Forbidden - Need OrgAdmin" });
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "FullName, Email, Password required" });
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == req.Email.ToLower()))
            return BadRequest(new { error = "Email already exists" });
        if (!Enum.TryParse<WorkspaceRole>(req.Role, true, out var role))
            return BadRequest(new { error = "Invalid role" });
        // Create user
        var user = new User(req.Email.ToLowerInvariant(), BCrypt.Net.BCrypt.HashPassword(req.Password), req.FullName);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        // Add to first workspace of org (or specified)
        var workspaceId = req.WorkspaceId ?? await _db.Workspaces.Where(w => w.OrganizationId == id).Select(w => w.Id).FirstOrDefaultAsync();
        if (workspaceId != Guid.Empty)
        {
            _db.WorkspaceMembers.Add(new WorkspaceMember(workspaceId, user.Id, role));
            await _db.SaveChangesAsync();
        }
        return StatusCode(201, new { user.Id, user.FullName, user.Email, Role = role.ToString() });
    }

    // PUT /api/organizations/{id}/employees/{userId} - update employee details
    [HttpPut("{id}/employees/{userId}")]
    public async Task<IActionResult> UpdateEmployee(Guid id, Guid userId, [FromBody] UpdateEmployeeRequest req)
    {
        var callerId = GetUserId();
        if (callerId == null) return Unauthorized();
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == id);
        if (org == null) return NotFound(new { error = "Organization not found" });
        var isOwner = org.OwnerId == callerId.Value;
        var isOrgAdmin = await _db.WorkspaceMembers
            .Where(m => m.UserId == callerId.Value)
            .Join(_db.Workspaces.Where(w => w.OrganizationId == id), m => m.WorkspaceId, w => w.Id, (m,w) => m)
            .AnyAsync(m => m.Role == WorkspaceRole.OrgAdmin || m.Role == WorkspaceRole.SuperAdmin);
        if (!isOwner && !isOrgAdmin) return StatusCode(403, new { error = "Forbidden - Need OrgAdmin" });
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { error = "User not found" });
        if (!string.IsNullOrWhiteSpace(req.FullName)) user.UpdateFullName(req.FullName);
        if (!string.IsNullOrWhiteSpace(req.Email) && req.Email.ToLower() != user.Email.ToLower())
        {
            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == req.Email.ToLower() && u.Id != userId))
                return BadRequest(new { error = "Email already exists" });
            user.UpdateEmail(req.Email.ToLowerInvariant());
        }
        await _db.SaveChangesAsync();
        // Update role if provided and workspaceId provided
        if (!string.IsNullOrWhiteSpace(req.Role) && req.WorkspaceId.HasValue && Enum.TryParse<WorkspaceRole>(req.Role, true, out var newRole))
        {
            var member = await _db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == req.WorkspaceId.Value && m.UserId == userId);
            if (member != null) { member.Role = newRole; await _db.SaveChangesAsync(); }
        }
        return Ok(new { user.Id, user.FullName, user.Email });
    }

    // DELETE /api/organizations/{id}/employees/{userId} - remove employee from org (all workspaces)
    [HttpDelete("{id}/employees/{userId}")]
    public async Task<IActionResult> DeleteEmployee(Guid id, Guid userId)
    {
        var callerId = GetUserId();
        if (callerId == null) return Unauthorized();
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == id);
        if (org == null) return NotFound(new { error = "Organization not found" });
        var isOwner = org.OwnerId == callerId.Value;
        var isOrgAdmin = await _db.WorkspaceMembers
            .Where(m => m.UserId == callerId.Value)
            .Join(_db.Workspaces.Where(w => w.OrganizationId == id), m => m.WorkspaceId, w => w.Id, (m,w) => m)
            .AnyAsync(m => m.Role == WorkspaceRole.OrgAdmin || m.Role == WorkspaceRole.SuperAdmin);
        if (!isOwner && !isOrgAdmin) return StatusCode(403, new { error = "Forbidden - Need OrgAdmin" });
        if (userId == org.OwnerId) return BadRequest(new { error = "Cannot remove organization owner" });
        var workspaceIds = await _db.Workspaces.Where(w => w.OrganizationId == id).Select(w => w.Id).ToListAsync();
        var memberships = await _db.WorkspaceMembers.Where(m => m.UserId == userId && workspaceIds.Contains(m.WorkspaceId)).ToListAsync();
        _db.WorkspaceMembers.RemoveRange(memberships);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Removed" });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
}

public record CreateOrgRequest(string Name, string? Description = null);
public record UpdateOrgRequest(string Name, string? Description);
public record CreateEmployeeRequest(string FullName, string Email, string Password, string Role, Guid? WorkspaceId = null);
public record UpdateEmployeeRequest(string? FullName = null, string? Email = null, string? Role = null, Guid? WorkspaceId = null);
