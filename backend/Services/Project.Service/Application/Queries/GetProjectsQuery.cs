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
    private readonly IProjectService _service;
    public GetProjectsHandler(IProjectService service) => _service = service;
    public Task<PaginatedResult<ProjectDto>> Handle(GetProjectsQuery req, CancellationToken ct)
        => _service.GetProjectsAsync(req.WorkspaceId, req.Page, req.PageSize, ct);
}
