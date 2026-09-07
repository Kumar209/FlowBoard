using FluentValidation;
using MediatR;
using SharedKernel;
using Identity.Service.Application.Interfaces;

namespace Identity.Service.Application.Commands;

public record GetMyWorkspacesQuery(Guid UserId, int Page, int PageSize, string? Search) : IRequest<Result<(List<WorkspaceDto> Items, int Total)>>;
public record CreateWorkspaceCommand(Guid OrganizationId, string Name, Guid CallerId) : IRequest<Result<WorkspaceDto>>;
public record InviteMemberCommand(Guid WorkspaceId, string Email, string Role, Guid CallerId) : IRequest<Result<WorkspaceMemberDto>>;
public record UpdateWorkspaceCommand(Guid WorkspaceId, string Name, string? Slug, Guid CallerId) : IRequest<Result<WorkspaceDto>>;
public record DeleteWorkspaceCommand(Guid WorkspaceId, Guid CallerId) : IRequest<Result>;
public record GetWorkspaceMembersQuery(Guid WorkspaceId, Guid CallerId) : IRequest<Result<List<WorkspaceMemberDto>>>;
public record ChangeMemberRoleCommand(Guid WorkspaceId, Guid UserId, string Role, Guid CallerId) : IRequest<Result<WorkspaceMemberDto>>;

public class GetMyWorkspacesValidator : AbstractValidator<GetMyWorkspacesQuery>
{
    public GetMyWorkspacesValidator() { RuleFor(x => x.UserId).NotEmpty(); RuleFor(x => x.Page).GreaterThan(0); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); }
}
public class CreateWorkspaceValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceValidator() { RuleFor(x => x.OrganizationId).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(200); }
}
public class InviteMemberValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberValidator() { RuleFor(x => x.WorkspaceId).NotEmpty(); RuleFor(x => x.Email).NotEmpty().EmailAddress(); RuleFor(x => x.Role).NotEmpty(); }
}
public class UpdateWorkspaceValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceValidator() { RuleFor(x => x.WorkspaceId).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(200); }
}

public class GetMyWorkspacesHandler : IRequestHandler<GetMyWorkspacesQuery, Result<(List<WorkspaceDto> Items, int Total)>>
{
    private readonly IWorkspaceService _service;
    public GetMyWorkspacesHandler(IWorkspaceService service) => _service = service;
    public async Task<Result<(List<WorkspaceDto> Items, int Total)>> Handle(GetMyWorkspacesQuery req, CancellationToken ct)
    {
        try
        {
            var (items, total) = await _service.GetMyWorkspacesAsync(req.UserId, req.Page, req.PageSize, req.Search, ct);
            return Result<(List<WorkspaceDto> Items, int Total)>.Success((items, total));
        }
        catch (Exception ex) { return Result<(List<WorkspaceDto> Items, int Total)>.Failure(ex.Message); }
    }
}
public class CreateWorkspaceHandler : IRequestHandler<CreateWorkspaceCommand, Result<WorkspaceDto>>
{
    private readonly IWorkspaceService _service;
    public CreateWorkspaceHandler(IWorkspaceService service) => _service = service;
    public async Task<Result<WorkspaceDto>> Handle(CreateWorkspaceCommand req, CancellationToken ct)
    {
        try { var dto = await _service.CreateWorkspaceAsync(req.OrganizationId, req.Name, req.CallerId, ct); return Result<WorkspaceDto>.Success(dto); }
        catch (Exception ex) { return Result<WorkspaceDto>.Failure(ex.Message); }
    }
}
public class InviteMemberHandler : IRequestHandler<InviteMemberCommand, Result<WorkspaceMemberDto>>
{
    private readonly IWorkspaceService _service;
    public InviteMemberHandler(IWorkspaceService service) => _service = service;
    public async Task<Result<WorkspaceMemberDto>> Handle(InviteMemberCommand req, CancellationToken ct)
    {
        try { var dto = await _service.InviteAsync(req.WorkspaceId, req.Email, req.Role, req.CallerId, ct); return Result<WorkspaceMemberDto>.Success(dto); }
        catch (Exception ex) { return Result<WorkspaceMemberDto>.Failure(ex.Message); }
    }
}
public class UpdateWorkspaceHandler : IRequestHandler<UpdateWorkspaceCommand, Result<WorkspaceDto>>
{
    private readonly IWorkspaceService _service;
    public UpdateWorkspaceHandler(IWorkspaceService service) => _service = service;
    public async Task<Result<WorkspaceDto>> Handle(UpdateWorkspaceCommand req, CancellationToken ct)
    {
        try { var dto = await _service.UpdateWorkspaceAsync(req.WorkspaceId, req.Name, req.Slug, req.CallerId, ct); return Result<WorkspaceDto>.Success(dto); }
        catch (Exception ex) { return Result<WorkspaceDto>.Failure(ex.Message); }
    }
}
public class DeleteWorkspaceHandler : IRequestHandler<DeleteWorkspaceCommand, Result>
{
    private readonly IWorkspaceService _service;
    public DeleteWorkspaceHandler(IWorkspaceService service) => _service = service;
    public async Task<Result> Handle(DeleteWorkspaceCommand req, CancellationToken ct)
    {
        try { await _service.DeleteWorkspaceAsync(req.WorkspaceId, req.CallerId, ct); return Result.Success(); }
        catch (Exception ex) { return Result.Failure(ex.Message); }
    }
}
public class GetWorkspaceMembersHandler : IRequestHandler<GetWorkspaceMembersQuery, Result<List<WorkspaceMemberDto>>>
{
    private readonly IWorkspaceService _service;
    public GetWorkspaceMembersHandler(IWorkspaceService service) => _service = service;
    public async Task<Result<List<WorkspaceMemberDto>>> Handle(GetWorkspaceMembersQuery req, CancellationToken ct)
    {
        try { var list = await _service.GetMembersAsync(req.WorkspaceId, req.CallerId, ct); return Result<List<WorkspaceMemberDto>>.Success(list); }
        catch (Exception ex) { return Result<List<WorkspaceMemberDto>>.Failure(ex.Message); }
    }
}
public class ChangeMemberRoleHandler : IRequestHandler<ChangeMemberRoleCommand, Result<WorkspaceMemberDto>>
{
    private readonly IWorkspaceService _service;
    public ChangeMemberRoleHandler(IWorkspaceService service) => _service = service;
    public async Task<Result<WorkspaceMemberDto>> Handle(ChangeMemberRoleCommand req, CancellationToken ct)
    {
        try { var dto = await _service.ChangeRoleAsync(req.WorkspaceId, req.UserId, req.Role, req.CallerId, ct); return Result<WorkspaceMemberDto>.Success(dto); }
        catch (Exception ex) { return Result<WorkspaceMemberDto>.Failure(ex.Message); }
    }
}
