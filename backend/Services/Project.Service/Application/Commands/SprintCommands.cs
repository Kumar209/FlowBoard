using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// Sprint CRUD - Board → Sprint → Columns. Sprint groups issues for time-box.
/// </summary>
public record CreateSprintCommand(Guid ProjectId, Guid? BoardId, string Name, DateTime StartDate, DateTime EndDate, Guid CallerId, List<string> CallerRoles) : IRequest<Result<SprintDto>>;
public record UpdateSprintCommand(Guid SprintId, string Name, DateTime StartDate, DateTime EndDate, Guid CallerId, List<string> CallerRoles) : IRequest<Result<SprintDto>>;
public record DeleteSprintCommand(Guid SprintId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<bool>>;

public class CreateSprintValidator : AbstractValidator<CreateSprintCommand>
{
    public CreateSprintValidator() { RuleFor(x => x.ProjectId).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(100); RuleFor(x => x.StartDate).NotEmpty(); RuleFor(x => x.EndDate).NotEmpty().GreaterThan(x => x.StartDate); }
}
public class UpdateSprintValidator : AbstractValidator<UpdateSprintCommand>
{
    public UpdateSprintValidator() { RuleFor(x => x.SprintId).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(100); }
}

public class CreateSprintHandler : IRequestHandler<CreateSprintCommand, Result<SprintDto>>
{
    private readonly ISprintService _service;
    public CreateSprintHandler(ISprintService service) => _service = service;
    public Task<Result<SprintDto>> Handle(CreateSprintCommand req, CancellationToken ct)
        => _service.CreateSprintAsync(req.ProjectId, req.BoardId, req.Name, req.StartDate, req.EndDate, req.CallerId, req.CallerRoles, ct);
}
public class UpdateSprintHandler : IRequestHandler<UpdateSprintCommand, Result<SprintDto>>
{
    private readonly ISprintService _service;
    public UpdateSprintHandler(ISprintService service) => _service = service;
    public Task<Result<SprintDto>> Handle(UpdateSprintCommand req, CancellationToken ct)
        => _service.UpdateSprintAsync(req.SprintId, req.Name, req.StartDate, req.EndDate, req.CallerId, req.CallerRoles, ct);
}
public class DeleteSprintHandler : IRequestHandler<DeleteSprintCommand, Result<bool>>
{
    private readonly ISprintService _service;
    public DeleteSprintHandler(ISprintService service) => _service = service;
    public Task<Result<bool>> Handle(DeleteSprintCommand req, CancellationToken ct)
        => _service.DeleteSprintAsync(req.SprintId, req.CallerId, req.CallerRoles, ct);
}
