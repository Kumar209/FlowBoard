using Microsoft.EntityFrameworkCore;
using File.Service.Application.Interfaces;
using SharedKernel;

namespace File.Service.Infrastructure.Helpers;

public static class PermissionHelper
{
    private class GuidRow { public Guid Value { get; set; } }

    public static async Task<bool> IsSuperAdminAsync(IApplicationDbContext db, Guid callerId, List<string> callerRoles, CancellationToken ct)
    {
        if (callerRoles.Contains(Roles.SuperAdmin)) return true;
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

    public static async Task<bool> IsOrgAdminAsync(IApplicationDbContext db, Guid orgId, Guid callerId, List<string> callerRoles, CancellationToken ct)
    {
        if (Roles.IsPrivilegedForManage(callerRoles)) return true;
        if (await IsSuperAdminAsync(db, callerId, callerRoles, ct)) return true;
        try
        {
            var isOwner = await db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[Organizations] WHERE Id = {0} AND OwnerId = {1}", orgId, callerId).FirstOrDefaultAsync(ct) > 0;
            if (isOwner) return true;
            var isOrgAdmin = await db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[OrganizationMembers] WHERE OrganizationId = {0} AND UserId = {1} AND Role = {2}", orgId, callerId, Roles.OrgAdminValue).FirstOrDefaultAsync(ct) > 0;
            if (isOrgAdmin) return true;
        }
        catch { }
        return false;
    }
}
