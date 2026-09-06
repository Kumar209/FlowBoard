using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// Subtasks CRUD - Jira checklist inside Task. POST /tasks/{id}/subtasks, toggle, rename, delete.
/// </summary>
public record CreateSubTaskCommand(Guid TaskId, string Title, Guid CallerId, List<string> CallerRoles) : IRequest<Result<SubTaskDto>>;
public record UpdateSubTaskCommand(Guid SubTaskId, string Title, Guid CallerId) : IRequest<Result<SubTaskDto>>;
public record ToggleSubTaskCommand(Guid SubTaskId, Guid CallerId) : IRequest<Result<SubTaskDto>>;
public record DeleteSubTaskCommand(Guid SubTaskId, Guid CallerId) : IRequest<Result<bool>>;

public class CreateSubTaskValidator : AbstractValidator<CreateSubTaskCommand>
{
    public CreateSubTaskValidator() { RuleFor(x => x.TaskId).NotEmpty(); RuleFor(x => x.Title).NotEmpty().MaximumLength(300); }
}
public class UpdateSubTaskValidator : AbstractValidator<UpdateSubTaskCommand>
{
    public UpdateSubTaskValidator() { RuleFor(x => x.SubTaskId).NotEmpty(); RuleFor(x => x.Title).NotEmpty().MaximumLength(300); }
}

public class CreateSubTaskHandler : IRequestHandler<CreateSubTaskCommand, Result<SubTaskDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public CreateSubTaskHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }
    public async Task<Result<SubTaskDto>> Handle(CreateSubTaskCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Viewer") || req.CallerRoles.Contains("Client"))
            return Result<SubTaskDto>.Failure("Forbidden - Viewer/Client cannot manage subtasks");
        var task = await _db.Tasks.FindAsync(new object[] { req.TaskId }, ct);
        if (task == null) return Result<SubTaskDto>.Failure("Task not found");
        var sub = new Domain.Entities.SubTask(req.TaskId, req.Title);
        _db.SubTasks.Add(sub);
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, req.TaskId, req.CallerId, "SubTaskCreated", $"{{\"title\":\"{req.Title}\"}}"));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        return Result<SubTaskDto>.Success(new SubTaskDto(sub.Id, sub.TaskId, sub.Title, sub.IsCompleted, sub.CreatedAt));
    }
}

public class UpdateSubTaskHandler : IRequestHandler<UpdateSubTaskCommand, Result<SubTaskDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public UpdateSubTaskHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }
    public async Task<Result<SubTaskDto>> Handle(UpdateSubTaskCommand req, CancellationToken ct)
    {
        var sub = await _db.SubTasks.FindAsync(new object[] { req.SubTaskId }, ct);
        if (sub == null) return Result<SubTaskDto>.Failure("Subtask not found");
        sub.Rename(req.Title);
        await _db.SaveChangesAsync(ct);
        var task = await _db.Tasks.FindAsync(new object[] { sub.TaskId }, ct);
        if (task != null) await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        return Result<SubTaskDto>.Success(new SubTaskDto(sub.Id, sub.TaskId, sub.Title, sub.IsCompleted, sub.CreatedAt));
    }
}

public class ToggleSubTaskHandler : IRequestHandler<ToggleSubTaskCommand, Result<SubTaskDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public ToggleSubTaskHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }
    public async Task<Result<SubTaskDto>> Handle(ToggleSubTaskCommand req, CancellationToken ct)
    {
        var sub = await _db.SubTasks.FindAsync(new object[] { req.SubTaskId }, ct);
        if (sub == null) return Result<SubTaskDto>.Failure("Subtask not found");
        sub.Toggle();
        await _db.SaveChangesAsync(ct);
        var task = await _db.Tasks.FindAsync(new object[] { sub.TaskId }, ct);
        if (task != null) await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        return Result<SubTaskDto>.Success(new SubTaskDto(sub.Id, sub.TaskId, sub.Title, sub.IsCompleted, sub.CreatedAt));
    }
}

public class DeleteSubTaskHandler : IRequestHandler<DeleteSubTaskCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public DeleteSubTaskHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }
    public async Task<Result<bool>> Handle(DeleteSubTaskCommand req, CancellationToken ct)
    {
        var sub = await _db.SubTasks.FindAsync(new object[] { req.SubTaskId }, ct);
        if (sub == null) return Result<bool>.Failure("Subtask not found");
        var task = await _db.Tasks.FindAsync(new object[] { sub.TaskId }, ct);
        _db.SubTasks.Remove(sub);
        if (task != null)
            _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, sub.TaskId, req.CallerId, "SubTaskDeleted", $"{{\"title\":\"{sub.Title}\"}}"));
        await _db.SaveChangesAsync(ct);
        if (task != null) await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        return Result<bool>.Success(true);
    }
}
