using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Service.Infrastructure.Services;

public class OrganizationRoleService : IOrganizationRoleService
{
    private readonly IApplicationDbContext _db;
    public OrganizationRoleService(IApplicationDbContext db) => _db = db;

    private async Task<bool> IsOrgAdminAsync(Guid organizationId, Guid callerId, CancellationToken ct)
    {
        if (await _db.WorkspaceMembers.AnyAsync(m => m.UserId == callerId && m.Role == Domain.Enums.WorkspaceRole.SuperAdmin, ct)) return true;
        return await _db.OrganizationMembers.AnyAsync(m => m.OrganizationId == organizationId && m.UserId == callerId && m.Role == 2, ct)
            || await _db.Organizations.AnyAsync(o => o.Id == organizationId && o.OwnerId == callerId, ct);
    }

    public async Task<List<OrganizationRoleDto>> GetRolesAsync(Guid organizationId, Guid callerId, CancellationToken ct = default)
    {
        if (!await _db.Organizations.AnyAsync(o => o.Id == organizationId, ct)) throw new NotFoundException("Organization not found");
        // Any member of org can view roles (role:view), but we check org membership
        var isMember = await _db.OrganizationMembers.AnyAsync(m => m.OrganizationId == organizationId && m.UserId == callerId, ct)
            || await _db.WorkspaceMembers.AnyAsync(m => m.UserId == callerId && m.Workspace!.OrganizationId == organizationId, ct);
        var isSuper = await _db.WorkspaceMembers.AnyAsync(m => m.UserId == callerId && m.Role == Domain.Enums.WorkspaceRole.SuperAdmin, ct);
        if (!isMember && !isSuper) throw new ForbiddenException("Forbidden - Not a member of organization");
        var roles = await _db.OrganizationWorkspaceRoles.Where(r => r.OrganizationId == organizationId).OrderBy(r => r.Name).ToListAsync(ct);
        var counts = await _db.WorkspaceMembers.Where(m => m.CustomRoleId != null).GroupBy(m => m.CustomRoleId).ToDictionaryAsync(g => g.Key!.Value, g => g.Count(), ct);
        var permCounts = await _db.RolePermissions.GroupBy(rp => rp.RoleId).ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
        return roles.Select(r => new OrganizationRoleDto(r.Id, r.OrganizationId, r.Name, r.Description, r.CreatedBy, r.CreatedAt, counts.TryGetValue(r.Id, out var c) ? c : 0, permCounts.TryGetValue(r.Id, out var pc) ? pc : 0)).ToList();
    }

    public async Task<OrganizationRoleDto> CreateRoleAsync(Guid organizationId, string name, string? description, Guid callerId, CancellationToken ct = default)
    {
        if (!await IsOrgAdminAsync(organizationId, callerId, ct)) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Name required");
        if (await _db.OrganizationWorkspaceRoles.AnyAsync(r => r.OrganizationId == organizationId && r.Name.ToLower() == name.ToLower(), ct)) throw new ValidationException("Role name already exists in this organization");
        var role = new OrganizationWorkspaceRole(organizationId, name.Trim(), description?.Trim(), callerId);
        _db.OrganizationWorkspaceRoles.Add(role);
        await _db.SaveChangesAsync(ct);
        return new OrganizationRoleDto(role.Id, role.OrganizationId, role.Name, role.Description, role.CreatedBy, role.CreatedAt, 0, 0);
    }

