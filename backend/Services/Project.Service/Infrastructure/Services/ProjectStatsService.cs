using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Project.Service.Application.Interfaces;
using SharedKernel;

namespace Project.Service.Infrastructure.Services;

public class ProjectStatsService : IProjectStatsService
{
    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProjectStatsService> _logger;
    public ProjectStatsService(IApplicationDbContext db, IMemoryCache cache, ILogger<ProjectStatsService> logger)
    {
        _db = db; _cache = cache; _logger = logger;
    }

    private async Task<bool> CanViewAsync(Guid projectId, Guid callerId, CancellationToken ct)
    {
        var proj = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (proj == null) return false;
        // SuperAdmin via identity WorkspaceMembers Role 0? Check via SqlQueryRaw same DB
        try
        {
            var isSuper = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM [identity].[WorkspaceMembers] WHERE UserId = @p0 AND Role = 0", callerId).ToListAsync(ct);
            if (isSuper.FirstOrDefault() > 0) return true;
        }
        catch { }
        // Check workspace membership
        try
        {
            var cnt = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = @p0 AND UserId = @p1", proj.WorkspaceId, callerId).ToListAsync(ct);
            if (cnt.FirstOrDefault() > 0) return true;
            var orgCnt = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM [identity].[OrganizationMembers] WHERE OrganizationId IN (SELECT OrganizationId FROM [identity].[Workspaces] WHERE Id = @p0) AND UserId = @p1", proj.WorkspaceId, callerId).ToListAsync(ct);
            if (orgCnt.FirstOrDefault() > 0) return true;
        }
        catch { }
        // Fallback: if caller created project, allow
        if (proj.OwnerId == callerId) return true;
        return false;
    }

