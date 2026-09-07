using MediatR;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

/// <summary>
/// GetActivities - paged activity timeline for project (DaisyUI timeline). Filtered by ProjectId, ordered OccurredAt desc. Used by activity.component + burndown (Task 4.4 ApexCharts). Delegates to IActivityService (MNC-grade DIP, like GetProjects).
/// </summary>
public record GetActivitiesQuery(Guid ProjectId, int Page = 1, int PageSize = 20, Guid? TaskId = null) : IRequest<(List<ActivityDto> Items, int Total)>;

public class GetActivitiesHandler : IRequestHandler<GetActivitiesQuery, (List<ActivityDto> Items, int Total)>
{
    private readonly IActivityService _service;
    public GetActivitiesHandler(IActivityService service) => _service = service;
    public Task<(List<ActivityDto> Items, int Total)> Handle(GetActivitiesQuery req, CancellationToken ct)
        => _service.GetActivitiesAsync(req.ProjectId, req.Page, req.PageSize, req.TaskId, ct);
}
