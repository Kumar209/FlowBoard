using FluentValidation;
using MediatR;
using SharedKernel;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// Comments CRUD - Viewer cannot comment, Client can. Update/Delete only author or OrgAdmin. Handler delegates to ICommentService (MNC-grade DIP).
/// </summary>
public record UpdateCommentCommand(Guid CommentId, string Content, Guid CallerId, List<string> CallerRoles) : IRequest<Result<CommentDto>>;
public record DeleteCommentCommand(Guid CommentId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<bool>>;

public class UpdateCommentValidator : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentValidator() { RuleFor(x => x.CommentId).NotEmpty(); RuleFor(x => x.Content).NotEmpty().MaximumLength(5000); }
}

public class UpdateCommentHandler : IRequestHandler<UpdateCommentCommand, Result<CommentDto>>
{
    private readonly ICommentService _service;
    public UpdateCommentHandler(ICommentService service) => _service = service;
    public Task<Result<CommentDto>> Handle(UpdateCommentCommand req, CancellationToken ct)
        => _service.UpdateCommentAsync(req.CommentId, req.Content, req.CallerId, req.CallerRoles, ct);
}

public class DeleteCommentHandler : IRequestHandler<DeleteCommentCommand, Result<bool>>
{
    private readonly ICommentService _service;
    public DeleteCommentHandler(ICommentService service) => _service = service;
    public Task<Result<bool>> Handle(DeleteCommentCommand req, CancellationToken ct)
        => _service.DeleteCommentAsync(req.CommentId, req.CallerId, req.CallerRoles, ct);
}
