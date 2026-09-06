using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

/// <summary>
/// GetActivities - paged activity timeline for project (DaisyUI timeline). Filtered by ProjectId, ordered OccurredAt desc. Used by activity.component + burndown (Task 4.4 ApexCharts).
/// </summary>
public record GetActivitiesQuery(Guid ProjectId, int Page = 1, int PageSize = 20, Guid? TaskId = null) : IRequest<(List<ActivityDto> Items, int Total)>;

public class GetActivitiesHandler : IRequestHandler<GetActivitiesQuery, (List<ActivityDto> Items, int Total)>
{
    private readonly IApplicationDbContext _db;
    public GetActivitiesHandler(IApplicationDbContext db) => _db = db;

    public async Task<(List<ActivityDto> Items, int Total)> Handle(GetActivitiesQuery req, CancellationToken ct)
    {
        var q = _db.ActivityLogs.Where(a => a.ProjectId == req.ProjectId);
        if (req.TaskId != null && req.TaskId != Guid.Empty) q = q.Where(a => a.TaskId == req.TaskId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.OccurredAt).Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(a => new ActivityDto(a.Id, a.ProjectId, a.TaskId, a.ActorId, a.Action, a.PayloadJson, a.OccurredAt))
            .ToListAsync(ct);
        return (items, total);
    }
}
