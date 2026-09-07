using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// Board CRUD - Enterprise: Project has multiple Boards (Engineering/QA/Support) as views. Boards own Columns.
/// </summary>
public record CreateBoardCommand(Guid ProjectId, string Name, string Type, string? Description, Guid CallerId, List<string> CallerRoles, string? FilterJson = null) : IRequest<Result<BoardInfoDto>>;
public record UpdateBoardCommand(Guid BoardId, string Name, string Type, Guid CallerId, List<string> CallerRoles, string? FilterJson = null) : IRequest<Result<BoardInfoDto>>;
public record DeleteBoardCommand(Guid BoardId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<bool>>;

public class CreateBoardValidator : AbstractValidator<CreateBoardCommand>
{
    public CreateBoardValidator() { RuleFor(x => x.ProjectId).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(100); RuleFor(x => x.Type).Must(t => new[]{"Scrum","Kanban"}.Contains(t)).When(x=>!string.IsNullOrEmpty(x.Type)); }
}
public class UpdateBoardValidator : AbstractValidator<UpdateBoardCommand>
{
    public UpdateBoardValidator() { RuleFor(x => x.BoardId).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(100); }
}

public class CreateBoardHandler : IRequestHandler<CreateBoardCommand, Result<BoardInfoDto>>
{
    private readonly IBoardService _service;
    public CreateBoardHandler(IBoardService service) => _service = service;
    public Task<Result<BoardInfoDto>> Handle(CreateBoardCommand req, CancellationToken ct)
        => _service.CreateBoardAsync(req.ProjectId, req.Name, req.Type, req.Description, req.FilterJson, req.CallerId, req.CallerRoles, ct);
}
public class UpdateBoardHandler : IRequestHandler<UpdateBoardCommand, Result<BoardInfoDto>>
{
    private readonly IBoardService _service;
    public UpdateBoardHandler(IBoardService service) => _service = service;
    public Task<Result<BoardInfoDto>> Handle(UpdateBoardCommand req, CancellationToken ct)
        => _service.UpdateBoardAsync(req.BoardId, req.Name, req.Type, req.FilterJson, req.CallerId, req.CallerRoles, ct);
}
public class DeleteBoardHandler : IRequestHandler<DeleteBoardCommand, Result<bool>>
{
    private readonly IBoardService _service;
    public DeleteBoardHandler(IBoardService service) => _service = service;
    public Task<Result<bool>> Handle(DeleteBoardCommand req, CancellationToken ct)
        => _service.DeleteBoardAsync(req.BoardId, req.CallerId, req.CallerRoles, ct);
}
