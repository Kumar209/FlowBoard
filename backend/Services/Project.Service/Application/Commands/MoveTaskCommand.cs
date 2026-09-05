using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Caching;
using Project.Service.Application.Interfaces;
using System.Text.Json;

namespace Project.Service.Application.Commands;

/// <summary>
/// MoveTask - drag-drop between lists (CDK) or reorder inside same list. FromListId+ToListId+Position. Allowed Member+ (Client 403). Publishes TaskMoved via Outbox for realtime SignalR (Task 3.2) + invalidates Redis board:{projectId} (Task 2.3). Uses Position reordering.
/// </summary>
public record MoveTaskCommand(Guid TaskId, Guid ToListId, int NewPosition, Guid CallerId, List<string> CallerRoles) : IRequest<Result>;

public class MoveTaskValidator : AbstractValidator<MoveTaskCommand>
{
    public MoveTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ToListId).NotEmpty();
        RuleFor(x => x.NewPosition).GreaterThanOrEqualTo(0);
    }
}

public class MoveTaskHandler : IRequestHandler<MoveTaskCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public MoveTaskHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }

    public async Task<Result> Handle(MoveTaskCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Client") || req.CallerRoles.Contains("Viewer"))
            return Result.Failure("Forbidden - Client/Viewer cannot move tasks");

        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == req.TaskId, ct);
        if (task == null) return Result.Failure("Task not found");

        var targetList = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == req.ToListId, ct);
        if (targetList == null) return Result.Failure("Target list not found");

        var fromListId = task.ListId;
        task.MoveToList(req.ToListId, req.NewPosition);

        // Outbox TaskMoved
        var evt = new { TaskId = task.Id, ProjectId = task.ProjectId, FromListId = fromListId, ToListId = req.ToListId, Position = req.NewPosition, ActorId = req.CallerId, OccurredOnUtc = DateTime.UtcNow, EventId = Guid.NewGuid(), CorrelationId = Guid.NewGuid().ToString() };
        _db.OutboxMessages.Add(new Domain.Entities.OutboxMessage("TaskMoved", JsonSerializer.Serialize(evt)));
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, task.Id, req.CallerId, "TaskMoved", JsonSerializer.Serialize(new { fromListId, toListId = req.ToListId })));

        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        await _cache.RemoveByPrefixAsync($"tasks:{task.ProjectId}:");
        await _cache.RemoveByPrefixAsync("board:");
        return Result.Success();
    }
}
