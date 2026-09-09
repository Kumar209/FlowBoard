using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// CreateBoardList - column in Project board (To Do/In Progress/Done). Position = max+1. Allowed for Member+ (any workspace member) - Viewer read-only check at controller if needed.
/// </summary>
public record CreateBoardListCommand(Guid ProjectId, string Name, Guid CallerId, List<string> CallerRoles, Guid? BoardId = null, int? Position = null, List<Guid>? StatusIds = null) : IRequest<Result<BoardListDto>>;

public class CreateBoardListValidator : AbstractValidator<CreateBoardListCommand>
{
    public CreateBoardListValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateBoardListHandler : IRequestHandler<CreateBoardListCommand, Result<BoardListDto>>
{
    private readonly IBoardService _service;
    public CreateBoardListHandler(IBoardService service) => _service = service;
    public Task<Result<BoardListDto>> Handle(CreateBoardListCommand req, CancellationToken ct)
        => _service.CreateBoardListAsync(req.ProjectId, req.Name, req.CallerId, req.CallerRoles, req.BoardId, req.Position, req.StatusIds, ct);
}
