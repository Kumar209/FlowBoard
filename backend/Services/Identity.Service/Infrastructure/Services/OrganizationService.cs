using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;
using SharedKernel;

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
        var isSuper = await _db.WorkspaceMembers.Where(m => m.UserId == callerId && m.Role == Roles.SuperAdminValue).AnyAsync(ct);
        var isOrgAdmin = isSuper || await _db.WorkspaceMembers.Where(m => m.UserId == callerId).Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m,w) => m).AnyAsync(m => m.Role == Roles.OrgAdminValue || m.Role == Roles.SuperAdminValue, ct);
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
        var isSuper = await _db.WorkspaceMembers.Where(m => m.UserId == callerId && m.Role == Roles.SuperAdminValue).AnyAsync(ct);
        var isOrgAdmin = isSuper || await _db.WorkspaceMembers.Where(m => m.UserId == callerId).Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m,w) => m).AnyAsync(m => m.Role == Roles.OrgAdminValue || m.Role == Roles.SuperAdminValue, ct);
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
        var orgMemberMap = await _db.OrganizationMembers.Where(m => m.OrganizationId == organizationId).ToDictionaryAsync(m => m.UserId, m => m.Role, ct);
        var customRoleMap = await _db.OrganizationWorkspaceRoles.Where(r => r.OrganizationId == organizationId).ToDictionaryAsync(r => r.Id, r => r.Name, ct);
        var members = await _db.WorkspaceMembers.Where(m => workspaceIds.Contains(m.WorkspaceId))
            .Join(_db.Users, m => m.UserId, u => u.Id, (m,u) => new { m.WorkspaceId, m.UserId, m.Role, m.CustomRoleId, m.JoinedAt, u.FullName, u.Email, u.AvatarUrl })
            .ToListAsync(ct);
        // Group by UserId and collect all workspaces for that user — Role is org-level from OrganizationMembers, not workspace Role
        var grouped = members.GroupBy(x => x.UserId).Select(g => {
            var first = g.First();
            var allWorkspaceIds = g.Select(x => x.WorkspaceId).Distinct().ToList();
            var allWorkspaceNames = allWorkspaceIds.Select(id => workspaceMap.TryGetValue(id, out var name) ? name : id.ToString()[..6]).ToList();
            var orgRoleInt = orgMemberMap.TryGetValue(g.Key, out var r) ? r : Roles.MemberValue;
            var roleStr = Roles.GetLabel(orgRoleInt);
            var wsRoleMap = g.ToDictionary(x => x.WorkspaceId.ToString(), x => x.CustomRoleId.HasValue && customRoleMap.TryGetValue(x.CustomRoleId.Value, out var crn) ? crn : Roles.GetLabel(x.Role));
            return new OrgMemberDto(first.UserId, first.FullName, first.Email, first.AvatarUrl, roleStr, orgRoleInt, first.WorkspaceId, first.JoinedAt, allWorkspaceNames, allWorkspaceIds, wsRoleMap);
        }).ToList();
        // Include org-only members (OrganizationMembers without any WorkspaceMember) - org level Member/OrgAdmin/Client
        var groupedIds = grouped.Select(g => g.UserId).ToHashSet();
        var orgOnlyMembers = await _db.OrganizationMembers.Where(m => m.OrganizationId == organizationId && !groupedIds.Contains(m.UserId))
            .Join(_db.Users, m => m.UserId, u => u.Id, (m,u) => new { m.UserId, m.Role, u.FullName, u.Email, u.AvatarUrl, u.CreatedAt })
            .ToListAsync(ct);
        foreach (var om in orgOnlyMembers)
        {
            string roleStr = Roles.GetLabel(om.Role);
            grouped.Add(new OrgMemberDto(om.UserId, om.FullName, om.Email, om.AvatarUrl, roleStr, om.Role, Guid.Empty, om.CreatedAt, new List<string>(), new List<Guid>()));
        }
        // Also include Organization Owner if not already
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct);
        if (org != null && !grouped.Any(g => g.UserId == org.OwnerId) && !groupedIds.Contains(org.OwnerId))
        {
            var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == org.OwnerId, ct);
            if (owner != null) grouped.Add(new OrgMemberDto(owner.Id, owner.FullName, owner.Email, owner.AvatarUrl, Roles.OrgAdmin, Roles.OrgAdminValue, Guid.Empty, owner.CreatedAt, new List<string>(), new List<Guid>()));
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
        var isOrgAdmin = await _db.WorkspaceMembers.Where(m => m.UserId == callerId).Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m,w) => m).AnyAsync(m => m.Role == Roles.OrgAdminValue || m.Role == Roles.SuperAdminValue, ct);
        if (!isOwner && !isOrgAdmin) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(password)) throw new ValidationException("FullName, Email, Password required");
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower(), ct)) throw new ValidationException("Email already exists");
        var user = new User(email.ToLowerInvariant(), BCrypt.Net.BCrypt.HashPassword(password), fullName);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        // Organization-level member (3 roles: Member 1, OrgAdmin 2, Client 3, SuperAdmin 0)
        int orgRoleInt = Roles.MemberValue;
        if (!string.IsNullOrWhiteSpace(orgRole))
        {
            var nr = orgRole.Trim();
            if (nr.Equals(Roles.OrgAdmin, StringComparison.OrdinalIgnoreCase)) orgRoleInt = Roles.OrgAdminValue;
            else if (nr.Equals(Roles.Client, StringComparison.OrdinalIgnoreCase)) orgRoleInt = Roles.ClientValue;
            else if (nr.Equals(Roles.Member, StringComparison.OrdinalIgnoreCase)) orgRoleInt = Roles.MemberValue;
            else if (nr.Equals(Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase)) orgRoleInt = Roles.SuperAdminValue;
            else throw new ValidationException($"Invalid org role {orgRole} - allowed {Roles.Member}/{Roles.OrgAdmin}/{Roles.Client}");
        }
        _db.OrganizationMembers.Add(new OrganizationMember(organizationId, user.Id, orgRoleInt));
        await _db.SaveChangesAsync(ct);
        var targetRoles = workspaceRoles ?? new List<WorkspaceRoleAssignment>();
        // No auto-assign when no workspace selected - org-level only is allowed (workspace assignment requires custom role)
        foreach (var wr in targetRoles.Where(x => x.WorkspaceId != Guid.Empty))
        {
            Guid? customId = wr.CustomRoleId;
            if (customId == null || customId == Guid.Empty)
            {
                var cwByName = await _db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Name.ToLower() == wr.Role.ToLower(), ct);
                if (cwByName != null) customId = cwByName.Id;
                else throw new ValidationException($"Custom role '{wr.Role}' not found in organization - create it in Roles first");
            }
            var roleEnt2 = await _db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.Id == customId.Value && r.OrganizationId == organizationId, ct);
            if (roleEnt2 == null) throw new ValidationException($"Custom role id {customId} not found");
            _db.WorkspaceMembers.Add(new WorkspaceMember(wr.WorkspaceId, user.Id, Roles.MemberValue, customId));
        }
        await _db.SaveChangesAsync(ct);
        // Org Activity audit - MemberAdded
        try { _db.OrganizationActivities.Add(new OrganizationActivity(organizationId, callerId, "MemberAdded", JsonSerializer.Serialize(new { userId = user.Id, email = user.Email, fullName = user.FullName, orgRole = orgRoleInt, workspaces = targetRoles.Select(r => r.WorkspaceId).ToArray() }))); await _db.SaveChangesAsync(ct); } catch { }
        var firstRole = targetRoles.FirstOrDefault()?.Role ?? (orgRole ?? "Member");
        var firstWid = targetRoles.FirstOrDefault()?.WorkspaceId ?? Guid.Empty;
        // Map org role to display if no workspace
        string displayRole = targetRoles.Any() ? firstRole : (orgRole ?? Roles.Member);
        int firstParsedInt = Roles.MemberValue;
        // keep custom name as display; parse org role if needed
        if (displayRole.Equals(Roles.OrgAdmin, StringComparison.OrdinalIgnoreCase)) firstParsedInt = Roles.OrgAdminValue;
        else if (displayRole.Equals(Roles.Client, StringComparison.OrdinalIgnoreCase)) firstParsedInt = Roles.ClientValue;
        else firstParsedInt = Roles.MemberValue;
        return new OrgMemberDto(user.Id, user.FullName, user.Email, user.AvatarUrl, displayRole, firstParsedInt, firstWid, DateTime.UtcNow);
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
        var isOrgAdmin = await _db.WorkspaceMembers.Where(m => m.UserId == callerId).Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m,w) => m).AnyAsync(m => m.Role == Roles.OrgAdminValue || m.Role == Roles.SuperAdminValue, ct);
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
        // Org-level role update (Member 1 / OrgAdmin 2 / Client 3)
        if (!string.IsNullOrWhiteSpace(orgRole))
        {
            int orgRoleInt = Roles.MemberValue;
            var nr = orgRole.Trim();
            if (nr.Equals(Roles.OrgAdmin, StringComparison.OrdinalIgnoreCase)) orgRoleInt = Roles.OrgAdminValue;
            else if (nr.Equals(Roles.Client, StringComparison.OrdinalIgnoreCase)) orgRoleInt = Roles.ClientValue;
            else if (nr.Equals(Roles.Member, StringComparison.OrdinalIgnoreCase)) orgRoleInt = Roles.MemberValue;
            else if (nr.Equals(Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase)) orgRoleInt = Roles.SuperAdminValue;
            else throw new ValidationException($"Invalid org role {orgRole} - allowed {Roles.Member}/{Roles.OrgAdmin}/{Roles.Client}");
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
                if (customId == null || customId == Guid.Empty)
                {
                    var cwByName = await _db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Name.ToLower() == wr.Role.ToLower(), ct);
                    if (cwByName != null) customId = cwByName.Id;
                    else throw new ValidationException($"Custom role '{wr.Role}' not found - create it in Roles first");
                }
                var roleEntAdd = await _db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.Id == customId.Value && r.OrganizationId == organizationId, ct);
                if (roleEntAdd == null) throw new ValidationException($"Custom role id {customId} not found");
                _db.WorkspaceMembers.Add(new WorkspaceMember(wr.WorkspaceId, userId, Roles.MemberValue, customId));
            }
            // Update roles for existing that remain (if role changed)
            foreach (var wr in validRoles)
            {
                var existing = await _db.WorkspaceMembers.FirstOrDefaultAsync(m => m.WorkspaceId == wr.WorkspaceId && m.UserId == userId, ct);
                if (existing != null)
                {
                    Guid? customId = wr.CustomRoleId;
                    if (customId == null || customId == Guid.Empty)
                    {
                        var cwByName = await _db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Name.ToLower() == wr.Role.ToLower(), ct);
                        if (cwByName != null) customId = cwByName.Id;
                    }
                    if (customId != null && customId != Guid.Empty && existing.CustomRoleId != customId)
                    {
                        existing.CustomRoleId = customId;
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
        var isOrgAdmin = await _db.WorkspaceMembers.Where(m => m.UserId == callerId).Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m,w) => m).AnyAsync(m => m.Role == Roles.OrgAdminValue || m.Role == Roles.SuperAdminValue, ct);
        if (!isOwner && !isOrgAdmin) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        if (userId == org.OwnerId) throw new ValidationException("Cannot remove organization owner");
        var workspaceIds = await _db.Workspaces.Where(w => w.OrganizationId == organizationId).Select(w => w.Id).ToListAsync(ct);
        var memberships = await _db.WorkspaceMembers.Where(m => m.UserId == userId && workspaceIds.Contains(m.WorkspaceId)).ToListAsync(ct);
        _db.WorkspaceMembers.RemoveRange(memberships);
        await _db.SaveChangesAsync(ct);
    }
}
