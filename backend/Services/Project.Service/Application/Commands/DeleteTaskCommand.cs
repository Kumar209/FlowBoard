using FluentValidation;
using MediatR;
using SharedKernel;
using Project.Service.Application.Caching;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

public record DeleteTaskCommand(Guid TaskId, Guid CallerId, List<string> CallerRoles) : IRequest<Result>;

public class DeleteTaskValidator : AbstractValidator<DeleteTaskCommand>
{
    public DeleteTaskValidator() { RuleFor(x => x.TaskId).NotEmpty(); }
}

public class DeleteTaskHandler : IRequestHandler<DeleteTaskCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public DeleteTaskHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }

    public async Task<Result> Handle(DeleteTaskCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Client") || req.CallerRoles.Contains("Viewer"))
            return Result.Failure("Forbidden - Client/Viewer cannot delete tasks");

        var task = await _db.Tasks.FindAsync(new object[] { req.TaskId }, ct);
        if (task == null) return Result.Failure("Task not found");

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(ct);
        await _db.ActivityLogs.AddAsync(new Domain.Entities.ActivityLog(task.ProjectId, task.Id, req.CallerId, "TaskDeleted", $"{{\"title\":\"{task.Title}\"}}"), ct);
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        await _cache.RemoveByPrefixAsync(CacheKeys.Board(task.ProjectId) + ":");
        await _cache.RemoveByPrefixAsync($"tasks:{task.ProjectId}:");
        return Result.Success();
    }
}
