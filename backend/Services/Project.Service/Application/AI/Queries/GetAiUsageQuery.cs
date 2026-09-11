using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.AI.DTOs;
using Project.Service.Application.AI.Interfaces;
using Project.Service.Application.Interfaces;
using SharedKernel;

namespace Project.Service.Application.AI.Queries;

/// <summary>
/// GetAiUsage - 7.6 Org + 7.7 Project AI Usage. Org-level (orgId) OrgAdmin only, Project-level (projectId) all members (like Activity). Returns list of AiUsageLogDto ordered by CreatedAt desc, filtered by orgId/projectId/userId. RBAC via Roles.IsPrivilegedForManage or ProjectMember check. Enriched with CallerName/Email/CustomRole/ProjectName/WorkspaceName for UI paginator.
/// </summary>
public record GetAiUsageQuery(Guid? OrgId, Guid? ProjectId, Guid? UserId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<IReadOnlyList<AiUsageLogDto>>>;

public class GetAiUsageHandler : IRequestHandler<GetAiUsageQuery, Result<IReadOnlyList<AiUsageLogDto>>>
{
    private readonly IAiService _ai;
    private readonly IApplicationDbContext _db;
    public GetAiUsageHandler(IAiService ai, IApplicationDbContext db) { _ai = ai; _db = db; }

    public async Task<Result<IReadOnlyList<AiUsageLogDto>>> Handle(GetAiUsageQuery req, CancellationToken ct)
    {
        if (req.OrgId.HasValue && !req.ProjectId.HasValue)
        {
            if (!SharedKernel.Roles.IsPrivilegedForManage(req.CallerRoles))
            {
                try
                {
                    var isOrgAdmin = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM [identity].[OrganizationMembers] WHERE OrganizationId = @p0 AND UserId = @p1 AND Role = 2", req.OrgId.Value, req.CallerId).FirstOrDefaultAsync(ct);
                    if (isOrgAdmin == 0)
                    {
                        var isSuper = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM [identity].[WorkspaceMembers] WHERE WorkspaceId IN (SELECT Id FROM [identity].[Workspaces] WHERE OrganizationId = @p0) AND UserId = @p1 AND Role = 0", req.OrgId.Value, req.CallerId).FirstOrDefaultAsync(ct);
                        if (isSuper == 0) return Result<IReadOnlyList<AiUsageLogDto>>.Failure("Forbidden — Org AI Usage requires OrgAdmin/SuperAdmin");
                    }
                }
                catch { return Result<IReadOnlyList<AiUsageLogDto>>.Failure("Forbidden — Org AI Usage requires OrgAdmin"); }
            }
        }
        if (req.ProjectId.HasValue)
        {
            var proj = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == req.ProjectId.Value, ct);
            if (proj == null) return Result<IReadOnlyList<AiUsageLogDto>>.Failure("Project not found");
        }

        var list = await _ai.GetUsageAsync(req.OrgId, req.ProjectId, req.UserId, ct);
        var enriched = await EnrichAsync(list, ct);
        return Result<IReadOnlyList<AiUsageLogDto>>.Success(enriched);
    }

    private async Task<IReadOnlyList<AiUsageLogDto>> EnrichAsync(IReadOnlyList<AiUsageLogDto> logs, CancellationToken ct)
    {
        if (!logs.Any()) return logs;
        var userIds = logs.Select(l => l.UserId).Distinct().ToList();
        var projectIds = logs.Where(l => l.ProjectId.HasValue).Select(l => l.ProjectId!.Value).Distinct().ToList();
        var wsIds = logs.Where(l => l.WorkspaceId.HasValue).Select(l => l.WorkspaceId!.Value).Distinct().ToList();

        var userMap = new Dictionary<Guid, (string Name, string Email)>();
        var projectMap = new Dictionary<Guid, string>();
        var wsMap = new Dictionary<Guid, string>();
        var roleMap = new Dictionary<(Guid UserId, Guid WsId), string>();

        try
        {
            // Users
            if (userIds.Any())
            {
                var idsParam = string.Join(",", userIds.Select(id => $"'{id}'"));
                // Use SqlQueryRaw with IN clause via string interpolation (safe for Guid list, not user input)
                var users = await _db.Database.SqlQueryRaw<UserRow>($"SELECT Id, FullName, Email FROM [identity].[Users] WHERE Id IN ({idsParam})").ToListAsync(ct);
                foreach (var u in users) userMap[u.Id] = (u.FullName ?? "", u.Email ?? "");
            }
        }
        catch { }
        try
        {
            if (projectIds.Any())
            {
                var idsParam = string.Join(",", projectIds.Select(id => $"'{id}'"));
                var projs = await _db.Database.SqlQueryRaw<ProjectRow>($"SELECT Id, Name FROM [project].[Projects] WHERE Id IN ({idsParam})").ToListAsync(ct);
                foreach (var p in projs) projectMap[p.Id] = p.Name;
            }
        }
        catch { }
        try
        {
            if (wsIds.Any())
            {
                var idsParam = string.Join(",", wsIds.Select(id => $"'{id}'"));
                var wss = await _db.Database.SqlQueryRaw<WorkspaceRow>($"SELECT Id, Name FROM [identity].[Workspaces] WHERE Id IN ({idsParam})").ToListAsync(ct);
                foreach (var w in wss) wsMap[w.Id] = w.Name;
            }
        }
        catch { }
        try
        {
            // Custom roles per workspace
            if (userIds.Any() && wsIds.Any())
            {
                var userParam = string.Join(",", userIds.Select(id => $"'{id}'"));
                var wsParam = string.Join(",", wsIds.Select(id => $"'{id}'"));
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
                    if (m.CustomRoleId.HasValue && customMap.TryGetValue(m.CustomRoleId.Value, out var cn)) roleName = cn;
                    else roleName = SharedKernel.Roles.GetLabel(m.Role);
                    roleMap[(m.UserId, m.WorkspaceId)] = roleName;
                }
            }
        }
        catch { }

        var enriched = logs.Select(l =>
        {
            userMap.TryGetValue(l.UserId, out var user);
            string projName = l.ProjectId.HasValue && projectMap.TryGetValue(l.ProjectId.Value, out var pn) ? pn : "";
            string wsName = l.WorkspaceId.HasValue && wsMap.TryGetValue(l.WorkspaceId.Value, out var wn) ? wn : "";
            string customRole = "";
            if (l.WorkspaceId.HasValue && roleMap.TryGetValue((l.UserId, l.WorkspaceId.Value), out var cr)) customRole = cr;
            else if (userMap.ContainsKey(l.UserId))
            {
                // fallback to org role? try to find any custom role for user in that org's workspaces
                var any = roleMap.FirstOrDefault(kv => kv.Key.UserId == l.UserId);
                if (!any.Equals(default(KeyValuePair<(Guid,Guid),string>))) customRole = any.Value;
            }
            return l with { CallerName = user.Name, CallerEmail = user.Email, CustomRoleName = customRole, ProjectName = projName, WorkspaceName = wsName };
        }).ToList();
        return enriched;
    }

    private class UserRow { public Guid Id { get; set; } public string? FullName { get; set; } public string? Email { get; set; } }
    private class ProjectRow { public Guid Id { get; set; } public string Name { get; set; } = ""; }
    private class WorkspaceRow { public Guid Id { get; set; } public string Name { get; set; } = ""; }
    private class WsMemberRow { public Guid WorkspaceId { get; set; } public Guid UserId { get; set; } public Guid? CustomRoleId { get; set; } public int Role { get; set; } }
    private class CustomRoleRow { public Guid Id { get; set; } public string Name { get; set; } = ""; }
}

