using Microsoft.EntityFrameworkCore;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;
using SharedKernel;

namespace Identity.Service.Infrastructure.Services;

public class OrganizationActivityService : IOrganizationActivityService
{
    private readonly IApplicationDbContext _db;
    public OrganizationActivityService(IApplicationDbContext db) => _db = db;

    private async Task<bool> CanViewAsync(Guid organizationId, Guid callerId, CancellationToken ct)
    {
        if (await _db.WorkspaceMembers.AnyAsync(m => m.UserId == callerId && m.Role == Roles.SuperAdminValue, ct)) return true;
        if (await _db.Organizations.AnyAsync(o => o.Id == organizationId && o.OwnerId == callerId, ct)) return true;
        if (await _db.OrganizationMembers.AnyAsync(m => m.OrganizationId == organizationId && m.UserId == callerId && m.Role == 2, ct)) return true;
        var customRoleIds = await _db.WorkspaceMembers.Where(m => m.UserId == callerId && m.CustomRoleId != null).Select(m => m.CustomRoleId!.Value).ToListAsync(ct);
        if (customRoleIds.Any())
        {
            var hasPerm = await _db.Permissions.Where(p => p.Key == "activity:view:org")
                .Join(_db.RolePermissions.Where(rp => customRoleIds.Contains(rp.RoleId)), p => p.Id, rp => rp.PermissionId, (p, rp) => p)
                .AnyAsync(ct);
            if (hasPerm) return true;
        }
        var isOrgAdmin = await _db.WorkspaceMembers.Where(m => m.UserId == callerId)
            .Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m, w) => m)
            .AnyAsync(m => m.Role == Roles.OrgAdminValue, ct);
        return isOrgAdmin;
    }

    public async Task<(List<OrganizationActivityDto> Items, int Total)> GetActivitiesAsync(Guid organizationId, int page, int pageSize, Guid callerId, bool includeProjects = false, CancellationToken ct = default)
    {
        if (!await _db.Organizations.AnyAsync(o => o.Id == organizationId, ct)) throw new NotFoundException("Organization not found");
        if (!await CanViewAsync(organizationId, callerId, ct)) throw new ForbiddenException("Forbidden - Need activity:view:org (OrgAdmin)");

        // 1. Org-level activities
        var orgQuery = _db.OrganizationActivities.Where(a => a.OrganizationId == organizationId);
        var orgItems = await orgQuery.OrderByDescending(a => a.OccurredOn).ToListAsync(ct);

        List<OrganizationActivityDto> combined = new();
        // Map org items with enrichment placeholders
        combined.AddRange(orgItems.Select(a => new OrganizationActivityDto(a.Id, a.OrganizationId, a.ActorUserId, a.Action, a.PayloadJson, a.OccurredOn, null)));

        // 2. Include project activities if requested (all projects in org)
        if (includeProjects)
        {
            try
            {
                var wsIds = await _db.Workspaces.Where(w => w.OrganizationId == organizationId).Select(w => w.Id).ToListAsync(ct);
                if (wsIds.Any())
                {
                    // Query [project].ActivityLogs where WorkspaceId in wsIds OR ProjectId in projects of those workspaces
                    var wsParam = string.Join(",", wsIds.Select(id => $"'{id}'"));
                    // Use LEFT JOIN to COALESCE null WorkspaceId (historic rows before fix) via Projects.WorkspaceId
                    var projectLogs = await _db.Database.SqlQueryRaw<ActivityLogRow>($"SELECT a.Id, a.ProjectId, COALESCE(a.WorkspaceId, p.WorkspaceId) as WorkspaceId, a.TaskId, a.ActorId as ActorUserId, a.Action, a.PayloadJson, a.OccurredAt as OccurredOn FROM [project].[ActivityLogs] a LEFT JOIN [project].[Projects] p ON p.Id = a.ProjectId WHERE a.WorkspaceId IN ({wsParam}) OR p.WorkspaceId IN ({wsParam}) OR a.ProjectId IN (SELECT Id FROM [project].[Projects] WHERE WorkspaceId IN ({wsParam})) ORDER BY a.OccurredAt DESC").ToListAsync(ct);
                    foreach (var p in projectLogs)
                    {
                        combined.Add(new OrganizationActivityDto(p.Id, organizationId, p.ActorUserId, p.Action, p.PayloadJson, p.OccurredOn, null, p.ProjectId, p.WorkspaceId, null, null, null, null));
                    }
                }
            }
            catch { }
        }

        // Sort combined by OccurredOn desc
        combined = combined.OrderByDescending(x => x.OccurredOn).ToList();
        var total = combined.Count;

        // Pagination in memory (since merged from two tables)
        var pageItems = combined.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        // Enrichment batch for page items only (to keep performant)
        if (pageItems.Any())
        {
            var actorIds = pageItems.Select(i => i.ActorUserId).Distinct().ToList();
            var projectIds = pageItems.Where(i => i.ProjectId.HasValue).Select(i => i.ProjectId!.Value).Distinct().ToList();
            var workspaceIds = pageItems.Where(i => i.WorkspaceId.HasValue).Select(i => i.WorkspaceId!.Value).Distinct().ToList();

            var userMap = new Dictionary<Guid, (string Name, string Email)>();
            var projectMap = new Dictionary<Guid, string>();
            var wsMap = new Dictionary<Guid, string>();
            var roleMap = new Dictionary<(Guid UserId, Guid WsId), string>();

            try
            {
                if (actorIds.Any())
                {
                    var ids = string.Join(",", actorIds.Select(id => $"'{id}'"));
                    var users = await _db.Database.SqlQueryRaw<UserRow>($"SELECT Id, FullName, Email FROM [identity].[Users] WHERE Id IN ({ids})").ToListAsync(ct);
                    foreach (var u in users) userMap[u.Id] = (u.FullName ?? "", u.Email ?? "");
                }
            }
            catch { }
            try
            {
                if (projectIds.Any())
                {
                    var ids = string.Join(",", projectIds.Select(id => $"'{id}'"));
                    var projs = await _db.Database.SqlQueryRaw<ProjectRow>($"SELECT Id, Name FROM [project].[Projects] WHERE Id IN ({ids})").ToListAsync(ct);
                    foreach (var p in projs) projectMap[p.Id] = p.Name;
                }
            }
            catch { }
            try
            {
                if (workspaceIds.Any())
                {
                    var ids = string.Join(",", workspaceIds.Select(id => $"'{id}'"));
                    var wss = await _db.Database.SqlQueryRaw<WorkspaceRow>($"SELECT Id, Name FROM [identity].[Workspaces] WHERE Id IN ({ids})").ToListAsync(ct);
                    foreach (var w in wss) wsMap[w.Id] = w.Name;
                }
            }
            catch { }
            try
            {
                if (actorIds.Any() && workspaceIds.Any())
                {
                    var userParam = string.Join(",", actorIds.Select(id => $"'{id}'"));
                    var wsParam = string.Join(",", workspaceIds.Select(id => $"'{id}'"));
                    var members = await _db.Database.SqlQueryRaw<WsMemberRow>($"SELECT WorkspaceId, UserId, CustomRoleId, Role FROM [identity].[WorkspaceMembers] WHERE UserId IN ({userParam}) AND WorkspaceId IN ({wsParam})").ToListAsync(ct);
                    var customIds = members.Where(m => m.CustomRoleId.HasValue).Select(m => m.CustomRoleId!.Value).Distinct().ToList();
                    var customMap = new Dictionary<Guid, string>();
                    if (customIds.Any())
                    {
                        var cids = string.Join(",", customIds.Select(id => $"'{id}'"));
                        var roles = await _db.Database.SqlQueryRaw<CustomRoleRow>($"SELECT Id, Name FROM [identity].[OrganizationWorkspaceRoles] WHERE Id IN ({cids})").ToListAsync(ct);
                        foreach (var r in roles) customMap[r.Id] = r.Name;
                    }
                    foreach (var m in members)
                    {
                        string roleName;
                        string? cnTemp = null; if (m.CustomRoleId.HasValue && customMap.TryGetValue(m.CustomRoleId.Value, out var cnVal)) cnTemp = cnVal; roleName = cnTemp ?? SharedKernel.Roles.GetLabel(m.Role); if (cnTemp != null) roleName = cnTemp;
                        else roleName = SharedKernel.Roles.GetLabel(m.Role);
                        roleMap[(m.UserId, m.WorkspaceId)] = roleName;
                    }
                }
            }
            catch { }

            // Fallback: for project logs with null WorkspaceId (old data before fix), derive WorkspaceId from ProjectId
            var fallbackProjectIds = pageItems.Where(i => !i.WorkspaceId.HasValue && i.ProjectId.HasValue && !projectMap.ContainsKey(i.ProjectId.Value)).Select(i => i.ProjectId!.Value).Distinct().ToList();
            // also need to handle null WorkspaceId but project exists in map → lookup workspace via Projects table
            var needFallbackWsLookup = pageItems.Where(i => !i.WorkspaceId.HasValue && i.ProjectId.HasValue).Select(i => i.ProjectId!.Value).Distinct().ToList();
            var fallbackWsMap = new Dictionary<Guid, Guid>();
            var fallbackWsNameMap = new Dictionary<Guid, string>();
            if (needFallbackWsLookup.Any())
            {
                try
                {
                    var pids = string.Join(",", needFallbackWsLookup.Select(id => $"'{id}'"));
                    var projWsRows = await _db.Database.SqlQueryRaw<ProjectWsRow>($"SELECT Id, WorkspaceId FROM [project].[Projects] WHERE Id IN ({pids})").ToListAsync(ct);
                    foreach (var r in projWsRows) fallbackWsMap[r.Id] = r.WorkspaceId;
                    var wsFallbackIds = projWsRows.Select(r => r.WorkspaceId).Distinct().ToList();
                    var missingWsIds = wsFallbackIds.Where(id => !wsMap.ContainsKey(id)).ToList();
                    if (missingWsIds.Any())
                    {
                        var ids = string.Join(",", missingWsIds.Select(id => $"'{id}'"));
                        var wss2 = await _db.Database.SqlQueryRaw<WorkspaceRow>($"SELECT Id, Name FROM [identity].[Workspaces] WHERE Id IN ({ids})").ToListAsync(ct);
                        foreach (var w in wss2) { wsMap[w.Id] = w.Name; fallbackWsNameMap[w.Id] = w.Name; }
                    }
                }
                catch { }
            }

            for (int i = 0; i < pageItems.Count; i++)
            {
                var it = pageItems[i];
                userMap.TryGetValue(it.ActorUserId, out var user);
                string projName = it.ProjectId.HasValue && projectMap.TryGetValue(it.ProjectId.Value, out var pn) ? pn : "";
                string wsName = it.WorkspaceId.HasValue && wsMap.TryGetValue(it.WorkspaceId.Value, out var wn) ? wn : "";
                Guid? effectiveWsId = it.WorkspaceId;
                if (!effectiveWsId.HasValue && it.ProjectId.HasValue && fallbackWsMap.TryGetValue(it.ProjectId.Value, out var fWs)) { effectiveWsId = fWs; if (wsMap.TryGetValue(fWs, out var fWn)) wsName = fWn; }
                string customRole = "";
                if (effectiveWsId.HasValue && roleMap.TryGetValue((it.ActorUserId, effectiveWsId.Value), out var cr)) customRole = cr;
                else
                {
                    var any = roleMap.FirstOrDefault(kv => kv.Key.UserId == it.ActorUserId);
                    if (!any.Equals(default(KeyValuePair<(Guid,Guid),string>))) customRole = any.Value;
                }
                // ensure dto has effective workspaceId even if originally null (for frontend display)
                var enriched = it with { ActorName = user.Name ?? it.ActorName, CallerEmail = user.Email, CustomRoleName = customRole, ProjectName = projName, WorkspaceName = wsName };
                if (!it.WorkspaceId.HasValue && effectiveWsId.HasValue) enriched = enriched with { WorkspaceId = effectiveWsId };
                pageItems[i] = enriched;
            }
        }

        return (pageItems, total);
    }

    public async Task LogAsync(Guid organizationId, Guid actorUserId, string action, string? payloadJson, CancellationToken ct = default)
    {
        var act = new OrganizationActivity(organizationId, actorUserId, action, payloadJson);
        _db.OrganizationActivities.Add(act);
        await _db.SaveChangesAsync(ct);
    }

    private class UserRow { public Guid Id { get; set; } public string? FullName { get; set; } public string? Email { get; set; } }
    private class ProjectRow { public Guid Id { get; set; } public string Name { get; set; } = ""; }
    private class ProjectWsRow { public Guid Id { get; set; } public Guid WorkspaceId { get; set; } }
    private class WorkspaceRow { public Guid Id { get; set; } public string Name { get; set; } = ""; }
    private class WsMemberRow { public Guid WorkspaceId { get; set; } public Guid UserId { get; set; } public Guid? CustomRoleId { get; set; } public int Role { get; set; } }
    private class CustomRoleRow { public Guid Id { get; set; } public string Name { get; set; } = ""; }
    private class ActivityLogRow { public Guid Id { get; set; } public Guid? ProjectId { get; set; } public Guid? WorkspaceId { get; set; } public Guid? TaskId { get; set; } public Guid ActorUserId { get; set; } public string Action { get; set; } = ""; public string? PayloadJson { get; set; } public DateTime OccurredOn { get; set; } }
}
