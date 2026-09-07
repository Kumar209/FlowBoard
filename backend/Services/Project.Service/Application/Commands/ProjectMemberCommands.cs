using FluentValidation;
using MediatR;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;
using SharedKernel;

namespace Project.Service.Application.Commands;

public record GetProjectMembersQuery(Guid ProjectId, int Page = 1, int PageSize = 20, string? Search = null) : IRequest<PaginatedResult<ProjectMemberDto>>;
public record AddProjectMemberCommand(Guid ProjectId, Guid UserId, string Role, Guid CallerId, List<string> CallerRoles) : IRequest<Result<ProjectMemberDto>>;
public record RemoveProjectMemberCommand(Guid ProjectId, Guid UserId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<bool>>;

public class GetProjectMembersValidator : AbstractValidator<GetProjectMembersQuery>
{
    public GetProjectMembersValidator() { RuleFor(x => x.ProjectId).NotEmpty(); RuleFor(x => x.Page).GreaterThan(0); }
}
public class AddProjectMemberValidator : AbstractValidator<AddProjectMemberCommand>
{
    public AddProjectMemberValidator() { RuleFor(x => x.ProjectId).NotEmpty(); RuleFor(x => x.UserId).NotEmpty(); }
}

public class GetProjectMembersHandler : IRequestHandler<GetProjectMembersQuery, PaginatedResult<ProjectMemberDto>>
{
    private readonly IProjectMemberService _service;
    public GetProjectMembersHandler(IProjectMemberService service) => _service = service;
    public Task<PaginatedResult<ProjectMemberDto>> Handle(GetProjectMembersQuery req, CancellationToken ct)
        => _service.GetMembersAsync(req.ProjectId, req.Page, req.PageSize, req.Search, ct);
}
public class AddProjectMemberHandler : IRequestHandler<AddProjectMemberCommand, Result<ProjectMemberDto>>
{
    private readonly IProjectMemberService _service;
    public AddProjectMemberHandler(IProjectMemberService service) => _service = service;
    public Task<Result<ProjectMemberDto>> Handle(AddProjectMemberCommand req, CancellationToken ct)
        => _service.AddMemberAsync(req.ProjectId, req.UserId, req.Role, req.CallerId, req.CallerRoles, ct);
}
public class RemoveProjectMemberHandler : IRequestHandler<RemoveProjectMemberCommand, Result<bool>>
{
    private readonly IProjectMemberService _service;
    public RemoveProjectMemberHandler(IProjectMemberService service) => _service = service;
    public Task<Result<bool>> Handle(RemoveProjectMemberCommand req, CancellationToken ct)
        => _service.RemoveMemberAsync(req.ProjectId, req.UserId, req.CallerId, req.CallerRoles, ct);
}
