using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;
using System.Text.Json;

namespace Project.Service.Application.Commands;

/// <summary>
/// CreateTask - card in BoardList. Title required, Priority Medium default, LabelsJson JSON array, AssigneeId optional. Allowed Member/PM/OrgAdmin/SuperAdmin (Client/Viewer 403). Publishes TaskCreated via Outbox (Task 3.1) for SignalR.
/// </summary>
public record CreateTaskCommand(Guid ProjectId, Guid ListId, string Title, string? Description, string Priority, string? LabelsJson, Guid? AssigneeId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<TaskDto>>;

public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ListId).NotEmpty();
        RuleFor(x => x.Priority).Must(p => new[] { "Low","Medium","High","Urgent" }.Contains(p)).When(x => !string.IsNullOrEmpty(x.Priority)).WithMessage("Priority must be Low/Medium/High/Urgent");
    }
}

public class CreateTaskHandler : IRequestHandler<CreateTaskCommand, Result<TaskDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public CreateTaskHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }

    public async Task<Result<TaskDto>> Handle(CreateTaskCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Client") || req.CallerRoles.Contains("Viewer"))
            return Result<TaskDto>.Failure("Forbidden - Client/Viewer cannot create tasks");

        var list = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == req.ListId && b.ProjectId == req.ProjectId, ct);
        if (list == null) return Result<TaskDto>.Failure("List not found in project");

        var priority = Enum.TryParse<Domain.Enums.TaskPriority>(req.Priority, true, out var p) ? p : Domain.Enums.TaskPriority.Medium;
        var maxPos = await _db.Tasks.Where(t => t.ListId == req.ListId).MaxAsync(t => (int?)t.Position, ct) ?? -1;

        var task = new Domain.Entities.TaskItem(req.ProjectId, req.ListId, req.Title, req.CallerId, maxPos + 1, priority, req.AssigneeId, req.Description, req.LabelsJson);
        _db.Tasks.Add(task);

        // Outbox for MassTransit (Task 3.1) - same transaction
        var evt = new { TaskId = task.Id, ProjectId = task.ProjectId, ListId = task.ListId, Title = task.Title, ActorId = req.CallerId, OccurredOnUtc = DateTime.UtcNow, EventId = Guid.NewGuid(), CorrelationId = Guid.NewGuid().ToString() };
        _db.OutboxMessages.Add(new Domain.Entities.OutboxMessage("TaskCreated", JsonSerializer.Serialize(evt)));

        // Activity
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(req.ProjectId, task.Id, req.CallerId, "TaskCreated", JsonSerializer.Serialize(new { task.Title, list.Name })));

        await _db.SaveChangesAsync(ct);

        // MNC-grade: invalidate read caches via pipeline invalidation (not controller) - keeps Api thin
        await _cache.RemoveAsync(CacheKeys.Board(req.ProjectId));
        await _cache.RemoveByPrefixAsync($"tasks:{req.ProjectId}:");

        var dto = new TaskDto(task.Id, task.ProjectId, task.ListId, task.Title, task.Description, task.Priority.ToString(), task.LabelsJson, task.AssigneeId, task.Position, task.CreatedAt);
        return Result<TaskDto>.Success(dto);
    }
}
