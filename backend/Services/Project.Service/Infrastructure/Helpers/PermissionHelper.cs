using Microsoft.EntityFrameworkCore;
using Project.Service.Application.Interfaces;
using SharedKernel;

namespace Project.Service.Infrastructure.Helpers;

public static class PermissionHelper
{
    private class GuidRow { public Guid Value { get; set; } }

    public static bool IsSuperAdminClaim(List<string> callerRoles)
        => callerRoles.Contains(Roles.SuperAdmin);

    public static async Task<bool> IsSuperAdminAsync(IApplicationDbContext db, Guid callerId, List<string> callerRoles, CancellationToken ct)
    {
        if (IsSuperAdminClaim(callerRoles)) return true;
        try
        {
            var isSuper = await db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[Users] WHERE Id = {0} AND IsSuperAdmin = 1", callerId).FirstOrDefaultAsync(ct) > 0;
            if (isSuper) return true;
        }
        catch { }
        try
        {
            var isSuperWs = await db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[WorkspaceMembers] WHERE UserId = {0} AND Role = {1}", callerId, Roles.SuperAdminValue).FirstOrDefaultAsync(ct) > 0;
            if (isSuperWs) return true;
        }
        catch { }
        return false;
    }

    public static async Task<bool> IsOrgAdminInOrgAsync(IApplicationDbContext db, Guid workspaceId, Guid callerId, List<string> callerRoles, CancellationToken ct)
    {
        if (Roles.IsPrivilegedForManage(callerRoles)) return true;
        if (await IsSuperAdminAsync(db, callerId, callerRoles, ct)) return true;
        try
        {
            var orgIdRow = await db.Database.SqlQueryRaw<GuidRow>("SELECT OrganizationId as Value FROM [identity].[Workspaces] WHERE Id = {0}", workspaceId).ToListAsync(ct);
            var orgId = orgIdRow.FirstOrDefault()?.Value ?? Guid.Empty;
            if (orgId == Guid.Empty) return false;
            var isOwner = await db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[Organizations] WHERE Id = {0} AND OwnerId = {1}", orgId, callerId).FirstOrDefaultAsync(ct) > 0;
            if (isOwner) return true;
            var isOrgAdmin = await db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[OrganizationMembers] WHERE OrganizationId = {0} AND UserId = {1} AND Role = {2}", orgId, callerId, Roles.OrgAdminValue).FirstOrDefaultAsync(ct) > 0;
            if (isOrgAdmin) return true;
        }
        catch { }
        return false;
    }

    public static async Task<bool> IsOrgAdminInOrgByProjectAsync(IApplicationDbContext db, Guid projectId, Guid callerId, List<string> callerRoles, CancellationToken ct)
    {
        var wsId = await db.Projects.Where(p => p.Id == projectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync(ct);
        if (wsId == Guid.Empty) return false;
        return await IsOrgAdminInOrgAsync(db, wsId, callerId, callerRoles, ct);
    }

    public static async Task<bool> HasCustomPermissionAsync(IApplicationDbContext db, Guid workspaceId, Guid callerId, string permKey, CancellationToken ct)
    {
        try
        {
            var customRoleId = await db.Database.SqlQueryRaw<Guid?>("SELECT CustomRoleId as Value FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0} AND UserId = {1}", workspaceId, callerId).FirstOrDefaultAsync(ct);
            if (customRoleId == null || customRoleId == Guid.Empty) return false;
            var permId = await db.Database.SqlQueryRaw<Guid>("SELECT Id as Value FROM [identity].[Permissions] WHERE [Key] = {0}", permKey).FirstOrDefaultAsync(ct);
            if (permId == Guid.Empty) return false;
            var has = await db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[RolePermissions] WHERE RoleId = {0} AND PermissionId = {1}", customRoleId.Value, permId).FirstOrDefaultAsync(ct) > 0;
            return has;
        }
        catch { return false; }
    }
}
