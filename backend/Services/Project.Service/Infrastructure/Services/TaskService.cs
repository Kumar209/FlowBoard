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

    public async Task<Result<TaskDto>> CreateTaskAsync(Guid projectId, Guid? listId, string title, string? description, string priority, string? labelsJson, Guid? assigneeId, DateTime? dueDate, string? issueType, string? epic, int? storyPoints, DateTime? startDate, string? environment, Guid? parentIssueId, Guid? sprintId, Guid? teamId, Guid callerId, List<string> callerRoles, CancellationToken ct = default, Guid? statusId = null)
    {
        if (Roles.CanUpload(callerRoles) == false) return Result<TaskDto>.Failure("Forbidden - Client cannot create tasks");
        if (assigneeId.HasValue && assigneeId.Value != Guid.Empty)
        {
            if (!await IsAssigneeValidAsync(projectId, assigneeId.Value, ct)) return Result<TaskDto>.Failure("Assignee must be member of organization ∩ workspace ∩ project");
        }
        string status = "To Do";
        Guid? resolvedStatusId = statusId;
        Guid? resolvedListId = listId;
        string? listName = null;
        if (statusId.HasValue && statusId.Value != Guid.Empty)
        {
            var st = await _db.Statuses.FirstOrDefaultAsync(s => s.Id == statusId.Value && s.ProjectId == projectId, ct);
            if (st != null) { status = st.Name; resolvedStatusId = st.Id; }
        }
        else if (listId.HasValue && listId.Value != Guid.Empty)
        {
            var list = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == listId.Value && b.ProjectId == projectId, ct);
            if (list == null) return Result<TaskDto>.Failure("List not found in project");
            status = list.Name;
            listName = list.Name;
            var stByName = await _db.Statuses.FirstOrDefaultAsync(s => s.ProjectId == projectId && s.Name.ToLower() == status.ToLower(), ct);
            if (stByName != null) resolvedStatusId = stByName.Id;
        }
        else
        {
            var stToDo = await _db.Statuses.FirstOrDefaultAsync(s => s.ProjectId == projectId && s.Name.ToLower() == "to do", ct);
            if (stToDo != null) { status = stToDo.Name; resolvedStatusId = stToDo.Id; }
            listName = status;
        }
        var prio = Enum.TryParse<Domain.Enums.TaskPriority>(priority, true, out var p) ? p : Domain.Enums.TaskPriority.Medium;
        int maxPos;
        if (resolvedListId.HasValue && resolvedListId.Value != Guid.Empty)
            maxPos = await _db.Tasks.Where(t => t.ListId == resolvedListId.Value).MaxAsync(t => (int?)t.Position, ct) ?? -1;
        else
            maxPos = await _db.Tasks.Where(t => t.ProjectId == projectId && t.ListId == null).MaxAsync(t => (int?)t.Position, ct) ?? -1;
        var task = new Domain.Entities.TaskItem(projectId, resolvedListId, title, callerId, maxPos + 1, prio, assigneeId, description, labelsJson, dueDate, issueType ?? "Task", epic, storyPoints, startDate, environment, parentIssueId, sprintId, teamId, status, resolvedStatusId);
        _db.Tasks.Add(task);
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        var workspaceId = project?.WorkspaceId ?? await _db.Projects.Where(p => p.Id == projectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync(ct);
        var projectKey = project?.Key ?? "";
        string actorName = callerId.ToString()[..8], actorRole = callerRoles.FirstOrDefault() ?? Roles.Member;
        try { var ar = await _db.Database.SqlQueryRaw<UserNameRow>("SELECT Id as UserId, FullName, Email FROM [identity].[Users] WHERE Id = {0}", callerId).FirstOrDefaultAsync(ct); if (ar != null) actorName = ar.FullName ?? ar.Email ?? actorName; } catch { }
        var recipientIds = await _db.ProjectMembers.Where(pm => pm.ProjectId == projectId).Select(pm => pm.UserId).ToListAsync(ct);
        if (!recipientIds.Any())
        {
            try { recipientIds = await _db.Database.SqlQueryRaw<Guid>("SELECT UserId FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0}", workspaceId).ToListAsync(ct); } catch { }
        }
        var evt = new { TaskId = task.Id, ProjectId = task.ProjectId, WorkspaceId = workspaceId, ProjectKey = projectKey, ListId = task.ListId, ListName = listName ?? status, Title = task.Title, ActorId = callerId, ActorName = actorName, ActorRole = actorRole, RecipientUserIds = recipientIds, OccurredOnUtc = DateTime.UtcNow, EventId = Guid.NewGuid(), CorrelationId = Guid.NewGuid().ToString() };
        _db.OutboxMessages.Add(new Domain.Entities.OutboxMessage("TaskCreated", JsonSerializer.Serialize(evt)));
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(projectId, task.Id, callerId, "TaskCreated", JsonSerializer.Serialize(new { task.Title, status, listName, projectKey, actorName, actorRole }), workspaceId));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"board:{projectId}");
        await _cache.RemoveByPrefixAsync($"board:{projectId}:");
        await _cache.RemoveByPrefixAsync($"tasks:{projectId}:");
        return Result<TaskDto>.Success(new TaskDto(task.Id, task.ProjectId, task.ListId, task.Title, task.Description, task.Priority.ToString(), task.LabelsJson, task.AssigneeId, task.Position, task.CreatedAt, task.DueDate, task.IssueType, task.Epic, task.StoryPoints, task.StartDate, task.Environment, task.ParentIssueId, task.SprintId, task.WatchersJson, task.LinkedIssuesJson, task.TimeEstimated, task.TimeSpent, task.TimeRemaining, task.TeamId, task.Status, task.StatusId, task.AcceptanceCriteriaJson));
    }

    public async Task<Result<TaskDto>> UpdateTaskAsync(Guid taskId, string title, string? description, string priority, string? labelsJson, Guid? assigneeId, DateTime? dueDate, string? issueType, string? epic, int? storyPoints, DateTime? startDate, string? environment, Guid? parentIssueId, Guid? sprintId, string? watchersJson, string? linkedIssuesJson, int? timeEstimated, int? timeSpent, int? timeRemaining, Guid? teamId, Guid? listId, string? status, Guid callerId, List<string> callerRoles, CancellationToken ct = default, Guid? statusId = null, string? acceptanceCriteriaJson = null)
    {
        if (Roles.CanUpload(callerRoles) == false) return Result<TaskDto>.Failure("Forbidden - Client cannot update tasks");
        var task = await _db.Tasks.FindAsync(new object[] { taskId }, ct);
        if (task == null) return Result<TaskDto>.Failure("Task not found");
        if (assigneeId.HasValue && assigneeId.Value != Guid.Empty && assigneeId.Value != task.AssigneeId)
        {
            if (!await IsAssigneeValidAsync(task.ProjectId, assigneeId.Value, ct)) return Result<TaskDto>.Failure("Assignee must be member of organization ∩ workspace ∩ project");
        }
        var prio = Enum.TryParse<Domain.Enums.TaskPriority>(priority, true, out var p) ? p : Domain.Enums.TaskPriority.Medium;
        string? newStatus = status;
        Guid? newStatusId = statusId;
        if (statusId.HasValue && statusId.Value != Guid.Empty)
        {
            var st = await _db.Statuses.FirstOrDefaultAsync(s => s.Id == statusId.Value, ct);
            if (st != null) { newStatus = st.Name; newStatusId = st.Id; }
        }
        Guid? targetListId = listId;
        if (targetListId.HasValue && targetListId.Value != Guid.Empty && targetListId.Value != task.ListId)
        {
            var targetList = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == targetListId.Value, ct);
            if (targetList != null) newStatus = targetList.Name;
            // if moving list and status not explicitly set, try to map list name to status
            if (!newStatusId.HasValue)
            {
                var stByName = await _db.Statuses.FirstOrDefaultAsync(s => s.ProjectId == task.ProjectId && s.Name.ToLower() == newStatus.ToLower(), ct);
                if (stByName != null) newStatusId = stByName.Id;
            }
            task.MoveToList(targetListId.Value, task.Position, newStatus, newStatusId);
        }
        task.Update(title, description, prio, labelsJson, assigneeId, dueDate, issueType, epic, storyPoints, startDate, environment, parentIssueId, sprintId, watchersJson, linkedIssuesJson, timeEstimated, timeSpent, timeRemaining, teamId, newStatus, newStatusId, acceptanceCriteriaJson);
        var updWs = await _db.Projects.Where(p => p.Id == task.ProjectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync(ct);
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, task.Id, callerId, "TaskUpdated", $"{{\"title\":\"{title}\"}}", updWs));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"board:{task.ProjectId}");
        await _cache.RemoveByPrefixAsync($"board:{task.ProjectId}:");
        await _cache.RemoveByPrefixAsync($"tasks:{task.ProjectId}:");
        return Result<TaskDto>.Success(new TaskDto(task.Id, task.ProjectId, task.ListId, task.Title, task.Description, task.Priority.ToString(), task.LabelsJson, task.AssigneeId, task.Position, task.CreatedAt, task.DueDate, task.IssueType, task.Epic, task.StoryPoints, task.StartDate, task.Environment, task.ParentIssueId, task.SprintId, task.WatchersJson, task.LinkedIssuesJson, task.TimeEstimated, task.TimeSpent, task.TimeRemaining, task.TeamId, task.Status, task.StatusId, task.AcceptanceCriteriaJson));
    }

    public async Task<Result> MoveTaskAsync(Guid taskId, Guid toListId, int newPosition, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (Roles.CanUpload(callerRoles) == false) return Result.Failure("Forbidden - Client cannot move tasks");
        // Upstash Redis distributed lock SET NX PX 5000 (MNC-grade for concurrent drag)
        var lockKey = $"lock:task:{taskId}";
        var lockVal = Guid.NewGuid().ToString();
        var acquired = await _cache.TryAcquireLockAsync(lockKey, lockVal, TimeSpan.FromMilliseconds(5000));
        if (!acquired) return Result.Failure("Task is being moved by another user - try again");
        try
        {
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
            if (task == null) return Result.Failure("Task not found");
            var targetList = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == toListId, ct);
            if (targetList == null) return Result.Failure("Target list not found");
        var fromListId = task.ListId;
        var fromList = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == fromListId, ct);
        var fromListName = fromList?.Name ?? fromListId.ToString()[..4];
        var taskTitle = task.Title;
        // Fetch actor name/role for enriched payload (MNC-grade)
        string actorName = callerId.ToString()[..8], actorRole = callerRoles.FirstOrDefault() ?? Roles.Member;
        try
        {
            var actorRow = await _db.Database.SqlQueryRaw<UserNameRow>("SELECT Id as UserId, FullName, Email FROM [identity].[Users] WHERE Id = {0}", callerId).FirstOrDefaultAsync(ct);
            if (actorRow != null) actorName = actorRow.FullName ?? actorRow.Email ?? actorName;
        }
        catch { }
        // Fetch board and sprint names for well-structured payload
        string boardName = "";
        string sprintName = "";
        try
        {
            var boardId = targetList.BoardId ?? fromList?.BoardId;
            if (boardId != null && boardId != Guid.Empty)
            {
                var board = await _db.Boards.FirstOrDefaultAsync(b => b.Id == boardId.Value, ct);
                if (board != null) boardName = board.Name;
            }
        }
        catch { }
        try
        {
            if (task.SprintId.HasValue && task.SprintId.Value != Guid.Empty)
            {
                var sprint = await _db.Sprints.FirstOrDefaultAsync(s => s.Id == task.SprintId.Value, ct);
                if (sprint != null) sprintName = sprint.Name;
            }
        }
        catch { }
        // Resolve target status from column's mapped statuses (one status one column, one column many statuses)
        var targetStatusIds = await _db.BoardColumnStatuses.Where(bcs => bcs.ColumnId == toListId).Select(bcs => bcs.StatusId).ToListAsync(ct);
        Guid? newStatusId = null;
        string? newStatusName = null;
        if (targetStatusIds.Any())
        {
            // If task's current StatusId already in target's mapping, keep it; else pick first status of target column
            if (task.StatusId.HasValue && targetStatusIds.Contains(task.StatusId.Value))
                newStatusId = task.StatusId;
            else
                newStatusId = targetStatusIds.First();
            var st = await _db.Statuses.FirstOrDefaultAsync(s => s.Id == newStatusId.Value, ct);
            newStatusName = st?.Name ?? targetList.Name;
        }
        else
        {
            newStatusName = targetList.Name;
        }
        task.MoveToList(toListId, newPosition, newStatusName, newStatusId);
        var workspaceId = await _db.Projects.Where(p => p.Id == task.ProjectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync(ct);
        var recipientIds = await _db.ProjectMembers.Where(pm => pm.ProjectId == task.ProjectId).Select(pm => pm.UserId).ToListAsync(ct);
        if (!recipientIds.Any())
        {
            try { recipientIds = await _db.Database.SqlQueryRaw<Guid>("SELECT UserId FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0}", workspaceId).ToListAsync(ct); } catch { }
        }
        var evt = new { TaskId = task.Id, ProjectId = task.ProjectId, WorkspaceId = workspaceId, FromListId = fromListId, FromListName = fromListName, ToListId = toListId, ToListName = targetList.Name, BoardName = boardName, SprintName = sprintName, TaskTitle = taskTitle, Position = newPosition, ActorId = callerId, ActorName = actorName, ActorRole = actorRole, RecipientUserIds = recipientIds, OccurredOnUtc = DateTime.UtcNow, EventId = Guid.NewGuid(), CorrelationId = Guid.NewGuid().ToString() };
        _db.OutboxMessages.Add(new Domain.Entities.OutboxMessage("TaskMoved", JsonSerializer.Serialize(evt)));
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, task.Id, callerId, "TaskMoved", JsonSerializer.Serialize(new { fromListId, fromListName, toListId, toListName = targetList.Name, boardName, sprintName, taskTitle, actorName, actorRole }), workspaceId));
            await _db.SaveChangesAsync(ct);
            await _cache.RemoveAsync($"board:{task.ProjectId}");
            await _cache.RemoveByPrefixAsync($"board:{task.ProjectId}:");
            await _cache.RemoveByPrefixAsync($"tasks:{task.ProjectId}:");
            await _cache.RemoveByPrefixAsync("board:");
            return Result.Success();
        }
        finally
        {
            await _cache.ReleaseLockAsync(lockKey, lockVal);
        }
    }

    public async Task<Result> DeleteTaskAsync(Guid taskId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (Roles.CanUpload(callerRoles) == false) return Result.Failure("Forbidden - Client cannot delete tasks");
        var task = await _db.Tasks.FindAsync(new object[] { taskId }, ct);
        if (task == null) return Result.Failure("Task not found");
        var projectId = task.ProjectId;
        var taskTitle = task.Title;
        var taskIdForLog = task.Id;
        var delWs = await _db.Projects.Where(p => p.Id == projectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync(ct);
        // Audit log before delete — TaskId null for deletion event so FK does not conflict (history remains, FK SetNull)
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(projectId, null, callerId, "TaskDeleted", $"{{\"title\":\"{taskTitle}\",\"taskId\":\"{taskIdForLog}\"}}", delWs));
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(ct); // single txn — ActivityLog + Task delete atomic (2A)
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
            .Select(t => new TaskDto(t.Id, t.ProjectId, t.ListId, t.Title, t.Description, t.Priority.ToString(), t.LabelsJson, t.AssigneeId, t.Position, t.CreatedAt, t.DueDate, t.IssueType, t.Epic, t.StoryPoints, t.StartDate, t.Environment, t.ParentIssueId, t.SprintId, t.WatchersJson, t.LinkedIssuesJson, t.TimeEstimated, t.TimeSpent, t.TimeRemaining, t.TeamId, t.Status, t.StatusId, t.AcceptanceCriteriaJson))
            .ToListAsync(ct);
        return new PaginatedResult<TaskDto>(items, total, page, pageSize);
    }

    public async Task<TaskDetailDto> GetTaskDetailAsync(Guid taskId, CancellationToken ct = default)
    {
        var t = await _db.Tasks.FirstOrDefaultAsync(x => x.Id == taskId, ct) ?? throw new Exception("Task not found");
        var taskDto = new TaskDto(t.Id, t.ProjectId, t.ListId, t.Title, t.Description, t.Priority.ToString(), t.LabelsJson, t.AssigneeId, t.Position, t.CreatedAt, t.DueDate, t.IssueType, t.Epic, t.StoryPoints, t.StartDate, t.Environment, t.ParentIssueId, t.SprintId, t.WatchersJson, t.LinkedIssuesJson, t.TimeEstimated, t.TimeSpent, t.TimeRemaining, t.TeamId, t.Status, t.StatusId, t.AcceptanceCriteriaJson);
        var subs = await _db.SubTasks.Where(s => s.TaskId == taskId).OrderBy(s => s.CreatedAt).Select(s => new SubTaskDto(s.Id, s.TaskId, s.Title, s.IsCompleted, s.CreatedAt)).ToListAsync(ct);
        var comments = await _db.Comments.Where(c => c.TaskId == taskId).OrderBy(c => c.CreatedAt).Select(c => new CommentDto(c.Id, c.TaskId, c.AuthorId, c.Content, c.CreatedAt)).ToListAsync(ct);
        return new TaskDetailDto(taskDto, subs, comments);
    }

    private async Task<bool> IsAssigneeValidAsync(Guid projectId, Guid assigneeId, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project == null) return false;
        var wsId = project.WorkspaceId;
        Guid orgId = Guid.Empty;
        try { var orgRow = await _db.Database.SqlQueryRaw<GuidRow>("SELECT OrganizationId as Value FROM [identity].[Workspaces] WHERE Id = {0}", wsId).FirstOrDefaultAsync(ct); if (orgRow != null) orgId = orgRow.Value; } catch { return false; }
        if (orgId == Guid.Empty) return false;
        List<Guid> orgIds = new(), wsIds = new(), projIds = new();
        try { orgIds = await _db.Database.SqlQueryRaw<Guid>("SELECT UserId FROM [identity].[OrganizationMembers] WHERE OrganizationId = {0} UNION SELECT OwnerId FROM [identity].[Organizations] WHERE Id = {0}", orgId).ToListAsync(ct); } catch { }
        try { wsIds = await _db.Database.SqlQueryRaw<Guid>("SELECT UserId FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0}", wsId).ToListAsync(ct); } catch { }
        try { projIds = await _db.ProjectMembers.Where(pm => pm.ProjectId == projectId).Select(pm => pm.UserId).ToListAsync(ct); } catch { }
        var candidates = orgIds.Intersect(wsIds).Intersect(projIds).ToList();
        return candidates.Contains(assigneeId);
    }
    private class GuidRow { public Guid Value { get; set; } }
    private class UserNameRow { public Guid UserId { get; set; } public string? FullName { get; set; } public string? Email { get; set; } }
}