    public async Task<ProjectStatsDto> GetProjectStatsAsync(Guid projectId, Guid callerId, CancellationToken ct = default)
    {
        var proj = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (proj == null) throw new NotFoundException("Project not found");
        if (!await CanViewAsync(projectId, callerId, ct)) throw new ForbiddenException("Forbidden - Not a member of this project workspace");

        var key = $"project:stats:{projectId}";
        if (_cache.TryGetValue<ProjectStatsDto>(key, out var cached) && cached != null) return cached;

        try
        {
            var totalIssues = await _db.Tasks.CountAsync(t => t.ProjectId == projectId, ct);
            var completedIssues = await _db.Tasks.CountAsync(t => t.ProjectId == projectId && t.Status == "Done", ct);
            var inProgressIssues = await _db.Tasks.CountAsync(t => t.ProjectId == projectId && t.Status != "Done" && t.Status != "To Do", ct);
            var totalStoryPoints = await _db.Tasks.Where(t => t.ProjectId == projectId && t.StoryPoints != null).SumAsync(t => t.StoryPoints!.Value, ct);
            var activeSprints = await _db.Sprints.CountAsync(s => s.ProjectId == projectId && s.Status == "Active", ct);
            string? activeName = null;
            if (activeSprints > 0)
            {
                var act = await _db.Sprints.AsNoTracking().FirstOrDefaultAsync(s => s.ProjectId == projectId && s.Status == "Active", ct);
                activeName = act?.Name;
            }
            else
            {
                var last = await _db.Sprints.AsNoTracking().Where(s => s.ProjectId == projectId).OrderByDescending(s => s.StartDate).FirstOrDefaultAsync(ct);
                activeName = last?.Name;
            }

            var dto = new ProjectStatsDto(totalIssues, completedIssues, inProgressIssues, totalStoryPoints, activeSprints, activeName);
            _cache.Set(key, dto, TimeSpan.FromMinutes(2));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetProjectStats failed {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<ProjectChartDataDto> GetProjectChartDataAsync(Guid projectId, Guid callerId, CancellationToken ct = default)
    {
        var proj = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (proj == null) throw new NotFoundException("Project not found");
        if (!await CanViewAsync(projectId, callerId, ct)) throw new ForbiddenException("Forbidden - Not a member of this project workspace");

        var key = $"project:chart:{projectId}";
        if (_cache.TryGetValue<ProjectChartDataDto>(key, out var cached) && cached != null) return cached;

        try
        {
            var statusRows = await _db.Tasks.Where(t => t.ProjectId == projectId).GroupBy(t => t.Status).Select(g => new { Label = g.Key, Value = g.Count() }).ToListAsync(ct);
            var issuesByStatus = statusRows.Select(r => new ProjectChartBucketDto(r.Label ?? "Unknown", r.Value)).ToList();

            var typeRows = await _db.Tasks.Where(t => t.ProjectId == projectId).GroupBy(t => t.IssueType).Select(g => new { Label = g.Key, Value = g.Count() }).ToListAsync(ct);
            var issuesByType = typeRows.Select(r => new ProjectChartBucketDto(r.Label ?? "Task", r.Value)).ToList();

            var prioRows = await _db.Tasks.Where(t => t.ProjectId == projectId).GroupBy(t => t.Priority).Select(g => new { Pri = g.Key, Value = g.Count() }).ToListAsync(ct);
            var prioMap = new Dictionary<int, string> { {0,"Low"},{1,"Medium"},{2,"High"},{3,"Urgent"} };
            var issuesByPriority = prioRows.Select(r => new ProjectChartBucketDto(prioMap.TryGetValue((int)r.Pri, out var n) ? n : r.Pri.ToString(), r.Value)).ToList();

            // Assignee workload
            var assigneeGroups = await _db.Tasks.Where(t => t.ProjectId == projectId && t.AssigneeId != null).GroupBy(t => t.AssigneeId!.Value).Select(g => new { AssigneeId = g.Key, Value = g.Count() }).ToListAsync(ct);
            var workload = new List<AssigneeWorkloadDto>();
            if (assigneeGroups.Any())
            {
                var ids = assigneeGroups.Select(g => g.AssigneeId).Distinct().ToList();
                var idStr = string.Join(",", ids.Select(id => $"'{id}'"));
                try
                {
                    var users = await _db.Database.SqlQueryRaw<UserRow>($"SELECT Id, FullName FROM [identity].[Users] WHERE Id IN ({idStr})").ToListAsync(ct);
                    var map = users.ToDictionary(u => u.Id, u => u.FullName ?? u.Id.ToString()[..8]);
                    foreach (var g in assigneeGroups)
                    {
                        var name = map.TryGetValue(g.AssigneeId, out var n) ? n : g.AssigneeId.ToString()[..8];
                        workload.Add(new AssigneeWorkloadDto(name, g.Value));
                    }
                }
                catch
                {
                    foreach (var g in assigneeGroups) workload.Add(new AssigneeWorkloadDto(g.AssigneeId.ToString()[..8], g.Value));
                }
            }
            // Unassigned bucket
            var unassigned = await _db.Tasks.CountAsync(t => t.ProjectId == projectId && t.AssigneeId == null, ct);
            if (unassigned > 0) workload.Add(new AssigneeWorkloadDto("Unassigned", unassigned));

            // Sprint velocity
            var sprints = await _db.Sprints.AsNoTracking().Where(s => s.ProjectId == projectId).OrderBy(s => s.StartDate).ToListAsync(ct);
            var velocity = new List<ProjectVelocityDto>();
            foreach (var s in sprints)
            {
                var totalSp = await _db.Tasks.Where(t => t.ProjectId == projectId && t.SprintId == s.Id && t.StoryPoints != null).SumAsync(t => t.StoryPoints!.Value, ct);
                var completedSp = await _db.Tasks.Where(t => t.ProjectId == projectId && t.SprintId == s.Id && t.Status == "Done" && t.StoryPoints != null).SumAsync(t => t.StoryPoints!.Value, ct);
                velocity.Add(new ProjectVelocityDto(s.Name, totalSp, completedSp));
            }

            var dto = new ProjectChartDataDto(issuesByStatus, issuesByType, issuesByPriority, workload, velocity);
            _cache.Set(key, dto, TimeSpan.FromMinutes(2));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetProjectChartData failed {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<BurndownDto> GetBurndownAsync(Guid projectId, Guid? sprintId, Guid callerId, CancellationToken ct = default)
    {
        var proj = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (proj == null) throw new NotFoundException("Project not found");
        if (!await CanViewAsync(projectId, callerId, ct)) throw new ForbiddenException("Forbidden - Not a member of this project workspace");

        var sprint = sprintId.HasValue ? await _db.Sprints.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sprintId.Value && s.ProjectId == projectId, ct) : null;
        if (sprint == null)
        {
            sprint = await _db.Sprints.AsNoTracking().Where(s => s.ProjectId == projectId && s.Status == "Active").OrderByDescending(s => s.StartDate).FirstOrDefaultAsync(ct)
                ?? await _db.Sprints.AsNoTracking().Where(s => s.ProjectId == projectId).OrderByDescending(s => s.StartDate).FirstOrDefaultAsync(ct);
        }
        if (sprint == null) throw new NotFoundException("Sprint not found for burndown - create a sprint first");

        var cacheKey = $"burndown:{projectId}:{sprint.Id}";
        if (_cache.TryGetValue<BurndownDto>(cacheKey, out var cached) && cached != null) return cached;

        var tasks = await _db.Tasks.AsNoTracking().Where(t => t.ProjectId == projectId && t.SprintId == sprint.Id).ToListAsync(ct);
        var total = tasks.Count;
        var start = sprint.StartDate.Date;
        var end = sprint.EndDate.Date;
        if (end < start) end = start.AddDays(14);
        var days = (end - start).Days + 1;
        if (days > 30) days = 30;
        if (days < 1) days = 1;

        var points = new List<BurndownPointDto>();
        for (int i = 0; i < days; i++)
        {
            var day = start.AddDays(i);
            var completed = tasks.Count(t => t.Status == "Done" && t.UpdatedAt.Date <= day);
            var remaining = total - completed;
            var ideal = total - (int)Math.Round((double)total * (i + 1) / days);
            if (ideal < 0) ideal = 0;
            points.Add(new BurndownPointDto(day.ToString("yyyy-MM-dd"), total, remaining, ideal));
        }

        var dto = new BurndownDto(sprint.Id, sprint.Name, sprint.StartDate.ToString("yyyy-MM-dd"), sprint.EndDate.ToString("yyyy-MM-dd"), points);
        _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(2));
        return dto;
    }

    private class UserRow { public Guid Id { get; set; } public string? FullName { get; set; } }
    private class NotFoundException : Exception { public NotFoundException(string msg) : base(msg) {} }
    private class ForbiddenException : Exception { public ForbiddenException(string msg) : base(msg) {} }
}
