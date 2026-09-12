using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Identity.Service.Application.Interfaces;
using SharedKernel;

namespace Identity.Service.Infrastructure.Services;

public class OrganizationStatsService : IOrganizationStatsService
{
    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OrganizationStatsService> _logger;
    public OrganizationStatsService(IApplicationDbContext db, IMemoryCache cache, ILogger<OrganizationStatsService> logger)
    {
        _db = db; _cache = cache; _logger = logger;
    }

    private async Task<bool> CanViewAsync(Guid organizationId, Guid callerId, CancellationToken ct)
    {
        if (await _db.WorkspaceMembers.AnyAsync(m => m.UserId == callerId && m.Role == Roles.SuperAdminValue, ct)) return true;
        if (await _db.Organizations.AnyAsync(o => o.Id == organizationId && o.OwnerId == callerId, ct)) return true;
        if (await _db.OrganizationMembers.AnyAsync(m => m.OrganizationId == organizationId && m.UserId == callerId, ct)) return true;
        var isMember = await _db.WorkspaceMembers.Where(m => m.UserId == callerId)
            .Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m, w) => m)
            .AnyAsync(ct);
        return isMember;
    }

    public async Task<OrgStatsDto> GetOrgStatsAsync(Guid organizationId, Guid callerId, CancellationToken ct = default)
    {
        if (!await _db.Organizations.AnyAsync(o => o.Id == organizationId, ct))
            throw new NotFoundException("Organization not found");
        if (!await CanViewAsync(organizationId, callerId, ct))
            throw new ForbiddenException("Forbidden - Need organization membership");

        var cacheKey = $"org:stats:{organizationId}";
        if (_cache.TryGetValue<OrgStatsDto>(cacheKey, out var cached) && cached != null) return cached;

        try
        {
            var orgIdStr = organizationId.ToString();

            var totalWorkspaces = await _db.Workspaces.CountAsync(w => w.OrganizationId == organizationId, ct);

            var totalProjects = await QueryCountAsync($"SELECT COUNT(*) as Value FROM [project].[Projects] p INNER JOIN [identity].[Workspaces] w ON w.Id = p.WorkspaceId WHERE w.OrganizationId = '{orgIdStr}'", ct);
            var totalMembers = await QueryCountAsync($"SELECT COUNT(DISTINCT UserId) as Value FROM [identity].[WorkspaceMembers] WHERE WorkspaceId IN (SELECT Id FROM [identity].[Workspaces] WHERE OrganizationId = '{orgIdStr}')", ct);
            var orgMembersDistinct = await _db.OrganizationMembers.CountAsync(m => m.OrganizationId == organizationId, ct);
            var members = Math.Max(totalMembers, orgMembersDistinct);
            try
            {
                var distinctUsers = await _db.Database.SqlQueryRaw<CountRow>($"SELECT COUNT(*) as Value FROM (SELECT UserId FROM [identity].[WorkspaceMembers] WHERE WorkspaceId IN (SELECT Id FROM [identity].[Workspaces] WHERE OrganizationId = '{orgIdStr}') UNION SELECT UserId FROM [identity].[OrganizationMembers] WHERE OrganizationId = '{orgIdStr}') as u").ToListAsync(ct);
                if (distinctUsers.Any()) members = distinctUsers.First().Value;
            }
            catch { }

            var totalIssues = await QueryCountAsync($"SELECT COUNT(*) as Value FROM [project].[Tasks] t INNER JOIN [project].[Projects] p ON p.Id = t.ProjectId INNER JOIN [identity].[Workspaces] w ON w.Id = p.WorkspaceId WHERE w.OrganizationId = '{orgIdStr}'", ct);
            var activeSprints = await QueryCountAsync($"SELECT COUNT(*) as Value FROM [project].[Sprints] s INNER JOIN [project].[Projects] p ON p.Id = s.ProjectId INNER JOIN [identity].[Workspaces] w ON w.Id = p.WorkspaceId WHERE w.OrganizationId = '{orgIdStr}' AND s.Status = 'Active'", ct);
            var completedIssues = await QueryCountAsync($"SELECT COUNT(*) as Value FROM [project].[Tasks] t INNER JOIN [project].[Projects] p ON p.Id = t.ProjectId INNER JOIN [identity].[Workspaces] w ON w.Id = p.WorkspaceId WHERE w.OrganizationId = '{orgIdStr}' AND t.Status = 'Done'", ct);

            var dto = new OrgStatsDto(totalWorkspaces, totalProjects, members, totalIssues, activeSprints, completedIssues);
            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(2));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrgStats failed orgId {OrgId}", organizationId);
            throw;
        }
    }

    public async Task<OrgChartDataDto> GetOrgChartDataAsync(Guid organizationId, Guid callerId, CancellationToken ct = default)
    {
        if (!await _db.Organizations.AnyAsync(o => o.Id == organizationId, ct))
            throw new NotFoundException("Organization not found");
        if (!await CanViewAsync(organizationId, callerId, ct))
            throw new ForbiddenException("Forbidden - Need organization membership");

        var cacheKey = $"org:chart:{organizationId}";
        if (_cache.TryGetValue<OrgChartDataDto>(cacheKey, out var cached) && cached != null) return cached;

        try
        {
            var orgIdStr = organizationId.ToString();

            var statusRows = await _db.Database.SqlQueryRaw<BucketRow>($"SELECT t.Status as Label, COUNT(*) as Value FROM [project].[Tasks] t INNER JOIN [project].[Projects] p ON p.Id = t.ProjectId INNER JOIN [identity].[Workspaces] w ON w.Id = p.WorkspaceId WHERE w.OrganizationId = '{orgIdStr}' GROUP BY t.Status").ToListAsync(ct);
            var issuesByStatus = statusRows.Select(r => new ChartBucketDto(r.Label ?? "Unknown", r.Value)).ToList();

            var priorityRows = await _db.Database.SqlQueryRaw<BucketRowInt>($"SELECT CAST(t.Priority as int) as LabelInt, COUNT(*) as Value FROM [project].[Tasks] t INNER JOIN [project].[Projects] p ON p.Id = t.ProjectId INNER JOIN [identity].[Workspaces] w ON w.Id = p.WorkspaceId WHERE w.OrganizationId = '{orgIdStr}' GROUP BY t.Priority").ToListAsync(ct);
            var prioMap = new Dictionary<int, string> { {0,"Low"}, {1,"Medium"}, {2,"High"}, {3,"Urgent"} };
            var issuesByPriority = priorityRows.Select(r => new ChartBucketDto(prioMap.TryGetValue(r.LabelInt, out var n) ? n : r.LabelInt.ToString(), r.Value)).ToList();

            var wsRows = await _db.Database.SqlQueryRaw<BucketRow>($"SELECT w.Name as Label, COUNT(*) as Value FROM [project].[Tasks] t INNER JOIN [project].[Projects] p ON p.Id = t.ProjectId INNER JOIN [identity].[Workspaces] w ON w.Id = p.WorkspaceId WHERE w.OrganizationId = '{orgIdStr}' GROUP BY w.Name").ToListAsync(ct);
            var issuesByWorkspace = wsRows.Select(r => new ChartBucketDto(r.Label ?? "Unknown", r.Value)).ToList();

            var activityTrend = new List<DailyCountDto>();
            try
            {
                var trendRows = await _db.Database.SqlQueryRaw<DailyRow>(
                    $"SELECT CAST(OccurredAt as date) as Day, COUNT(*) as Value FROM [project].[ActivityLogs] a INNER JOIN [project].[Projects] p ON p.Id = a.ProjectId INNER JOIN [identity].[Workspaces] w ON w.Id = p.WorkspaceId WHERE w.OrganizationId = '{orgIdStr}' AND a.OccurredAt >= DATEADD(day, -14, GETUTCDATE()) GROUP BY CAST(OccurredAt as date) UNION ALL SELECT CAST(OccurredOn as date) as Day, COUNT(*) as Value FROM [identity].[OrganizationActivities] WHERE OrganizationId = '{orgIdStr}' AND OccurredOn >= DATEADD(day, -14, GETUTCDATE()) GROUP BY CAST(OccurredOn as date)"
                ).ToListAsync(ct);
                activityTrend = trendRows.GroupBy(r => r.Day)
                    .Select(g => new DailyCountDto(g.Key.ToString("yyyy-MM-dd"), g.Sum(x => x.Value)))
                    .OrderBy(x => x.Date).ToList();
                var last14 = Enumerable.Range(0, 14).Select(i => DateTime.UtcNow.Date.AddDays(-13 + i).ToString("yyyy-MM-dd")).ToList();
                var dict = activityTrend.ToDictionary(x => x.Date, x => x.Count);
                activityTrend = last14.Select(d => new DailyCountDto(d, dict.TryGetValue(d, out var c) ? c : 0)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ActivityTrend query failed");
            }

            var aiUsage = new List<AiUsageDailyDto>();
            try
            {
                var aiRows = await _db.Database.SqlQueryRaw<AiDailyRow>(
                    $"SELECT CAST(CreatedAt as date) as Day, SUM(CAST(TotalTokens as bigint)) as Tokens, SUM(CAST(Cost as decimal(18,6))) as Cost, COUNT(*) as Requests FROM [project].[AiUsageLogs] WHERE OrgId = '{orgIdStr}' AND CreatedAt >= DATEADD(day, -14, GETUTCDATE()) GROUP BY CAST(CreatedAt as date) ORDER BY Day"
                ).ToListAsync(ct);
                aiUsage = aiRows.Select(r => new AiUsageDailyDto(r.Day.ToString("yyyy-MM-dd"), r.Tokens ?? 0, r.Cost ?? 0m, r.Requests)).ToList();
                var last14 = Enumerable.Range(0, 14).Select(i => DateTime.UtcNow.Date.AddDays(-13 + i).ToString("yyyy-MM-dd")).ToList();
                var dict = aiUsage.ToDictionary(x => x.Date);
                aiUsage = last14.Select(d => dict.TryGetValue(d, out var v) ? v : new AiUsageDailyDto(d, 0, 0m, 0)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AiUsage chart query failed");
            }

            var dto = new OrgChartDataDto(issuesByStatus, issuesByPriority, issuesByWorkspace, activityTrend, aiUsage);
            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(2));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrgChartData failed orgId {OrgId}", organizationId);
            throw;
        }
    }

    private async Task<int> QueryCountAsync(string sql, CancellationToken ct)
    {
        try
        {
            var rows = await _db.Database.SqlQueryRaw<CountRow>(sql).ToListAsync(ct);
            return rows.FirstOrDefault()?.Value ?? 0;
        }
        catch { return 0; }
    }

    private class CountRow { public int Value { get; set; } }
    private class BucketRow { public string? Label { get; set; } public int Value { get; set; } }
    private class BucketRowInt { public int LabelInt { get; set; } public int Value { get; set; } }
    private class DailyRow { public DateTime Day { get; set; } public int Value { get; set; } }
    private class AiDailyRow { public DateTime Day { get; set; } public long? Tokens { get; set; } public decimal? Cost { get; set; } public int Requests { get; set; } }
}
