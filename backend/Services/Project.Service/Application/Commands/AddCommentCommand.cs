using FluentValidation;
using MediatR;
using SharedKernel;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

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
    private readonly ICommentService _service;
    public AddCommentHandler(ICommentService service) => _service = service;
    public Task<Result<CommentDto>> Handle(AddCommentCommand req, CancellationToken ct)
        => _service.AddCommentAsync(req.TaskId, req.Content, req.CallerId, req.CallerRoles, ct);
}
