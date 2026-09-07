using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Infrastructure.Services;

public class ActivityService : IActivityService
{
    private readonly IApplicationDbContext _db;
    public ActivityService(IApplicationDbContext db) => _db = db;

    public async Task<(List<ActivityDto> Items, int Total)> GetActivitiesAsync(Guid projectId, int page, int pageSize, Guid? taskId, CancellationToken ct = default)
    {
        var q = _db.ActivityLogs.Where(a => a.ProjectId == projectId);
        if (taskId != null && taskId != Guid.Empty) q = q.Where(a => a.TaskId == taskId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.OccurredAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new ActivityDto(a.Id, a.ProjectId, a.TaskId, a.ActorId, a.Action, a.PayloadJson, a.OccurredAt))
            .ToListAsync(ct);
        return (items, total);
    }
}
