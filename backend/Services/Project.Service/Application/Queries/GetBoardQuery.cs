using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

/// <summary>
/// GetBoard - returns project with ordered lists and tasks grouped by ListId+Position. Cached as board:{projectId} TTL 5m via Upstash Redis (Task 2.3). Invalidated on task write.
/// </summary>
public record GetBoardQuery(Guid ProjectId, Guid? BoardId = null) : ICacheableRequest<BoardDto>
{
    public string CacheKey => BoardId.HasValue ? $"{CacheKeys.Board(ProjectId)}:{BoardId}" : CacheKeys.Board(ProjectId);
    public TimeSpan Expiration => TimeSpan.FromMinutes(CacheKeys.BoardTtlMinutes);
}

public record BoardDto(ProjectDto Project, List<BoardListDto> Lists, List<TaskDto> Tasks, List<BoardListDto>? Boards = null);

public class GetBoardHandler : IRequestHandler<GetBoardQuery, BoardDto>
{
    private readonly IApplicationDbContext _db;
    public GetBoardHandler(IApplicationDbContext db) => _db = db;

    public async Task<BoardDto> Handle(GetBoardQuery req, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, ct)
            ?? throw new Exception("Project not found");
        // Fetch board for filter
        Domain.Entities.Board? board = null;
        if (req.BoardId.HasValue && req.BoardId.Value != Guid.Empty)
            board = await _db.Boards.FirstOrDefaultAsync(b => b.Id == req.BoardId.Value, ct);
        // Board filtering: if BoardId supplied, only columns for that board; else all project lists (for backlog/issues views)
        var listsQuery = _db.BoardLists.Where(b => b.ProjectId == req.ProjectId);
        if (req.BoardId.HasValue && req.BoardId.Value != Guid.Empty)
            listsQuery = listsQuery.Where(b => b.BoardId == req.BoardId.Value);
        var lists = await listsQuery.OrderBy(b => b.Position)
            .Select(b => new BoardListDto(b.Id, b.ProjectId, b.Name, b.Position)).ToListAsync(ct);
        var listIds = lists.Select(l => l.Id).ToList();
        // Tasks: if BoardId filtered, only tasks whose ListId in board's lists; else all project tasks
        var tasksQuery = _db.Tasks.Where(t => t.ProjectId == req.ProjectId);
        if (req.BoardId.HasValue && req.BoardId.Value != Guid.Empty && listIds.Any())
            tasksQuery = tasksQuery.Where(t => listIds.Contains(t.ListId));
        // Apply board filter (TeamIds etc.) — Board = view filter (Engineering=Dev team, QA= QA team)
        if (board?.FilterJson != null)
        {
            try
            {
                var filter = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(board.FilterJson);
                if (filter != null && filter.TryGetValue("teamIds", out var teamIdsEl) && teamIdsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var teamIds = teamIdsEl.EnumerateArray().Select(e => Guid.TryParse(e.GetString(), out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList();
                    if (teamIds.Any()) tasksQuery = tasksQuery.Where(t => t.TeamId != null && teamIds.Contains(t.TeamId.Value));
                }
            } catch { }
        }
        // For backlog view (BoardId null), include all tasks; board view shows only its columns' tasks (Kanban isolation)
        var tasks = await tasksQuery.OrderBy(t => t.Position)
            .Select(t => new TaskDto(t.Id, t.ProjectId, t.ListId, t.Title, t.Description, t.Priority.ToString(), t.LabelsJson, t.AssigneeId, t.Position, t.CreatedAt, t.DueDate, t.IssueType, t.Epic, t.StoryPoints, t.StartDate, t.Environment, t.ParentIssueId, t.SprintId, t.WatchersJson, t.LinkedIssuesJson, t.TimeEstimated, t.TimeSpent, t.TimeRemaining, t.TeamId, t.Status)).ToListAsync(ct);

        var dto = new ProjectDto(project.Id, project.WorkspaceId, project.Name, project.Key, project.Description, project.OwnerId, project.CreatedAt);
        return new BoardDto(dto, lists, tasks);
    }
}
