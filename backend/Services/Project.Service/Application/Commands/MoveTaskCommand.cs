using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Caching;
using Project.Service.Application.Interfaces;
using System.Text.Json;

namespace Project.Service.Application.Commands;

/// <summary>
/// MoveTask - drag-drop between lists (CDK) or reorder inside same list. FromListId+ToListId+Position. Allowed Member+ (Client 403). Publishes TaskMoved via Outbox for realtime SignalR (Task 3.2) + invalidates Redis board:{projectId} (Task 2.3). Uses Position reordering.
/// </summary>
public record MoveTaskCommand(Guid TaskId, Guid ToListId, int NewPosition, Guid CallerId, List<string> CallerRoles) : IRequest<Result>;

public class MoveTaskValidator : AbstractValidator<MoveTaskCommand>
{
    public MoveTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ToListId).NotEmpty();
        RuleFor(x => x.NewPosition).GreaterThanOrEqualTo(0);
    }
}

public class MoveTaskHandler : IRequestHandler<MoveTaskCommand, Result>
{
    private readonly ITaskService _service;
    public MoveTaskHandler(ITaskService service) => _service = service;
    public Task<Result> Handle(MoveTaskCommand req, CancellationToken ct)
        => _service.MoveTaskAsync(req.TaskId, req.ToListId, req.NewPosition, req.CallerId, req.CallerRoles, ct);
}
