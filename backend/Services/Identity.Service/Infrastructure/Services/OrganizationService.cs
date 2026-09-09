using System.Text.Json;
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
        var isSuper = await _db.WorkspaceMembers.Where(m => m.UserId == callerId && m.Role == WorkspaceRole.SuperAdmin).AnyAsync(ct);
        var isOrgAdmin = isSuper || await _db.WorkspaceMembers.Where(m => m.UserId == callerId).Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m,w) => m).AnyAsync(m => m.Role == WorkspaceRole.OrgAdmin || m.Role == WorkspaceRole.SuperAdmin, ct);
        if (!isOwner && !isOrgAdmin && !isSuper) throw new ForbiddenException("Forbidden - Need OrgAdmin for own org or SuperAdmin");
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Name required");
        org.Update(name, description);
        await _db.SaveChangesAsync(ct);
        return new OrganizationDto(org.Id, org.Name, org.Slug, org.OwnerId, org.Description, org.CreatedAt);
    }

    public async Task DeleteOrganizationAsync(Guid organizationId, Guid callerId, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct);
        if (org == null) throw new NotFoundException("Organization not found");
        var isOwner = org.OwnerId == callerId;
        var isSuper = await _db.WorkspaceMembers.Where(m => m.UserId == callerId && m.Role == WorkspaceRole.SuperAdmin).AnyAsync(ct);
        var isOrgAdmin = isSuper || await _db.WorkspaceMembers.Where(m => m.UserId == callerId).Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m,w) => m).AnyAsync(m => m.Role == WorkspaceRole.OrgAdmin || m.Role == WorkspaceRole.SuperAdmin, ct);
        if (!isOwner && !isSuper && !isOrgAdmin) throw new ForbiddenException("Forbidden - Need OrgAdmin for own org or SuperAdmin");
        // For SuperAdmin can delete any org, for OrgAdmin only own org (already checked via isOrgAdmin/isOwner)
        var workspaceIds = await _db.Workspaces.Where(w => w.OrganizationId == organizationId).Select(w => w.Id).ToListAsync(ct);
        if (workspaceIds.Any())
        {
            var members = await _db.WorkspaceMembers.Where(m => workspaceIds.Contains(m.WorkspaceId)).ToListAsync(ct);
            _db.WorkspaceMembers.RemoveRange(members);
            var workspaces = await _db.Workspaces.Where(w => workspaceIds.Contains(w.Id)).ToListAsync(ct);
            _db.Workspaces.RemoveRange(workspaces);
        }
        _db.Organizations.Remove(org);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<OrgMemberDto>> GetOrgMembersAsync(Guid organizationId, Guid callerId, CancellationToken ct = default)
    {
        var orgExists = await _db.Organizations.AnyAsync(o => o.Id == organizationId, ct);
        if (!orgExists) throw new NotFoundException("Organization not found");
        var workspaceIds = await _db.Workspaces.Where(w => w.OrganizationId == organizationId).Select(w => w.Id).ToListAsync(ct);
        var workspaceMap = await _db.Workspaces.Where(w => workspaceIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, w => w.Name, ct);
        var members = await _db.WorkspaceMembers.Where(m => workspaceIds.Contains(m.WorkspaceId))
            .Join(_db.Users, m => m.UserId, u => u.Id, (m,u) => new { m.WorkspaceId, m.UserId, m.Role, m.JoinedAt, u.FullName, u.Email, u.AvatarUrl })
            .ToListAsync(ct);
        // Group by UserId and collect all workspaces for that user
        var grouped = members.GroupBy(x => x.UserId).Select(g => {
            var first = g.First();
            var allWorkspaceIds = g.Select(x => x.WorkspaceId).Distinct().ToList();
            var allWorkspaceNames = allWorkspaceIds.Select(id => workspaceMap.TryGetValue(id, out var name) ? name : id.ToString()[..6]).ToList();
            return new OrgMemberDto(first.UserId, first.FullName, first.Email, first.AvatarUrl, first.Role.ToString(), (int)first.Role, first.WorkspaceId, first.JoinedAt, allWorkspaceNames, allWorkspaceIds);
        }).ToList();
        // Include org-only members (OrganizationMembers without any WorkspaceMember) - org level Member/OrgAdmin/Client
        var groupedIds = grouped.Select(g => g.UserId).ToHashSet();
        var orgOnlyMembers = await _db.OrganizationMembers.Where(m => m.OrganizationId == organizationId && !groupedIds.Contains(m.UserId))
            .Join(_db.Users, m => m.UserId, u => u.Id, (m,u) => new { m.UserId, m.Role, u.FullName, u.Email, u.AvatarUrl, u.CreatedAt })
            .ToListAsync(ct);
        foreach (var om in orgOnlyMembers)
        {
            string roleStr = om.Role == 2 ? "OrgAdmin" : om.Role == 3 ? "Client" : "Member";
            grouped.Add(new OrgMemberDto(om.UserId, om.FullName, om.Email, om.AvatarUrl, roleStr, om.Role, Guid.Empty, om.CreatedAt, new List<string>(), new List<Guid>()));
        }
        // Also include Organization Owner if not already
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct);
        if (org != null && !grouped.Any(g => g.UserId == org.OwnerId) && !groupedIds.Contains(org.OwnerId))
        {
            var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == org.OwnerId, ct);
            if (owner != null) grouped.Add(new OrgMemberDto(owner.Id, owner.FullName, owner.Email, owner.AvatarUrl, "OrgAdmin", 2, Guid.Empty, owner.CreatedAt, new List<string>(), new List<Guid>()));
        }
        return grouped;
    }

    public async Task<OrgMemberDto> CreateEmployeeAsync(Guid organizationId, string fullName, string email, string password, string role, List<Guid>? workspaceIds, Guid callerId, CancellationToken ct = default)
    {
        // Legacy single role for multiple workspaces - delegate to per-workspace method with same role for all
        var wsRoles = workspaceIds?.Select(wid => new WorkspaceRoleAssignment(wid, role)).ToList();
        return await CreateEmployeeWithRolesAsync(organizationId, fullName, email, password, wsRoles ?? new List<WorkspaceRoleAssignment>(), callerId, ct);
    }

    public async Task<OrgMemberDto> CreateEmployeeWithRolesAsync(Guid organizationId, string fullName, string email, string password, List<WorkspaceRoleAssignment> workspaceRoles, Guid callerId, CancellationToken ct = default, string? orgRole = null)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct);
        if (org == null) throw new NotFoundException("Organization not found");
        var isOwner = org.OwnerId == callerId;
        var isOrgAdmin = await _db.WorkspaceMembers.Where(m => m.UserId == callerId).Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m,w) => m).AnyAsync(m => m.Role == WorkspaceRole.OrgAdmin || m.Role == WorkspaceRole.SuperAdmin, ct);
        if (!isOwner && !isOrgAdmin) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(password)) throw new ValidationException("FullName, Email, Password required");
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower(), ct)) throw new ValidationException("Email already exists");
        var user = new User(email.ToLowerInvariant(), BCrypt.Net.BCrypt.HashPassword(password), fullName);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        // Organization-level member (3 roles: Member 0, OrgAdmin 2, Client 3)
        int orgRoleInt = 0; // Member default
        if (!string.IsNullOrWhiteSpace(orgRole))
        {
            var nr = orgRole.Trim();
            if (nr.Equals("OrgAdmin", StringComparison.OrdinalIgnoreCase)) orgRoleInt = 2;
            else if (nr.Equals("Client", StringComparison.OrdinalIgnoreCase)) orgRoleInt = 3;
            else if (nr.Equals("Member", StringComparison.OrdinalIgnoreCase)) orgRoleInt = 0;
            else throw new ValidationException($"Invalid org role {orgRole} - allowed Member/OrgAdmin/Client");
        }
        _db.OrganizationMembers.Add(new OrganizationMember(organizationId, user.Id, orgRoleInt));
        await _db.SaveChangesAsync(ct);
        var targetRoles = workspaceRoles ?? new List<WorkspaceRoleAssignment>();
        // No auto-assign when no workspace selected - org-level only is allowed (workspace assignment requires custom role)
        foreach (var wr in targetRoles.Where(x => x.WorkspaceId != Guid.Empty))
        {
            Guid? customId = wr.CustomRoleId;
            if ((customId == null || customId == Guid.Empty) && !Enum.TryParse<WorkspaceRole>(wr.Role, true, out _))
            {
                // treat Role string as custom role name
                var cwByName = await _db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Name.ToLower() == wr.Role.ToLower(), ct);
                if (cwByName != null) customId = cwByName.Id;
                else throw new ValidationException($"Custom role '{wr.Role}' not found in organization");
            }
            if (customId != null && customId != Guid.Empty)
            {
                var roleEnt = await _db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.Id == customId.Value && r.OrganizationId == organizationId, ct);
                if (roleEnt == null) throw new ValidationException($"Custom role id {customId} not found");
                _db.WorkspaceMembers.Add(new WorkspaceMember(wr.WorkspaceId, user.Id, WorkspaceRole.Member, customId));
            }
            else
            {
                if (!Enum.TryParse<WorkspaceRole>(wr.Role, true, out var parsed)) throw new ValidationException($"Invalid role {wr.Role}");
                if (parsed == WorkspaceRole.SuperAdmin) throw new ValidationException("SuperAdmin not allowed");
                _db.WorkspaceMembers.Add(new WorkspaceMember(wr.WorkspaceId, user.Id, parsed));
            }
        }
        await _db.SaveChangesAsync(ct);
        // Org Activity audit - MemberAdded
        try { _db.OrganizationActivities.Add(new OrganizationActivity(organizationId, callerId, "MemberAdded", JsonSerializer.Serialize(new { userId = user.Id, email = user.Email, fullName = user.FullName, orgRole = orgRoleInt, workspaces = targetRoles.Select(r => r.WorkspaceId).ToArray() }))); await _db.SaveChangesAsync(ct); } catch { }
        var firstRole = targetRoles.FirstOrDefault()?.Role ?? (orgRole ?? "Member");
        var firstWid = targetRoles.FirstOrDefault()?.WorkspaceId ?? Guid.Empty;
        // Map org role to display if no workspace
        string displayRole = targetRoles.Any() ? firstRole : (orgRole ?? "Member");
        Enum.TryParse<WorkspaceRole>(displayRole, true, out var firstParsed);
        // For custom role display, keep custom name if not enum
        if (!Enum.TryParse<WorkspaceRole>(displayRole, true, out _)) displayRole = targetRoles.FirstOrDefault()?.Role ?? orgRole ?? "Member";
        // Try to map custom name to display; if custom, keep as is for OrgMemberDto Role string
        return new OrgMemberDto(user.Id, user.FullName, user.Email, user.AvatarUrl, displayRole, (int)firstParsed, firstWid, DateTime.UtcNow);
    }

    public async Task<OrgMemberDto> UpdateEmployeeAsync(Guid organizationId, Guid userId, string? fullName, string? email, string? role, List<Guid>? workspaceIds, Guid callerId, CancellationToken ct = default)
    {
        var wr = workspaceIds?.Select(id => new WorkspaceRoleAssignment(id, role ?? "Member")).ToList();
        return await UpdateEmployeeWithRolesAsync(organizationId, userId, fullName, email, wr, callerId, ct);
    }

    public async Task<OrgMemberDto> UpdateEmployeeWithRolesAsync(Guid organizationId, Guid userId, string? fullName, string? email, List<WorkspaceRoleAssignment>? workspaceRoles, Guid callerId, CancellationToken ct = default, string? orgRole = null)
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
        // Org-level role update (Member 0 / OrgAdmin 2 / Client 3)
        if (!string.IsNullOrWhiteSpace(orgRole))
        {
            int orgRoleInt = 0;
            var nr = orgRole.Trim();
            if (nr.Equals("OrgAdmin", StringComparison.OrdinalIgnoreCase)) orgRoleInt = 2;
            else if (nr.Equals("Client", StringComparison.OrdinalIgnoreCase)) orgRoleInt = 3;
            else if (nr.Equals("Member", StringComparison.OrdinalIgnoreCase)) orgRoleInt = 0;
            else throw new ValidationException($"Invalid org role {orgRole} - allowed Member/OrgAdmin/Client");
            var orgMember = await _db.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId, ct);
            if (orgMember != null) orgMember.UpdateRole(orgRoleInt);
            else _db.OrganizationMembers.Add(new OrganizationMember(organizationId, userId, orgRoleInt));
            await _db.SaveChangesAsync(ct);
        }
        if (workspaceRoles != null)
        {
            var orgWorkspaceIds = await _db.Workspaces.Where(w => w.OrganizationId == organizationId).Select(w => w.Id).ToListAsync(ct);
            var validRoles = workspaceRoles.Where(x => orgWorkspaceIds.Contains(x.WorkspaceId)).ToList();
            var currentWorkspaceIds = await _db.WorkspaceMembers.Where(m => m.UserId == userId && orgWorkspaceIds.Contains(m.WorkspaceId)).Select(m => m.WorkspaceId).ToListAsync(ct);
            var validIds = validRoles.Select(x => x.WorkspaceId).ToList();
            var toAdd = validRoles.Where(x => !currentWorkspaceIds.Contains(x.WorkspaceId)).ToList();
            var toRemove = currentWorkspaceIds.Except(validIds).ToList();
            if (toRemove.Any())
            {
                var toRemoveMembers = await _db.WorkspaceMembers.Where(m => m.UserId == userId && toRemove.Contains(m.WorkspaceId)).ToListAsync(ct);
                _db.WorkspaceMembers.RemoveRange(toRemoveMembers);
            }
            foreach (var wr in toAdd)
            {
                Guid? customId = wr.CustomRoleId;
                if ((customId == null || customId == Guid.Empty) && !Enum.TryParse<WorkspaceRole>(wr.Role, true, out _))
                {
                    var cwByName = await _db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Name.ToLower() == wr.Role.ToLower(), ct);
                    if (cwByName != null) customId = cwByName.Id;
                    else throw new ValidationException($"Custom role '{wr.Role}' not found");
                }
                if (customId != null && customId != Guid.Empty)
                {
                    var roleEnt = await _db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.Id == customId.Value && r.OrganizationId == organizationId, ct);
                    if (roleEnt == null) throw new ValidationException($"Custom role id {customId} not found");
                    _db.WorkspaceMembers.Add(new WorkspaceMember(wr.WorkspaceId, userId, WorkspaceRole.Member, customId));
                }
                else
                {
                    if (!Enum.TryParse<WorkspaceRole>(wr.Role, true, out var parsed)) throw new ValidationException($"Invalid role {wr.Role}");
                    _db.WorkspaceMembers.Add(new WorkspaceMember(wr.WorkspaceId, userId, parsed));
                }
            }
            // Update roles for existing that remain (if role changed)
            foreach (var wr in validRoles)
            {
                var existing = await _db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == wr.WorkspaceId && m.UserId == userId, ct);
                if (existing != null)
                {
                    Guid? customId = wr.CustomRoleId;
                    if ((customId == null || customId == Guid.Empty) && !Enum.TryParse<WorkspaceRole>(wr.Role, true, out _))
                    {
                        var cwByName = await _db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Name.ToLower() == wr.Role.ToLower(), ct);
                        if (cwByName != null) customId = cwByName.Id;
                    }
                    if (customId != null && customId != Guid.Empty)
                    {
                        if (existing.CustomRoleId != customId) existing.CustomRoleId = customId;
                    }
                    else if (Enum.TryParse<WorkspaceRole>(wr.Role, true, out var newRole) && existing.Role != newRole)
                    {
                        existing.Role = newRole;
                    }
                }
            }
            await _db.SaveChangesAsync(ct);
        }
        var memberInfo = await _db.WorkspaceMembers.Where(m => m.UserId == userId).Join(_db.Users, m => m.UserId, u => u.Id, (m,u) => new { m.WorkspaceId, m.Role, m.JoinedAt, u.FullName, u.Email, u.AvatarUrl }).FirstOrDefaultAsync(ct);
        var firstRole = workspaceRoles?.FirstOrDefault()?.Role ?? "Member";
        return new OrgMemberDto(userId, user.FullName, user.Email, user.AvatarUrl, memberInfo?.Role.ToString() ?? firstRole, memberInfo != null ? (int)memberInfo.Role : 0, memberInfo?.WorkspaceId ?? Guid.Empty, memberInfo?.JoinedAt ?? DateTime.UtcNow);
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
