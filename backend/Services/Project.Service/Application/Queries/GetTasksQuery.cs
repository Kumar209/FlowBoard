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
    private readonly ITaskService _service;
    public GetTasksHandler(ITaskService service) => _service = service;
    public Task<PaginatedResult<TaskDto>> Handle(GetTasksQuery req, CancellationToken ct)
        => _service.GetTasksAsync(req.ProjectId, req.Search, req.AssigneeId, req.Priority, req.Label, req.DueFrom, req.DueTo, req.SortBy, req.SortDesc, req.Page, req.PageSize, ct);
}
