using FluentValidation;
using MediatR;
using SharedKernel;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;
using System.Text.Json;

namespace Project.Service.Application.Commands;

/// <summary>
/// AddComment - discussion on task. Content 5000 max. Allowed all authenticated including Client (external view+comment). Publishes TaskCommented via Outbox.
/// </summary>
public record AddCommentCommand(Guid TaskId, string Content, Guid CallerId, List<string> CallerRoles) : IRequest<Result<CommentDto>>;

public class AddCommentValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(5000);
    }
}

public class AddCommentHandler : IRequestHandler<AddCommentCommand, Result<CommentDto>>
{
    private readonly IApplicationDbContext _db;
    public AddCommentHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<CommentDto>> Handle(AddCommentCommand req, CancellationToken ct)
    {
        var task = await _db.Tasks.FindAsync(new object[] { req.TaskId }, ct);
        if (task == null) return Result<CommentDto>.Failure("Task not found");

        var comment = new Domain.Entities.Comment(req.TaskId, req.CallerId, req.Content);
        _db.Comments.Add(comment);

        var evt = new { TaskId = req.TaskId, ProjectId = task.ProjectId, CommentId = comment.Id, ActorId = req.CallerId, OccurredOnUtc = DateTime.UtcNow, EventId = Guid.NewGuid(), CorrelationId = Guid.NewGuid().ToString() };
        _db.OutboxMessages.Add(new Domain.Entities.OutboxMessage("TaskCommented", JsonSerializer.Serialize(evt)));
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(task.ProjectId, req.TaskId, req.CallerId, "TaskCommented", JsonSerializer.Serialize(new { req.Content })));

        await _db.SaveChangesAsync(ct);
        return Result<CommentDto>.Success(new CommentDto(comment.Id, comment.TaskId, comment.AuthorId, comment.Content, comment.CreatedAt));
    }
}
