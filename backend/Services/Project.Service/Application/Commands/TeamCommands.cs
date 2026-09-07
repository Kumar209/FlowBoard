using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

public record CreateTeamCommand(Guid ProjectId, string Name, string? Description, Guid CallerId) : IRequest<Result<TeamDto>>;
public record UpdateTeamCommand(Guid TeamId, string Name, string? Description, Guid CallerId) : IRequest<Result<TeamDto>>;
public record DeleteTeamCommand(Guid TeamId, Guid CallerId) : IRequest<Result<object>>;
public record AddTeamMemberCommand(Guid TeamId, Guid UserId, Guid CallerId) : IRequest<Result<TeamMemberDto>>;
public record RemoveTeamMemberCommand(Guid TeamId, Guid UserId, Guid CallerId) : IRequest<Result<object>>;

public class CreateTeamValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(100); RuleFor(x => x.ProjectId).NotEmpty(); }
}
public class UpdateTeamValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(100); }
}

public class CreateTeamHandler : IRequestHandler<CreateTeamCommand, Result<TeamDto>>
{
    private readonly ITeamService _service;
    public CreateTeamHandler(ITeamService service) => _service = service;
    public Task<Result<TeamDto>> Handle(CreateTeamCommand req, CancellationToken ct)
        => _service.CreateTeamAsync(req.ProjectId, req.Name, req.Description, req.CallerId, ct);
}

public class UpdateTeamHandler : IRequestHandler<UpdateTeamCommand, Result<TeamDto>>
{
    private readonly ITeamService _service;
    public UpdateTeamHandler(ITeamService service) => _service = service;
    public Task<Result<TeamDto>> Handle(UpdateTeamCommand req, CancellationToken ct)
        => _service.UpdateTeamAsync(req.TeamId, req.Name, req.Description, req.CallerId, ct);
}

public class DeleteTeamHandler : IRequestHandler<DeleteTeamCommand, Result<object>>
{
    private readonly ITeamService _service;
    public DeleteTeamHandler(ITeamService service) => _service = service;
    public Task<Result<object>> Handle(DeleteTeamCommand req, CancellationToken ct)
        => _service.DeleteTeamAsync(req.TeamId, req.CallerId, ct).ContinueWith(t => t.Result.IsSuccess ? Result<object>.Success(new { message = "Deleted" }) : Result<object>.Failure(t.Result.Error!), ct);
}

public class AddTeamMemberHandler : IRequestHandler<AddTeamMemberCommand, Result<TeamMemberDto>>
{
    private readonly ITeamService _service;
    public AddTeamMemberHandler(ITeamService service) => _service = service;
    public Task<Result<TeamMemberDto>> Handle(AddTeamMemberCommand req, CancellationToken ct)
        => _service.AddMemberAsync(req.TeamId, req.UserId, req.CallerId, ct);
}

public class RemoveTeamMemberHandler : IRequestHandler<RemoveTeamMemberCommand, Result<object>>
{
    private readonly ITeamService _service;
    public RemoveTeamMemberHandler(ITeamService service) => _service = service;
    public Task<Result<object>> Handle(RemoveTeamMemberCommand req, CancellationToken ct)
        => _service.RemoveMemberAsync(req.TeamId, req.UserId, req.CallerId, ct).ContinueWith(t => t.Result.IsSuccess ? Result<object>.Success(new { message = "Removed" }) : Result<object>.Failure(t.Result.Error!), ct);
}
