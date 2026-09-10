using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// UpdateBoardList - Rename list (Jira Kanban). Viewer/Client 403.
/// </summary>
public record UpdateBoardListCommand(Guid ProjectId, Guid ListId, string Name, int? Position, Guid CallerId, List<string> CallerRoles, List<Guid>? StatusIds = null) : IRequest<Result<BoardListDto>>;

public class UpdateBoardListValidator : AbstractValidator<UpdateBoardListCommand>
{
    public UpdateBoardListValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ListId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateBoardListHandler : IRequestHandler<UpdateBoardListCommand, Result<BoardListDto>>
{
    private readonly IBoardService _service;
    public UpdateBoardListHandler(IBoardService service) => _service = service;
    public Task<Result<BoardListDto>> Handle(UpdateBoardListCommand req, CancellationToken ct)
        => _service.UpdateBoardListAsync(req.ProjectId, req.ListId, req.Name, req.Position, req.CallerId, req.CallerRoles, ct, req.StatusIds);
}

public record DeleteBoardListCommand(Guid ProjectId, Guid ListId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<bool>>;

public class DeleteBoardListHandler : IRequestHandler<DeleteBoardListCommand, Result<bool>>
{
    private readonly IBoardService _service;
    public DeleteBoardListHandler(IBoardService service) => _service = service;
    public Task<Result<bool>> Handle(DeleteBoardListCommand req, CancellationToken ct)
        => _service.DeleteBoardListAsync(req.ProjectId, req.ListId, req.CallerId, req.CallerRoles, ct);
}
