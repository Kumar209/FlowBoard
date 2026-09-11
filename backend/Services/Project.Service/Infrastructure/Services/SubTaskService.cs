using Microsoft.EntityFrameworkCore;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;
using SharedKernel;

namespace Project.Service.Infrastructure.Services;

public class SubTaskService : ISubTaskService
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public SubTaskService(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }

    public async Task<Result<SubTaskDto>> CreateSubTaskAsync(Guid taskId, string title, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (Roles.CanUpload(callerRoles) == false)
            return Result<SubTaskDto>.Failure("Forbidden - Client cannot manage subtasks");
        var task = await _db.Tasks.FindAsync(new object[] { taskId }, ct);
        if (task == null) return Result<SubTaskDto>.Failure("Task not found");
        var sub = new Domain.Entities.SubTask(taskId, title);
        _db.SubTasks.Add(sub);
        var wsSub = await _db.Projects.Where(p => p.Id == task.ProjectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync(ct);
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, taskId, callerId, "SubTaskCreated", $"{{\"title\":\"{title}\"}}", wsSub));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        return Result<SubTaskDto>.Success(new SubTaskDto(sub.Id, sub.TaskId, sub.Title, sub.IsCompleted, sub.CreatedAt));
    }

    public async Task<Result<SubTaskDto>> UpdateSubTaskAsync(Guid subTaskId, string title, Guid callerId, CancellationToken ct = default)
    {
        var sub = await _db.SubTasks.FindAsync(new object[] { subTaskId }, ct);
        if (sub == null) return Result<SubTaskDto>.Failure("Subtask not found");
        sub.Rename(title);
        await _db.SaveChangesAsync(ct);
        var task = await _db.Tasks.FindAsync(new object[] { sub.TaskId }, ct);
        if (task != null) await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        return Result<SubTaskDto>.Success(new SubTaskDto(sub.Id, sub.TaskId, sub.Title, sub.IsCompleted, sub.CreatedAt));
    }

    public async Task<Result<SubTaskDto>> ToggleSubTaskAsync(Guid subTaskId, Guid callerId, CancellationToken ct = default)
    {
        var sub = await _db.SubTasks.FindAsync(new object[] { subTaskId }, ct);
        if (sub == null) return Result<SubTaskDto>.Failure("Subtask not found");
        sub.Toggle();
        await _db.SaveChangesAsync(ct);
        var task = await _db.Tasks.FindAsync(new object[] { sub.TaskId }, ct);
        if (task != null) await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        return Result<SubTaskDto>.Success(new SubTaskDto(sub.Id, sub.TaskId, sub.Title, sub.IsCompleted, sub.CreatedAt));
    }

    public async Task<Result<bool>> DeleteSubTaskAsync(Guid subTaskId, Guid callerId, CancellationToken ct = default)
    {
        var sub = await _db.SubTasks.FindAsync(new object[] { subTaskId }, ct);
        if (sub == null) return Result<bool>.Failure("Subtask not found");
        var task = await _db.Tasks.FindAsync(new object[] { sub.TaskId }, ct);
        _db.SubTasks.Remove(sub);
        if (task != null) { var wsDel = await _db.Projects.Where(p => p.Id == task.ProjectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync(ct); _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, sub.TaskId, callerId, "SubTaskDeleted", $"{{\"title\":\"{sub.Title}\"}}", wsDel)); }
        await _db.SaveChangesAsync(ct);
        if (task != null) await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        return Result<bool>.Success(true);
    }

    public async Task<List<SubTaskDto>> GetSubTasksAsync(Guid taskId, CancellationToken ct = default)
    {
        return await _db.SubTasks.Where(s => s.TaskId == taskId).OrderBy(s => s.CreatedAt)
            .Select(s => new SubTaskDto(s.Id, s.TaskId, s.Title, s.IsCompleted, s.CreatedAt))
            .ToListAsync(ct);
    }
}