public record GetAiUsageSummaryQuery(Guid? OrgId, Guid? ProjectId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<IReadOnlyList<AiUsageSummaryDto>>>;

public class GetAiUsageSummaryHandler : IRequestHandler<GetAiUsageSummaryQuery, Result<IReadOnlyList<AiUsageSummaryDto>>>
{
    private readonly IAiService _ai;
    private readonly IApplicationDbContext _db;
    public GetAiUsageSummaryHandler(IAiService ai, IApplicationDbContext db) { _ai = ai; _db = db; }

    public async Task<Result<IReadOnlyList<AiUsageSummaryDto>>> Handle(GetAiUsageSummaryQuery req, CancellationToken ct)
    {
        if (req.OrgId.HasValue && !req.ProjectId.HasValue)
        {
            if (!SharedKernel.Roles.IsPrivilegedForManage(req.CallerRoles))
            {
                try
                {
                    var isOrgAdmin = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM [identity].[OrganizationMembers] WHERE OrganizationId = @p0 AND UserId = @p1 AND Role = 2", req.OrgId.Value, req.CallerId).FirstOrDefaultAsync(ct);
                    if (isOrgAdmin == 0) return Result<IReadOnlyList<AiUsageSummaryDto>>.Failure("Forbidden — Org AI Usage requires OrgAdmin");
                }
                catch { return Result<IReadOnlyList<AiUsageSummaryDto>>.Failure("Forbidden"); }
            }
        }
        var list = await _ai.GetUsageSummaryAsync(req.OrgId, req.ProjectId, ct);
        return Result<IReadOnlyList<AiUsageSummaryDto>>.Success(list);
    }
}
