using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// UpdateTask - edit title/description/priority/labels/assignee/dueDate + IssueType/Epic/StoryPoints/StartDate/Environment/Sprint/Parent/Time/Watches. Allowed Member+ (Client 403 for title change but can comment via AddComment). Validates Title required.
/// </summary>
public record UpdateTaskCommand(Guid TaskId, string Title, string? Description, string Priority, string? LabelsJson, Guid? AssigneeId, DateTime? DueDate, Guid CallerId, List<string> CallerRoles, string? IssueType = null, string? Epic = null, int? StoryPoints = null, DateTime? StartDate = null, string? Environment = null, Guid? ParentIssueId = null, Guid? SprintId = null, string? WatchersJson = null, string? LinkedIssuesJson = null, int? TimeEstimated = null, int? TimeSpent = null, int? TimeRemaining = null, Guid? TeamId = null, Guid? ListId = null, string? Status = null) : IRequest<Result<TaskDto>>;

public class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Priority).Must(p => new[] { "Low","Medium","High","Urgent" }.Contains(p)).When(x => !string.IsNullOrEmpty(x.Priority));
    }
}

public class UpdateTaskHandler : IRequestHandler<UpdateTaskCommand, Result<TaskDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public UpdateTaskHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }

    public async Task<Result<TaskDto>> Handle(UpdateTaskCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Client") || req.CallerRoles.Contains("Viewer"))
            return Result<TaskDto>.Failure("Forbidden - Client/Viewer cannot update tasks");

        var task = await _db.Tasks.FindAsync(new object[] { req.TaskId }, ct);
        if (task == null) return Result<TaskDto>.Failure("Task not found");

        var priority = Enum.TryParse<Domain.Enums.TaskPriority>(req.Priority, true, out var p) ? p : Domain.Enums.TaskPriority.Medium;
        // If ListId changed, sync Status to target column name
        string? status = req.Status;
        Guid? targetListId = req.ListId;
        if (targetListId.HasValue && targetListId.Value != Guid.Empty && targetListId.Value != task.ListId)
        {
            var targetList = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == targetListId.Value, ct);
            if (targetList != null) status = targetList.Name;
            task.MoveToList(targetListId.Value, task.Position, status);
        }
        task.Update(req.Title, req.Description, priority, req.LabelsJson, req.AssigneeId, req.DueDate, req.IssueType, req.Epic, req.StoryPoints, req.StartDate, req.Environment, req.ParentIssueId, req.SprintId, req.WatchersJson, req.LinkedIssuesJson, req.TimeEstimated, req.TimeSpent, req.TimeRemaining, req.TeamId, status);

        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, task.Id, req.CallerId, "TaskUpdated", $"{{\"title\":\"{req.Title}\"}}"));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        await _cache.RemoveByPrefixAsync(CacheKeys.Board(task.ProjectId) + ":");
        await _cache.RemoveByPrefixAsync($"tasks:{task.ProjectId}:");
        return Result<TaskDto>.Success(new TaskDto(task.Id, task.ProjectId, task.ListId, task.Title, task.Description, task.Priority.ToString(), task.LabelsJson, task.AssigneeId, task.Position, task.CreatedAt, task.DueDate, task.IssueType, task.Epic, task.StoryPoints, task.StartDate, task.Environment, task.ParentIssueId, task.SprintId, task.WatchersJson, task.LinkedIssuesJson, task.TimeEstimated, task.TimeSpent, task.TimeRemaining, task.TeamId, task.Status));
    }
}
