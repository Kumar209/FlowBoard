using FluentValidation;
using MediatR;
using SharedKernel;
using Project.Service.Application.Caching;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

public record DeleteTaskCommand(Guid TaskId, Guid CallerId, List<string> CallerRoles) : IRequest<Result>;

public class DeleteTaskValidator : AbstractValidator<DeleteTaskCommand>
{
    public DeleteTaskValidator() { RuleFor(x => x.TaskId).NotEmpty(); }
}

public class DeleteTaskHandler : IRequestHandler<DeleteTaskCommand, Result>
{
    private readonly ITaskService _service;
    public DeleteTaskHandler(ITaskService service) => _service = service;
    public Task<Result> Handle(DeleteTaskCommand req, CancellationToken ct)
        => _service.DeleteTaskAsync(req.TaskId, req.CallerId, req.CallerRoles, ct);
}
