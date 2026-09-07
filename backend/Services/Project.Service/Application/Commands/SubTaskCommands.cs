using FluentValidation;
using MediatR;
using SharedKernel;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// Subtasks CRUD - Jira checklist inside Task. POST /tasks/{id}/subtasks, toggle, rename, delete. Handlers delegate to ISubTaskService (MNC-grade DIP, like SprintCommands).
/// </summary>
public record CreateSubTaskCommand(Guid TaskId, string Title, Guid CallerId, List<string> CallerRoles) : IRequest<Result<SubTaskDto>>;
public record UpdateSubTaskCommand(Guid SubTaskId, string Title, Guid CallerId) : IRequest<Result<SubTaskDto>>;
public record ToggleSubTaskCommand(Guid SubTaskId, Guid CallerId) : IRequest<Result<SubTaskDto>>;
public record DeleteSubTaskCommand(Guid SubTaskId, Guid CallerId) : IRequest<Result<bool>>;

public class CreateSubTaskValidator : AbstractValidator<CreateSubTaskCommand>
{
    public CreateSubTaskValidator() { RuleFor(x => x.TaskId).NotEmpty(); RuleFor(x => x.Title).NotEmpty().MaximumLength(300); }
}
public class UpdateSubTaskValidator : AbstractValidator<UpdateSubTaskCommand>
{
    public UpdateSubTaskValidator() { RuleFor(x => x.SubTaskId).NotEmpty(); RuleFor(x => x.Title).NotEmpty().MaximumLength(300); }
}

public class CreateSubTaskHandler : IRequestHandler<CreateSubTaskCommand, Result<SubTaskDto>>
{
    private readonly ISubTaskService _service;
    public CreateSubTaskHandler(ISubTaskService service) => _service = service;
    public Task<Result<SubTaskDto>> Handle(CreateSubTaskCommand req, CancellationToken ct)
        => _service.CreateSubTaskAsync(req.TaskId, req.Title, req.CallerId, req.CallerRoles, ct);
}

public class UpdateSubTaskHandler : IRequestHandler<UpdateSubTaskCommand, Result<SubTaskDto>>
{
    private readonly ISubTaskService _service;
    public UpdateSubTaskHandler(ISubTaskService service) => _service = service;
    public Task<Result<SubTaskDto>> Handle(UpdateSubTaskCommand req, CancellationToken ct)
        => _service.UpdateSubTaskAsync(req.SubTaskId, req.Title, req.CallerId, ct);
}

public class ToggleSubTaskHandler : IRequestHandler<ToggleSubTaskCommand, Result<SubTaskDto>>
{
    private readonly ISubTaskService _service;
    public ToggleSubTaskHandler(ISubTaskService service) => _service = service;
    public Task<Result<SubTaskDto>> Handle(ToggleSubTaskCommand req, CancellationToken ct)
        => _service.ToggleSubTaskAsync(req.SubTaskId, req.CallerId, ct);
}

public class DeleteSubTaskHandler : IRequestHandler<DeleteSubTaskCommand, Result<bool>>
{
    private readonly ISubTaskService _service;
    public DeleteSubTaskHandler(ISubTaskService service) => _service = service;
    public Task<Result<bool>> Handle(DeleteSubTaskCommand req, CancellationToken ct)
        => _service.DeleteSubTaskAsync(req.SubTaskId, req.CallerId, ct);
}