    public async Task<OrganizationRoleDto> UpdateRoleAsync(Guid organizationId, Guid roleId, string name, string? description, Guid callerId, CancellationToken ct = default)
    {
        if (!await IsOrgAdminAsync(organizationId, callerId, ct)) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        var role = await _db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.Id == roleId && r.OrganizationId == organizationId, ct);
        if (role == null) throw new NotFoundException("Role not found");
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Name required");
        if (name.ToLower() != role.Name.ToLower() && await _db.OrganizationWorkspaceRoles.AnyAsync(r => r.OrganizationId == organizationId && r.Name.ToLower() == name.ToLower() && r.Id != roleId, ct)) throw new ValidationException("Role name already exists");
        role.Update(name.Trim(), description?.Trim());
        await _db.SaveChangesAsync(ct);
        var memberCount = await _db.WorkspaceMembers.CountAsync(m => m.CustomRoleId == roleId, ct);
        var permCount = await _db.RolePermissions.CountAsync(rp => rp.RoleId == roleId, ct);
        return new OrganizationRoleDto(role.Id, role.OrganizationId, role.Name, role.Description, role.CreatedBy, role.CreatedAt, memberCount, permCount);
    }

    public async Task DeleteRoleAsync(Guid organizationId, Guid roleId, Guid callerId, CancellationToken ct = default)
    {
        if (!await IsOrgAdminAsync(organizationId, callerId, ct)) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        var role = await _db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.Id == roleId && r.OrganizationId == organizationId, ct);
        if (role == null) throw new NotFoundException("Role not found");
        var inUse = await _db.WorkspaceMembers.AnyAsync(m => m.CustomRoleId == roleId, ct);
        if (inUse) throw new ValidationException("Cannot delete role in use by workspace members - reassign members first");
        var perms = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(ct);
        _db.RolePermissions.RemoveRange(perms);
        _db.OrganizationWorkspaceRoles.Remove(role);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<RoleWithPermissionsDto> GetRoleWithPermissionsAsync(Guid organizationId, Guid roleId, Guid callerId, CancellationToken ct = default)
    {
        var role = await _db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.Id == roleId && r.OrganizationId == organizationId, ct);
        if (role == null) throw new NotFoundException("Role not found");
        var memberCount = await _db.WorkspaceMembers.CountAsync(m => m.CustomRoleId == roleId, ct);
        var permCount = await _db.RolePermissions.CountAsync(rp => rp.RoleId == roleId, ct);
        var dto = new OrganizationRoleDto(role.Id, role.OrganizationId, role.Name, role.Description, role.CreatedBy, role.CreatedAt, memberCount, permCount);
        var permIds = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).Select(rp => rp.PermissionId).ToListAsync(ct);
        var perms = await _db.Permissions.Where(p => permIds.Contains(p.Id)).Select(p => new PermissionDto(p.Id, p.Key, p.Name, p.Group, p.Description)).ToListAsync(ct);
        return new RoleWithPermissionsDto(dto, permIds, perms);
    }

    public async Task<RoleWithPermissionsDto> UpdateRolePermissionsAsync(Guid organizationId, Guid roleId, List<Guid> permissionIds, Guid callerId, CancellationToken ct = default)
    {
        if (!await IsOrgAdminAsync(organizationId, callerId, ct)) throw new ForbiddenException("Forbidden - Need OrgAdmin");
        var role = await _db.OrganizationWorkspaceRoles.FirstOrDefaultAsync(r => r.Id == roleId && r.OrganizationId == organizationId, ct);
        if (role == null) throw new NotFoundException("Role not found");
        var validIds = await _db.Permissions.Where(p => permissionIds.Contains(p.Id)).Select(p => p.Id).ToListAsync(ct);
        if (validIds.Count != permissionIds.Count) throw new ValidationException("Some permissionIds are invalid");
        var existing = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(ct);
        var toRemove = existing.Where(rp => !validIds.Contains(rp.PermissionId)).ToList();
        var toAdd = validIds.Where(id => !existing.Any(rp => rp.PermissionId == id)).Select(id => new RolePermission(roleId, id)).ToList();
        _db.RolePermissions.RemoveRange(toRemove);
        _db.RolePermissions.AddRange(toAdd);
        await _db.SaveChangesAsync(ct);
        return await GetRoleWithPermissionsAsync(organizationId, roleId, callerId, ct);
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken ct = default)
    {
        return await _db.Permissions.OrderBy(p => p.Group).ThenBy(p => p.Key).Select(p => new PermissionDto(p.Id, p.Key, p.Name, p.Group, p.Description)).ToListAsync(ct);
    }
}
