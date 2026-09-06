using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

/// <summary>
/// GetTasks - filtered, sorted, paginated task list for board/list-view. Supports ?search (FULLTEXT Title), assignee, priority, label, dueDate. Used by Angular board + list-view (DaisyUI table -> cards on mobile) via TanStack Query.
/// </summary>
public record GetTasksQuery(Guid ProjectId, string? Search, Guid? AssigneeId, string? Priority, string? Label, DateTime? DueFrom, DateTime? DueTo, string? SortBy, bool SortDesc, int Page, int PageSize) : ICacheableRequest<PaginatedResult<TaskDto>>
{
    public string CacheKey
    {
        get
        {
            var raw = $"{ProjectId}:{Search}:{AssigneeId}:{Priority}:{Label}:{DueFrom}:{DueTo}:{SortBy}:{SortDesc}:{Page}:{PageSize}";
            using var sha = SHA256.Create();
            var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)))[..12].ToLower();
            return CacheKeys.Tasks(ProjectId, hash);
        }
    }
    public TimeSpan Expiration => TimeSpan.FromMinutes(CacheKeys.TasksTtlMinutes);
}

public class GetTasksHandler : IRequestHandler<GetTasksQuery, PaginatedResult<TaskDto>>
{
    private readonly IApplicationDbContext _db;
    public GetTasksHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedResult<TaskDto>> Handle(GetTasksQuery req, CancellationToken ct)
    {
        var q = _db.Tasks.Where(t => t.ProjectId == req.ProjectId);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower();
            q = q.Where(t => t.Title.ToLower().Contains(s) || (t.Description != null && t.Description.ToLower().Contains(s)));
        }
        if (req.AssigneeId.HasValue) q = q.Where(t => t.AssigneeId == req.AssigneeId.Value);
        if (!string.IsNullOrWhiteSpace(req.Priority) && Enum.TryParse<Domain.Enums.TaskPriority>(req.Priority, true, out var pr)) q = q.Where(t => t.Priority == pr);
        if (!string.IsNullOrWhiteSpace(req.Label)) q = q.Where(t => t.LabelsJson != null && t.LabelsJson.Contains(req.Label));
        if (req.DueFrom.HasValue) q = q.Where(t => t.DueDate >= req.DueFrom.Value);
        if (req.DueTo.HasValue) q = q.Where(t => t.DueDate <= req.DueTo.Value);

        // Sort
        q = req.SortBy?.ToLower() switch
        {
            "priority" => req.SortDesc ? q.OrderByDescending(t => t.Priority) : q.OrderBy(t => t.Priority),
            "createdat" => req.SortDesc ? q.OrderByDescending(t => t.CreatedAt) : q.OrderBy(t => t.CreatedAt),
            _ => req.SortDesc ? q.OrderByDescending(t => t.Position) : q.OrderBy(t => t.Position)
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(t => new TaskDto(t.Id, t.ProjectId, t.ListId, t.Title, t.Description, t.Priority.ToString(), t.LabelsJson, t.AssigneeId, t.Position, t.CreatedAt, t.DueDate, t.IssueType, t.Epic, t.StoryPoints, t.StartDate, t.Environment, t.ParentIssueId, t.SprintId, t.WatchersJson, t.LinkedIssuesJson, t.TimeEstimated, t.TimeSpent, t.TimeRemaining, t.TeamId, t.Status))
            .ToListAsync(ct);
        return new PaginatedResult<TaskDto>(items, total, req.Page, req.PageSize);
    }
}
