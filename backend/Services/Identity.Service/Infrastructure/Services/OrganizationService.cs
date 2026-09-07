using Microsoft.EntityFrameworkCore;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;
using Identity.Service.Domain.Enums;

namespace Identity.Service.Infrastructure.Services;

public class OrganizationService : IOrganizationService
{
    private readonly IApplicationDbContext _db;

    public OrganizationService(IApplicationDbContext db) => _db = db;

    public async Task<List<OrganizationDto>> GetMyOrganizationsAsync(Guid userId, CancellationToken ct = default)
    {
        var workspaceOrgIds = await _db.WorkspaceMembers.Where(m => m.UserId == userId).Select(m => m.Workspace).Where(w => w != null).Select(w => w!.OrganizationId).Distinct().ToListAsync(ct);
        return await _db.Organizations.Where(o => workspaceOrgIds.Contains(o.Id) || o.OwnerId == userId)
            .Select(o => new OrganizationDto(o.Id, o.Name, o.Slug, o.OwnerId, o.Description, o.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<OrganizationDto> CreateOrganizationAsync(string name, string? description, Guid userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Name required");
        var slug = name.ToLowerInvariant().Replace(" ", "-") + "-" + Guid.NewGuid().ToString()[..6];
        var org = new Organization(name, slug, userId, description);
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync(ct);
        return new OrganizationDto(org.Id, org.Name, org.Slug, org.OwnerId, org.Description, org.CreatedAt);
    }

    public async Task<OrganizationDto> UpdateOrganizationAsync(Guid organizationId, string name, string? description, Guid callerId, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct);
        if (org == null) throw new NotFoundException("Organization not found");
        var isOwner = org.OwnerId == callerId;
        var isOrgAdmin = await _db.WorkspaceMembers.Where(m => m.UserId == callerId).Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m,w) => m).AnyAsync(m => m.Role == WorkspaceRole.OrgAdmin || m.Role == WorkspaceRole.SuperAdmin, ct);
        if (!isOwner && !isOrgAdmin) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Name required");
        org.Update(name, description);
        await _db.SaveChangesAsync(ct);
        return new OrganizationDto(org.Id, org.Name, org.Slug, org.OwnerId, org.Description, org.CreatedAt);
    }

    public async Task<List<OrgMemberDto>> GetOrgMembersAsync(Guid organizationId, Guid callerId, CancellationToken ct = default)
    {
        var orgExists = await _db.Organizations.AnyAsync(o => o.Id == organizationId, ct);
        if (!orgExists) throw new NotFoundException("Organization not found");
        var workspaceIds = await _db.Workspaces.Where(w => w.OrganizationId == organizationId).Select(w => w.Id).ToListAsync(ct);
        var members = await _db.WorkspaceMembers.Where(m => workspaceIds.Contains(m.WorkspaceId))
            .Join(_db.Users, m => m.UserId, u => u.Id, (m,u) => new { m.WorkspaceId, m.UserId, m.Role, m.JoinedAt, u.FullName, u.Email, u.AvatarUrl })
            .ToListAsync(ct);
        return members.GroupBy(x => x.UserId).Select(g => g.First()).Select(x => new OrgMemberDto(x.UserId, x.FullName, x.Email, x.AvatarUrl, x.Role.ToString(), (int)x.Role, x.WorkspaceId, x.JoinedAt)).ToList();
    }

