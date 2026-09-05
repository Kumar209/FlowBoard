using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

/// <summary>
/// GetProjects - list projects in workspace with pagination. Any workspace member can view (filtered by WorkspaceId). Used by Angular workspace.component grid 1 col mobile 3 col desktop (Task 2.4).
/// </summary>
public record GetProjectsQuery(Guid WorkspaceId, int Page = 1, int PageSize = 20) : ICacheableRequest<PaginatedResult<ProjectDto>>
{
    public string CacheKey => CacheKeys.Projects(WorkspaceId, Page, PageSize);
    public TimeSpan Expiration => TimeSpan.FromMinutes(CacheKeys.ProjectsTtlMinutes);
}

public class GetProjectsHandler : IRequestHandler<GetProjectsQuery, PaginatedResult<ProjectDto>>
{
    private readonly IApplicationDbContext _db;
    public GetProjectsHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedResult<ProjectDto>> Handle(GetProjectsQuery req, CancellationToken ct)
    {
        var q = _db.Projects.Where(p => p.WorkspaceId == req.WorkspaceId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(p => new ProjectDto(p.Id, p.WorkspaceId, p.Name, p.Key, p.Description, p.OwnerId, p.CreatedAt))
            .ToListAsync(ct);
        return new PaginatedResult<ProjectDto>(items, total, req.Page, req.PageSize);
    }
}
