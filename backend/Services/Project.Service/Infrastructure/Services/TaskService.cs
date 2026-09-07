using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;
using SharedKernel;
using System.Text.Json;

namespace Project.Service.Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public TaskService(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }

    public async Task<Result<TaskDto>> CreateTaskAsync(Guid projectId, Guid listId, string title, string? description, string priority, string? labelsJson, Guid? assigneeId, DateTime? dueDate, string? issueType, string? epic, int? storyPoints, DateTime? startDate, string? environment, Guid? parentIssueId, Guid? sprintId, Guid? teamId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Client") || callerRoles.Contains("Viewer")) return Result<TaskDto>.Failure("Forbidden - Client/Viewer cannot create tasks");
        var list = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == listId && b.ProjectId == projectId, ct);
        if (list == null) return Result<TaskDto>.Failure("List not found in project");
        var prio = Enum.TryParse<Domain.Enums.TaskPriority>(priority, true, out var p) ? p : Domain.Enums.TaskPriority.Medium;
        var maxPos = await _db.Tasks.Where(t => t.ListId == listId).MaxAsync(t => (int?)t.Position, ct) ?? -1;
        var status = list.Name;
        var task = new Domain.Entities.TaskItem(projectId, listId, title, callerId, maxPos + 1, prio, assigneeId, description, labelsJson, dueDate, issueType ?? "Task", epic, storyPoints, startDate, environment, parentIssueId, sprintId, teamId, status);
        _db.Tasks.Add(task);
        var workspaceId = await _db.Projects.Where(p => p.Id == projectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync(ct);
        var recipientIds = await _db.ProjectMembers.Where(pm => pm.ProjectId == projectId).Select(pm => pm.UserId).ToListAsync(ct);
        if (!recipientIds.Any())
        {
            try { recipientIds = await _db.Database.SqlQueryRaw<Guid>("SELECT UserId FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0}", workspaceId).ToListAsync(ct); } catch { }
        }
        var evt = new { TaskId = task.Id, ProjectId = task.ProjectId, WorkspaceId = workspaceId, ListId = task.ListId, Title = task.Title, ActorId = callerId, RecipientUserIds = recipientIds, OccurredOnUtc = DateTime.UtcNow, EventId = Guid.NewGuid(), CorrelationId = Guid.NewGuid().ToString() };
        _db.OutboxMessages.Add(new Domain.Entities.OutboxMessage("TaskCreated", JsonSerializer.Serialize(evt)));
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(projectId, task.Id, callerId, "TaskCreated", JsonSerializer.Serialize(new { task.Title, list.Name })));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"board:{projectId}");
        await _cache.RemoveByPrefixAsync($"board:{projectId}:");
        await _cache.RemoveByPrefixAsync($"tasks:{projectId}:");
        return Result<TaskDto>.Success(new TaskDto(task.Id, task.ProjectId, task.ListId, task.Title, task.Description, task.Priority.ToString(), task.LabelsJson, task.AssigneeId, task.Position, task.CreatedAt, task.DueDate, task.IssueType, task.Epic, task.StoryPoints, task.StartDate, task.Environment, task.ParentIssueId, task.SprintId, task.WatchersJson, task.LinkedIssuesJson, task.TimeEstimated, task.TimeSpent, task.TimeRemaining, task.TeamId, task.Status));
    }

    public async Task<Result<TaskDto>> UpdateTaskAsync(Guid taskId, string title, string? description, string priority, string? labelsJson, Guid? assigneeId, DateTime? dueDate, string? issueType, string? epic, int? storyPoints, DateTime? startDate, string? environment, Guid? parentIssueId, Guid? sprintId, string? watchersJson, string? linkedIssuesJson, int? timeEstimated, int? timeSpent, int? timeRemaining, Guid? teamId, Guid? listId, string? status, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Client") || callerRoles.Contains("Viewer")) return Result<TaskDto>.Failure("Forbidden - Client/Viewer cannot update tasks");
        var task = await _db.Tasks.FindAsync(new object[] { taskId }, ct);
        if (task == null) return Result<TaskDto>.Failure("Task not found");
        var prio = Enum.TryParse<Domain.Enums.TaskPriority>(priority, true, out var p) ? p : Domain.Enums.TaskPriority.Medium;
        string? newStatus = status;
        Guid? targetListId = listId;
        if (targetListId.HasValue && targetListId.Value != Guid.Empty && targetListId.Value != task.ListId)
        {
            var targetList = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == targetListId.Value, ct);
            if (targetList != null) newStatus = targetList.Name;
            task.MoveToList(targetListId.Value, task.Position, newStatus);
        }
        task.Update(title, description, prio, labelsJson, assigneeId, dueDate, issueType, epic, storyPoints, startDate, environment, parentIssueId, sprintId, watchersJson, linkedIssuesJson, timeEstimated, timeSpent, timeRemaining, teamId, newStatus);
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, task.Id, callerId, "TaskUpdated", $"{{\"title\":\"{title}\"}}"));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"board:{task.ProjectId}");
        await _cache.RemoveByPrefixAsync($"board:{task.ProjectId}:");
        await _cache.RemoveByPrefixAsync($"tasks:{task.ProjectId}:");
        return Result<TaskDto>.Success(new TaskDto(task.Id, task.ProjectId, task.ListId, task.Title, task.Description, task.Priority.ToString(), task.LabelsJson, task.AssigneeId, task.Position, task.CreatedAt, task.DueDate, task.IssueType, task.Epic, task.StoryPoints, task.StartDate, task.Environment, task.ParentIssueId, task.SprintId, task.WatchersJson, task.LinkedIssuesJson, task.TimeEstimated, task.TimeSpent, task.TimeRemaining, task.TeamId, task.Status));
    }

    public async Task<Result> MoveTaskAsync(Guid taskId, Guid toListId, int newPosition, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Client") || callerRoles.Contains("Viewer")) return Result.Failure("Forbidden - Client/Viewer cannot move tasks");
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task == null) return Result.Failure("Task not found");
        var targetList = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == toListId, ct);
        if (targetList == null) return Result.Failure("Target list not found");
        var fromListId = task.ListId;
        task.MoveToList(toListId, newPosition, targetList.Name);
        var workspaceId = await _db.Projects.Where(p => p.Id == task.ProjectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync(ct);
        var recipientIds = await _db.ProjectMembers.Where(pm => pm.ProjectId == task.ProjectId).Select(pm => pm.UserId).ToListAsync(ct);
        if (!recipientIds.Any())
        {
            try { recipientIds = await _db.Database.SqlQueryRaw<Guid>("SELECT UserId FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0}", workspaceId).ToListAsync(ct); } catch { }
        }
        var evt = new { TaskId = task.Id, ProjectId = task.ProjectId, WorkspaceId = workspaceId, FromListId = fromListId, ToListId = toListId, Position = newPosition, ActorId = callerId, RecipientUserIds = recipientIds, OccurredOnUtc = DateTime.UtcNow, EventId = Guid.NewGuid(), CorrelationId = Guid.NewGuid().ToString() };
        _db.OutboxMessages.Add(new Domain.Entities.OutboxMessage("TaskMoved", JsonSerializer.Serialize(evt)));
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, task.Id, callerId, "TaskMoved", JsonSerializer.Serialize(new { fromListId, toListId })));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"board:{task.ProjectId}");
        await _cache.RemoveByPrefixAsync($"board:{task.ProjectId}:");
        await _cache.RemoveByPrefixAsync($"tasks:{task.ProjectId}:");
        await _cache.RemoveByPrefixAsync("board:");
        return Result.Success();
    }

    public async Task<Result> DeleteTaskAsync(Guid taskId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Client") || callerRoles.Contains("Viewer")) return Result.Failure("Forbidden - Client/Viewer cannot delete tasks");
        var task = await _db.Tasks.FindAsync(new object[] { taskId }, ct);
        if (task == null) return Result.Failure("Task not found");
        var projectId = task.ProjectId;
        var taskTitle = task.Title;
        var taskIdForLog = task.Id;
        // Audit log before delete — TaskId null for deletion event so FK does not conflict (history remains, FK SetNull)
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(projectId, null, callerId, "TaskDeleted", $"{{\"title\":\"{taskTitle}\",\"taskId\":\"{taskIdForLog}\"}}"));
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(ct);
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"board:{task.ProjectId}");
        await _cache.RemoveByPrefixAsync($"board:{task.ProjectId}:");
        await _cache.RemoveByPrefixAsync($"tasks:{task.ProjectId}:");
        return Result.Success();
    }

    public async Task<PaginatedResult<TaskDto>> GetTasksAsync(Guid projectId, string? search, Guid? assigneeId, string? priority, string? label, DateTime? dueFrom, DateTime? dueTo, string? sortBy, bool sortDesc, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Tasks.Where(t => t.ProjectId == projectId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            q = q.Where(t => t.Title.ToLower().Contains(s) || (t.Description != null && t.Description.ToLower().Contains(s)));
        }
        if (assigneeId.HasValue) q = q.Where(t => t.AssigneeId == assigneeId.Value);
        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<Domain.Enums.TaskPriority>(priority, true, out var pr)) q = q.Where(t => t.Priority == pr);
        if (!string.IsNullOrWhiteSpace(label)) q = q.Where(t => t.LabelsJson != null && t.LabelsJson.Contains(label));
        if (dueFrom.HasValue) q = q.Where(t => t.DueDate >= dueFrom.Value);
        if (dueTo.HasValue) q = q.Where(t => t.DueDate <= dueTo.Value);
        q = sortBy?.ToLower() switch
        {
            "priority" => sortDesc ? q.OrderByDescending(t => t.Priority) : q.OrderBy(t => t.Priority),
            "createdat" => sortDesc ? q.OrderByDescending(t => t.CreatedAt) : q.OrderBy(t => t.CreatedAt),
            _ => sortDesc ? q.OrderByDescending(t => t.Position) : q.OrderBy(t => t.Position)
        };
        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new TaskDto(t.Id, t.ProjectId, t.ListId, t.Title, t.Description, t.Priority.ToString(), t.LabelsJson, t.AssigneeId, t.Position, t.CreatedAt, t.DueDate, t.IssueType, t.Epic, t.StoryPoints, t.StartDate, t.Environment, t.ParentIssueId, t.SprintId, t.WatchersJson, t.LinkedIssuesJson, t.TimeEstimated, t.TimeSpent, t.TimeRemaining, t.TeamId, t.Status))
            .ToListAsync(ct);
        return new PaginatedResult<TaskDto>(items, total, page, pageSize);
    }

    public async Task<TaskDetailDto> GetTaskDetailAsync(Guid taskId, CancellationToken ct = default)
    {
        var t = await _db.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, ct) ?? throw new Exception("Task not found");
        var taskDto = new TaskDto(t.Id, t.ProjectId, t.ListId, t.Title, t.Description, t.Priority.ToString(), t.LabelsJson, t.AssigneeId, t.Position, t.CreatedAt, t.DueDate, t.IssueType, t.Epic, t.StoryPoints, t.StartDate, t.Environment, t.ParentIssueId, t.SprintId, t.WatchersJson, t.LinkedIssuesJson, t.TimeEstimated, t.TimeSpent, t.TimeRemaining, t.TeamId, t.Status);
        var subs = await _db.SubTasks.Where(s => s.TaskId == taskId).OrderBy(s => s.CreatedAt).Select(s => new SubTaskDto(s.Id, s.TaskId, s.Title, s.IsCompleted, s.CreatedAt)).ToListAsync(ct);
        var comments = await _db.Comments.Where(c => c.TaskId == taskId).OrderBy(c => c.CreatedAt).Select(c => new CommentDto(c.Id, c.TaskId, c.AuthorId, c.Content, c.CreatedAt)).ToListAsync(ct);
        return new TaskDetailDto(taskDto, subs, comments);
    }
}