    public async Task<OrgMemberDto> CreateEmployeeAsync(Guid organizationId, string fullName, string email, string password, string role, Guid? workspaceId, Guid callerId, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct);
        if (org == null) throw new NotFoundException("Organization not found");
        var isOwner = org.OwnerId == callerId;
        var isOrgAdmin = await _db.WorkspaceMembers.Where(m => m.UserId == callerId).Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m,w) => m).AnyAsync(m => m.Role == WorkspaceRole.OrgAdmin || m.Role == WorkspaceRole.SuperAdmin, ct);
        if (!isOwner && !isOrgAdmin) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(password)) throw new ValidationException("FullName, Email, Password required");
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower(), ct)) throw new ValidationException("Email already exists");
        if (!Enum.TryParse<WorkspaceRole>(role, true, out var parsedRole)) throw new ValidationException("Invalid role");
        var user = new User(email.ToLowerInvariant(), BCrypt.Net.BCrypt.HashPassword(password), fullName);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        var targetWorkspaceId = workspaceId ?? await _db.Workspaces.Where(w => w.OrganizationId == organizationId).Select(w => w.Id).FirstOrDefaultAsync(ct);
        if (targetWorkspaceId != Guid.Empty)
        {
            _db.WorkspaceMembers.Add(new WorkspaceMember(targetWorkspaceId, user.Id, parsedRole));
            await _db.SaveChangesAsync(ct);
        }
        return new OrgMemberDto(user.Id, user.FullName, user.Email, user.AvatarUrl, parsedRole.ToString(), (int)parsedRole, targetWorkspaceId, DateTime.UtcNow);
    }

    public async Task<OrgMemberDto> UpdateEmployeeAsync(Guid organizationId, Guid userId, string? fullName, string? email, string? role, Guid? workspaceId, Guid callerId, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct);
        if (org == null) throw new NotFoundException("Organization not found");
        var isOwner = org.OwnerId == callerId;
        var isOrgAdmin = await _db.WorkspaceMembers.Where(m => m.UserId == callerId).Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m,w) => m).AnyAsync(m => m.Role == WorkspaceRole.OrgAdmin || m.Role == WorkspaceRole.SuperAdmin, ct);
        if (!isOwner && !isOrgAdmin) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) throw new NotFoundException("User not found");
        if (!string.IsNullOrWhiteSpace(fullName)) user.UpdateFullName(fullName);
        if (!string.IsNullOrWhiteSpace(email) && email.ToLower() != user.Email.ToLower())
        {
            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.Id != userId, ct)) throw new ValidationException("Email already exists");
            user.UpdateEmail(email.ToLowerInvariant());
        }
        await _db.SaveChangesAsync(ct);
        if (!string.IsNullOrWhiteSpace(role) && workspaceId.HasValue && Enum.TryParse<WorkspaceRole>(role, true, out var newRole))
        {
            var member = await _db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId.Value && m.UserId == userId, ct);
            if (member != null) { member.Role = newRole; await _db.SaveChangesAsync(ct); }
        }
        var memberInfo = await _db.WorkspaceMembers.Where(m => m.UserId == userId).Join(_db.Users, m => m.UserId, u => u.Id, (m,u) => new { m.WorkspaceId, m.Role, m.JoinedAt, u.FullName, u.Email, u.AvatarUrl }).FirstOrDefaultAsync(ct);
        return new OrgMemberDto(userId, user.FullName, user.Email, user.AvatarUrl, memberInfo?.Role.ToString() ?? role ?? "Member", memberInfo != null ? (int)memberInfo.Role : 0, memberInfo?.WorkspaceId ?? Guid.Empty, memberInfo?.JoinedAt ?? DateTime.UtcNow);
    }

    public async Task DeleteEmployeeAsync(Guid organizationId, Guid userId, Guid callerId, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct);
        if (org == null) throw new NotFoundException("Organization not found");
        var isOwner = org.OwnerId == callerId;
        var isOrgAdmin = await _db.WorkspaceMembers.Where(m => m.UserId == callerId).Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m,w) => m).AnyAsync(m => m.Role == WorkspaceRole.OrgAdmin || m.Role == WorkspaceRole.SuperAdmin, ct);
        if (!isOwner && !isOrgAdmin) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        if (userId == org.OwnerId) throw new ValidationException("Cannot remove organization owner");
        var workspaceIds = await _db.Workspaces.Where(w => w.OrganizationId == organizationId).Select(w => w.Id).ToListAsync(ct);
        var memberships = await _db.WorkspaceMembers.Where(m => m.UserId == userId && workspaceIds.Contains(m.WorkspaceId)).ToListAsync(ct);
        _db.WorkspaceMembers.RemoveRange(memberships);
        await _db.SaveChangesAsync(ct);
    }
}
