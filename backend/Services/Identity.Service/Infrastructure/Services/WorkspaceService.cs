using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;
using SharedKernel;

namespace Identity.Service.Infrastructure.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly IApplicationDbContext _db;
    private readonly IBrevoEmailService _brevo;

    public WorkspaceService(IApplicationDbContext db, IBrevoEmailService brevo)
    {
        _db = db;
        _brevo = brevo;
    }

    private async Task<bool> IsOrganizationAdminAsync(Guid organizationId, Guid userId, CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(u => u.Id == userId && u.IsSuperAdmin, ct)) return true;
        if (await _db.Organizations.AnyAsync(o => o.Id == organizationId && o.OwnerId == userId, ct)) return true;
        return await _db.OrganizationMembers.AnyAsync(m => m.OrganizationId == organizationId && m.UserId == userId && m.Role == Roles.OrgAdminValue, ct);
    }

    public async Task<(List<WorkspaceDto> Items, int Total)> GetMyWorkspacesAsync(Guid userId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = _db.WorkspaceMembers.Where(m => m.UserId == userId).Include(m => m.Workspace).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLowerInvariant();
            query = query.Where(m => m.Workspace!.Name.ToLower().Contains(s) || m.Workspace!.Slug.ToLower().Contains(s));
        }
        var total = await query.CountAsync(ct);
        var workspaces = await query.OrderBy(m => m.Workspace!.Name).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new WorkspaceDto(m.Workspace!.Id, m.Workspace.Name, m.Workspace.Slug, m.Workspace.OrganizationId, m.Role.ToString()))
            .ToListAsync(ct);
        return (workspaces, total);
    }

    public async Task<WorkspaceDto> CreateWorkspaceAsync(Guid organizationId, string name, Guid userId, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct);
        if (org == null) throw new NotFoundException("Organization not found");
        var isOrganizationAdmin = await IsOrganizationAdminAsync(organizationId, userId, ct);
        var isWorkspaceAdmin = await _db.WorkspaceMembers
            .Where(m => m.UserId == userId && m.Workspace!.OrganizationId == organizationId && (m.Role == Roles.OrgAdminValue || m.Role == Roles.SuperAdminValue))
            .AnyAsync(ct);
        var isAuthorized = isOrganizationAdmin || isWorkspaceAdmin || org.OwnerId == userId;
        var hasWorkspaces = await _db.Workspaces.AnyAsync(w => w.OrganizationId == organizationId, ct);
        if (!hasWorkspaces) isAuthorized = true;
        if (!isAuthorized) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        var slug = name.ToLowerInvariant().Replace(" ", "-") + "-" + Guid.NewGuid().ToString()[..4];
        var workspace = new Workspace(organizationId, name, slug);
        _db.Workspaces.Add(workspace);
        await _db.SaveChangesAsync(ct);
        var member = new WorkspaceMember(workspace.Id, userId, Roles.OrgAdminValue);
        _db.WorkspaceMembers.Add(member);
        await _db.SaveChangesAsync(ct);
        try { _db.OrganizationActivities.Add(new OrganizationActivity(organizationId, userId, "WorkspaceCreated", JsonSerializer.Serialize(new { workspaceId = workspace.Id, name = workspace.Name, slug = workspace.Slug }))); await _db.SaveChangesAsync(ct); } catch { }
        return new WorkspaceDto(workspace.Id, workspace.Name, workspace.Slug, workspace.OrganizationId, Roles.OrgAdminValue.ToString());
    }

    public async Task<WorkspaceMemberDto> InviteAsync(Guid workspaceId, string email, string role, Guid callerId, CancellationToken ct = default)
    {
        var workspace = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, ct);
        if (workspace == null) throw new NotFoundException("Workspace not found");
        var workspaceMembership = await _db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == callerId, ct);
        var isWorkspaceAdmin = workspaceMembership != null && (workspaceMembership.Role == Roles.OrgAdminValue || workspaceMembership.Role == Roles.SuperAdminValue);
        var isOrgAdmin = await IsOrganizationAdminAsync(workspace.OrganizationId, callerId, ct);
        if (!isWorkspaceAdmin && !isOrgAdmin) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        if (!Enum.TryParse<int>(role, true, out var parsedRole)) throw new ValidationException("Invalid role");
        if (parsedRole == Roles.SuperAdminValue) throw new ValidationException("Cannot invite as SuperAdmin");
        var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);
        if (targetUser == null) throw new NotFoundException("User not found - they must register first");
        var exists = await _db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == targetUser.Id, ct);
        if (exists) throw new ValidationException("User already member");
        var newMember = new WorkspaceMember(workspaceId, targetUser.Id, parsedRole);
        _db.WorkspaceMembers.Add(newMember);
        await _db.SaveChangesAsync(ct);
        var inviter = await _db.Users.FirstOrDefaultAsync(u => u.Id == callerId, ct);
        var inviteLink = $"https://flowboard.vercel.app/invite?workspace={workspaceId}";
        await _brevo.SendInviteAsync(email, inviteLink, workspace.Name, inviter?.FullName ?? "A teammate");
        return new WorkspaceMemberDto(workspaceId, targetUser.Id, targetUser.Email, targetUser.FullName, targetUser.AvatarUrl, parsedRole.ToString(), (int)parsedRole, newMember.JoinedAt);
    }

    public async Task<WorkspaceDto> UpdateWorkspaceAsync(Guid workspaceId, string name, string? slug, Guid callerId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Name required");
        if (!string.IsNullOrWhiteSpace(slug) && !System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9-]+$")) throw new ValidationException("Slug must be lowercase a-z 0-9 -");
        var ws = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, ct);
        if (ws == null) throw new NotFoundException("Workspace not found");
        var membership = await _db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == callerId, ct);
        var isWorkspaceAdmin = membership != null && (membership.Role == Roles.OrgAdminValue || membership.Role == Roles.SuperAdminValue);
        var isOrgAdmin = await IsOrganizationAdminAsync(ws.OrganizationId, callerId, ct);
        if (!isWorkspaceAdmin && !isOrgAdmin) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        var newSlug = string.IsNullOrWhiteSpace(slug) ? ws.Slug : slug!.ToLowerInvariant();
        if (newSlug != ws.Slug && await _db.Workspaces.AnyAsync(w => w.OrganizationId == ws.OrganizationId && w.Slug == newSlug && w.Id != workspaceId, ct))
            throw new ConflictException("Slug already taken in this organization");
        ws.Update(name, newSlug);
        await _db.SaveChangesAsync(ct);
        try { _db.OrganizationActivities.Add(new OrganizationActivity(ws.OrganizationId, callerId, "WorkspaceUpdated", JsonSerializer.Serialize(new { workspaceId = ws.Id, name, slug = newSlug }))); await _db.SaveChangesAsync(ct); } catch { }
        return new WorkspaceDto(ws.Id, ws.Name, ws.Slug, ws.OrganizationId, membership?.Role.ToString() ?? Roles.OrgAdmin);
    }

    public async Task DeleteWorkspaceAsync(Guid workspaceId, Guid callerId, CancellationToken ct = default)
    {
        var ws = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, ct);
        if (ws == null) throw new NotFoundException("Workspace not found");
        var membership = await _db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == callerId, ct);
        var isWorkspaceAdmin = membership != null && (membership.Role == Roles.OrgAdminValue || membership.Role == Roles.SuperAdminValue);
        var isOrgAdmin = await IsOrganizationAdminAsync(ws.OrganizationId, callerId, ct);
        if (!isWorkspaceAdmin && !isOrgAdmin) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        _db.WorkspaceMembers.RemoveRange(_db.WorkspaceMembers.Where(m => m.WorkspaceId == workspaceId));
        _db.Workspaces.Remove(ws);
        await _db.SaveChangesAsync(ct);
        try { _db.OrganizationActivities.Add(new OrganizationActivity(ws.OrganizationId, callerId, "WorkspaceDeleted", JsonSerializer.Serialize(new { workspaceId, name = ws.Name }))); await _db.SaveChangesAsync(ct); } catch { }
    }

    public async Task<List<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId, Guid callerId, CancellationToken ct = default)
    {
        var ws = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, ct);
        if (ws == null) throw new NotFoundException("Workspace not found");
        var isMember = await _db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == callerId, ct);
        var isOrgAdmin = await IsOrganizationAdminAsync(ws.OrganizationId, callerId, ct);
        if (!isMember && !isOrgAdmin) throw new ForbiddenException("Forbidden");
        return await _db.WorkspaceMembers.Where(m => m.WorkspaceId == workspaceId).Include(m => m.User)
            .Select(m => new WorkspaceMemberDto(m.WorkspaceId, m.UserId, m.User!.Email, m.User.FullName, m.User.AvatarUrl, m.Role.ToString(), (int)m.Role, m.JoinedAt))
            .ToListAsync(ct);
    }

    public async Task<(List<WorkspaceMemberDto> Items, int Total)> GetMembersPagedAsync(Guid workspaceId, Guid callerId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var ws = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, ct);
        if (ws == null) throw new NotFoundException("Workspace not found");
        var isMember = await _db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == callerId, ct);
        var isOrgAdmin = await IsOrganizationAdminAsync(ws.OrganizationId, callerId, ct);
        if (!isMember && !isOrgAdmin) throw new ForbiddenException("Forbidden");
        var query = _db.WorkspaceMembers.Where(m => m.WorkspaceId == workspaceId).Include(m => m.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLowerInvariant();
            query = query.Where(m => m.User!.FullName.ToLower().Contains(s) || m.User!.Email.ToLower().Contains(s) || m.Role.ToString().ToLower().Contains(s));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(m => m.User!.FullName).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new WorkspaceMemberDto(m.WorkspaceId, m.UserId, m.User!.Email, m.User.FullName, m.User.AvatarUrl, m.Role.ToString(), (int)m.Role, m.JoinedAt))
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<WorkspaceMemberDto> ChangeRoleAsync(Guid workspaceId, Guid userId, string role, Guid callerId, CancellationToken ct = default)
    {
        var ws = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, ct);
        if (ws == null) throw new NotFoundException("Workspace not found");
        var callerMembership = await _db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == callerId, ct);
        var isWorkspaceAdmin = callerMembership != null && (callerMembership.Role == Roles.OrgAdminValue || callerMembership.Role == Roles.SuperAdminValue);
        var isOrgAdmin = await IsOrganizationAdminAsync(ws.OrganizationId, callerId, ct);
        if (!isWorkspaceAdmin && !isOrgAdmin) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        if (!Enum.TryParse<int>(role, true, out var newRole)) throw new ValidationException("Invalid role");
        var target = await _db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);
        if (target == null) throw new NotFoundException("Member not found");
        target.Role = newRole;
        await _db.SaveChangesAsync(ct);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        return new WorkspaceMemberDto(workspaceId, userId, user?.Email ?? "", user?.FullName ?? "", user?.AvatarUrl, newRole.ToString(), (int)newRole, target.JoinedAt);
    }
}

// Simple domain exceptions for service layer
public class NotFoundException : Exception { public NotFoundException(string msg) : base(msg) {} }
public class ForbiddenException : Exception { public ForbiddenException(string msg) : base(msg) {} }
public class ValidationException : Exception { public ValidationException(string msg) : base(msg) {} }
public class ConflictException : Exception { public ConflictException(string msg) : base(msg) {} }
