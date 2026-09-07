using Microsoft.EntityFrameworkCore;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;
using SharedKernel;
using System.Text.Json;

namespace Project.Service.Infrastructure.Services;

public class CommentService : ICommentService
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public CommentService(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }

    public async Task<Result<CommentDto>> AddCommentAsync(Guid taskId, string content, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        var task = await _db.Tasks.FindAsync(new object[] { taskId }, ct);
        if (task == null) return Result<CommentDto>.Failure("Task not found");
        var comment = new Domain.Entities.Comment(taskId, callerId, content);
        _db.Comments.Add(comment);
        var workspaceId = await _db.Projects.Where(p => p.Id == task.ProjectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync(ct);
        var recipientIds = await _db.ProjectMembers.Where(pm => pm.ProjectId == task.ProjectId).Select(pm => pm.UserId).ToListAsync(ct);
        if (!recipientIds.Any())
        {
            try { recipientIds = await _db.Database.SqlQueryRaw<Guid>("SELECT UserId FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0}", workspaceId).ToListAsync(ct); } catch { }
        }
        var evt = new { TaskId = taskId, ProjectId = task.ProjectId, WorkspaceId = workspaceId, CommentId = comment.Id, ActorId = callerId, RecipientUserIds = recipientIds, OccurredOnUtc = DateTime.UtcNow, EventId = Guid.NewGuid(), CorrelationId = Guid.NewGuid().ToString() };
        _db.OutboxMessages.Add(new Domain.Entities.OutboxMessage("TaskCommented", JsonSerializer.Serialize(evt)));
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, taskId, callerId, "TaskCommented", JsonSerializer.Serialize(new { content })));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        return Result<CommentDto>.Success(new CommentDto(comment.Id, comment.TaskId, comment.AuthorId, comment.Content, comment.CreatedAt));
    }

    public async Task<Result<CommentDto>> UpdateCommentAsync(Guid commentId, string content, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        var comment = await _db.Comments.FindAsync(new object[] { commentId }, ct);
        if (comment == null) return Result<CommentDto>.Failure("Comment not found");
        var isAdmin = callerRoles.Contains("OrgAdmin") || callerRoles.Contains("SuperAdmin");
        if (comment.AuthorId != callerId && !isAdmin) return Result<CommentDto>.Failure("Forbidden - only author or OrgAdmin can edit");
        comment.Edit(content);
        var task = await _db.Tasks.FindAsync(new object[] { comment.TaskId }, ct);
        if (task != null) _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, comment.TaskId, callerId, "CommentUpdated", JsonSerializer.Serialize(new { content })));
        await _db.SaveChangesAsync(ct);
        if (task != null) await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        return Result<CommentDto>.Success(new CommentDto(comment.Id, comment.TaskId, comment.AuthorId, comment.Content, comment.CreatedAt));
    }

    public async Task<Result<bool>> DeleteCommentAsync(Guid commentId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        var comment = await _db.Comments.FindAsync(new object[] { commentId }, ct);
        if (comment == null) return Result<bool>.Failure("Comment not found");
        var isAdmin = callerRoles.Contains("OrgAdmin") || callerRoles.Contains("SuperAdmin");
        if (comment.AuthorId != callerId && !isAdmin) return Result<bool>.Failure("Forbidden - only author or OrgAdmin can delete");
        var task = await _db.Tasks.FindAsync(new object[] { comment.TaskId }, ct);
        _db.Comments.Remove(comment);
        if (task != null) _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, comment.TaskId, callerId, "CommentDeleted", "{}"));
        await _db.SaveChangesAsync(ct);
        if (task != null) await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        return Result<bool>.Success(true);
    }

    public async Task<List<CommentDto>> GetCommentsAsync(Guid taskId, CancellationToken ct = default)
    {
        return await _db.Comments.Where(c => c.TaskId == taskId).OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(c.Id, c.TaskId, c.AuthorId, c.Content, c.CreatedAt))
            .ToListAsync(ct);
    }
}
