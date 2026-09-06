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
/// Comments CRUD - Viewer cannot comment, Client can. Update/Delete only author or OrgAdmin.
/// </summary>
public record UpdateCommentCommand(Guid CommentId, string Content, Guid CallerId, List<string> CallerRoles) : IRequest<Result<CommentDto>>;
public record DeleteCommentCommand(Guid CommentId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<bool>>;

public class UpdateCommentValidator : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentValidator() { RuleFor(x => x.CommentId).NotEmpty(); RuleFor(x => x.Content).NotEmpty().MaximumLength(5000); }
}

public class UpdateCommentHandler : IRequestHandler<UpdateCommentCommand, Result<CommentDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public UpdateCommentHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }
    public async Task<Result<CommentDto>> Handle(UpdateCommentCommand req, CancellationToken ct)
    {
        var comment = await _db.Comments.FindAsync(new object[] { req.CommentId }, ct);
        if (comment == null) return Result<CommentDto>.Failure("Comment not found");
        // Only author can edit (or OrgAdmin/SuperAdmin)
        var isAdmin = req.CallerRoles.Contains("OrgAdmin") || req.CallerRoles.Contains("SuperAdmin");
        if (comment.AuthorId != req.CallerId && !isAdmin)
            return Result<CommentDto>.Failure("Forbidden - only author or OrgAdmin can edit");
        comment.Edit(req.Content);
        var task = await _db.Tasks.FindAsync(new object[] { comment.TaskId }, ct);
        if (task != null) _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, comment.TaskId, req.CallerId, "CommentUpdated", JsonSerializer.Serialize(new { req.Content })));
        await _db.SaveChangesAsync(ct);
        if (task != null) await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        return Result<CommentDto>.Success(new CommentDto(comment.Id, comment.TaskId, comment.AuthorId, comment.Content, comment.CreatedAt));
    }
}

public class DeleteCommentHandler : IRequestHandler<DeleteCommentCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public DeleteCommentHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }
    public async Task<Result<bool>> Handle(DeleteCommentCommand req, CancellationToken ct)
    {
        var comment = await _db.Comments.FindAsync(new object[] { req.CommentId }, ct);
        if (comment == null) return Result<bool>.Failure("Comment not found");
        var isAdmin = req.CallerRoles.Contains("OrgAdmin") || req.CallerRoles.Contains("SuperAdmin");
        if (comment.AuthorId != req.CallerId && !isAdmin)
            return Result<bool>.Failure("Forbidden - only author or OrgAdmin can delete");
        var task = await _db.Tasks.FindAsync(new object[] { comment.TaskId }, ct);
        _db.Comments.Remove(comment);
        if (task != null) _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, comment.TaskId, req.CallerId, "CommentDeleted", "{}"));
        await _db.SaveChangesAsync(ct);
        if (task != null) await _cache.RemoveAsync(CacheKeys.Board(task.ProjectId));
        return Result<bool>.Success(true);
    }
}
