using Microsoft.EntityFrameworkCore;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;
using Project.Service.Application.Queries;
using Project.Service.Domain.Entities;
using SharedKernel;
using System.Text.Json;
using ProjectEntity = Project.Service.Domain.Entities.Project;

namespace Project.Service.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;

    public ProjectService(IApplicationDbContext db, IRedisCacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Result<ProjectDto>> CreateProjectAsync(Guid workspaceId, string name, string? description, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        var allowed = new[] { Roles.OrgAdmin, Roles.SuperAdmin };
        if (!callerRoles.Any(r => allowed.Contains(r)))
            return Result<ProjectDto>.Failure("Forbidden - Need OrgAdmin/SuperAdmin. Your roles: " + string.Join(",", callerRoles));
        var prefix = new string(name.Where(char.IsLetter).Take(3).ToArray()).ToUpperInvariant();
        if (prefix.Length < 2) prefix = "PRJ";
        var count = await _db.Projects.CountAsync(p => p.WorkspaceId == workspaceId, ct);
        var key = $"{prefix}-{count + 1}";
        var exists = await _db.Projects.AnyAsync(p => p.WorkspaceId == workspaceId && p.Key == key, ct);
        if (exists) key = $"{prefix}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        var project = new ProjectEntity(workspaceId, name, key, callerId, description);
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);
        _db.ActivityLogs.Add(new ActivityLog(project.Id, null, callerId, "ProjectCreated", $"{{\"name\":\"{name}\",\"key\":\"{key}\"}}"));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveByPrefixAsync($"projects:{workspaceId}:");
        return Result<ProjectDto>.Success(new ProjectDto(project.Id, project.WorkspaceId, project.Name, project.Key, project.Description, project.OwnerId, project.CreatedAt));
    }

    public async Task<Result<ProjectDto>> UpdateProjectAsync(Guid projectId, string name, string? description, string? slug, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        var allowed = new[] { Roles.OrgAdmin, Roles.SuperAdmin };
        if (!callerRoles.Any(r => allowed.Contains(r)))
            return Result<ProjectDto>.Failure("Forbidden - Need OrgAdmin/SuperAdmin");
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project == null) return Result<ProjectDto>.Failure("Project not found");
        project.Update(name, description);
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveByPrefixAsync($"projects:{project.WorkspaceId}:");
        await _cache.RemoveAsync($"board:{project.Id}");
        return Result<ProjectDto>.Success(new ProjectDto(project.Id, project.WorkspaceId, project.Name, project.Key, project.Description, project.OwnerId, project.CreatedAt));
    }

    public async Task<Result<bool>> DeleteProjectAsync(Guid projectId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        var allowed = new[] { Roles.OrgAdmin, Roles.SuperAdmin };
        if (!callerRoles.Any(r => allowed.Contains(r)))
            return Result<bool>.Failure("Forbidden - Need OrgAdmin/SuperAdmin");
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project == null) return Result<bool>.Failure("Project not found");
        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveByPrefixAsync($"projects:{project.WorkspaceId}:");
        await _cache.RemoveAsync($"board:{projectId}");
        return Result<bool>.Success(true);
    }

    public async Task<PaginatedResult<ProjectDto>> GetProjectsAsync(Guid workspaceId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Projects.Where(p => p.WorkspaceId == workspaceId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProjectDto(p.Id, p.WorkspaceId, p.Name, p.Key, p.Description, p.OwnerId, p.CreatedAt))
            .ToListAsync(ct);
        return new PaginatedResult<ProjectDto>(items, total, page, pageSize);
    }

    public async Task<BoardDto> GetBoardAsync(Guid projectId, Guid? boardId, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct) ?? throw new Exception("Project not found");
        Domain.Entities.Board? board = null;
        if (boardId.HasValue && boardId.Value != Guid.Empty)
            board = await _db.Boards.FirstOrDefaultAsync(b => b.Id == boardId.Value, ct);
        var listsQuery = _db.BoardLists.Where(b => b.ProjectId == projectId);
        if (boardId.HasValue && boardId.Value != Guid.Empty)
            listsQuery = listsQuery.Where(b => b.BoardId == boardId.Value);
        var listsRaw = await listsQuery.OrderBy(b => b.Position).ToListAsync(ct);
        var columnIds = listsRaw.Select(l => l.Id).ToList();
        var mappings = await _db.BoardColumnStatuses.Where(bcs => columnIds.Contains(bcs.ColumnId)).ToListAsync(ct);
        var lists = listsRaw.Select(l => new BoardListDto(l.Id, l.ProjectId, l.Name, l.Position, mappings.Where(m => m.ColumnId == l.Id).Select(m => m.StatusId).ToList())).ToList();
        var tasksQuery = _db.Tasks.Where(t => t.ProjectId == projectId);
        // Jira-like: board is a view — return all project tasks, frontend filters by Status == column name (via BoardColumnStatus). No ListId filtering for backlog.
        if (board?.FilterJson != null)
        {
            try
            {
                var filter = JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(board.FilterJson);
                if (filter != null && filter.TryGetValue("teamIds", out var teamIdsEl) && teamIdsEl.ValueKind == JsonValueKind.Array)
                {
                    var teamIds = teamIdsEl.EnumerateArray().Select(e => Guid.TryParse(e.GetString(), out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList();
                    if (teamIds.Any()) tasksQuery = tasksQuery.Where(t => t.TeamId != null && teamIds.Contains(t.TeamId.Value));
                }
            }
            catch { }
        }
        var tasks = await tasksQuery.OrderBy(t => t.Position)
            .Select(t => new TaskDto(t.Id, t.ProjectId, t.ListId, t.Title, t.Description, t.Priority.ToString(), t.LabelsJson, t.AssigneeId, t.Position, t.CreatedAt, t.DueDate, t.IssueType, t.Epic, t.StoryPoints, t.StartDate, t.Environment, t.ParentIssueId, t.SprintId, t.WatchersJson, t.LinkedIssuesJson, t.TimeEstimated, t.TimeSpent, t.TimeRemaining, t.TeamId, t.Status, t.StatusId, t.AcceptanceCriteriaJson))
            .ToListAsync(ct);
        var dto = new ProjectDto(project.Id, project.WorkspaceId, project.Name, project.Key, project.Description, project.OwnerId, project.CreatedAt);
        return new BoardDto(dto, lists, tasks);
    }
}
