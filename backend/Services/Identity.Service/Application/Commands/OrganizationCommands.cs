using FluentValidation;
using MediatR;
using SharedKernel;
using Identity.Service.Application.Interfaces;

namespace Identity.Service.Application.Commands;

public record GetMyOrganizationsQuery(Guid UserId) : IRequest<Result<List<OrganizationDto>>>;
public record CreateOrganizationCommand(string Name, string? Description, Guid CallerId) : IRequest<Result<OrganizationDto>>;
public record UpdateOrganizationCommand(Guid OrganizationId, string Name, string? Description, Guid CallerId) : IRequest<Result<OrganizationDto>>;
public record GetOrgMembersQuery(Guid OrganizationId, Guid CallerId) : IRequest<Result<List<OrgMemberDto>>>;
public record CreateEmployeeCommand(Guid OrganizationId, string FullName, string Email, string Password, string Role, List<Guid>? WorkspaceIds, Guid CallerId) : IRequest<Result<OrgMemberDto>>;
public record UpdateEmployeeCommand(Guid OrganizationId, Guid UserId, string? FullName, string? Email, string? Role, List<Guid>? WorkspaceIds, Guid CallerId) : IRequest<Result<OrgMemberDto>>;
public record DeleteEmployeeCommand(Guid OrganizationId, Guid UserId, Guid CallerId) : IRequest<Result>;

public class GetMyOrganizationsHandler : IRequestHandler<GetMyOrganizationsQuery, Result<List<OrganizationDto>>>
{
    private readonly IOrganizationService _service;
    public GetMyOrganizationsHandler(IOrganizationService service) => _service = service;
    public async Task<Result<List<OrganizationDto>>> Handle(GetMyOrganizationsQuery req, CancellationToken ct)
    {
        var list = await _service.GetMyOrganizationsAsync(req.UserId, ct);
        return Result<List<OrganizationDto>>.Success(list);
    }
}
public class CreateOrganizationHandler : IRequestHandler<CreateOrganizationCommand, Result<OrganizationDto>>
{
    private readonly IOrganizationService _service;
    public CreateOrganizationHandler(IOrganizationService service) => _service = service;
    public async Task<Result<OrganizationDto>> Handle(CreateOrganizationCommand req, CancellationToken ct)
    {
        try { var dto = await _service.CreateOrganizationAsync(req.Name, req.Description, req.CallerId, ct); return Result<OrganizationDto>.Success(dto); }
        catch (Exception ex) { return Result<OrganizationDto>.Failure(ex.Message); }
    }
}
public class UpdateOrganizationHandler : IRequestHandler<UpdateOrganizationCommand, Result<OrganizationDto>>
{
    private readonly IOrganizationService _service;
    public UpdateOrganizationHandler(IOrganizationService service) => _service = service;
    public async Task<Result<OrganizationDto>> Handle(UpdateOrganizationCommand req, CancellationToken ct)
    {
        try { var dto = await _service.UpdateOrganizationAsync(req.OrganizationId, req.Name, req.Description, req.CallerId, ct); return Result<OrganizationDto>.Success(dto); }
        catch (Exception ex) { return Result<OrganizationDto>.Failure(ex.Message); }
    }
}
public class GetOrgMembersHandler : IRequestHandler<GetOrgMembersQuery, Result<List<OrgMemberDto>>>
{
    private readonly IOrganizationService _service;
    public GetOrgMembersHandler(IOrganizationService service) => _service = service;
    public async Task<Result<List<OrgMemberDto>>> Handle(GetOrgMembersQuery req, CancellationToken ct)
    {
        try { var list = await _service.GetOrgMembersAsync(req.OrganizationId, req.CallerId, ct); return Result<List<OrgMemberDto>>.Success(list); }
        catch (Exception ex) { return Result<List<OrgMemberDto>>.Failure(ex.Message); }
    }
}
public class CreateEmployeeHandler : IRequestHandler<CreateEmployeeCommand, Result<OrgMemberDto>>
{
    private readonly IOrganizationService _service;
    public CreateEmployeeHandler(IOrganizationService service) => _service = service;
    public async Task<Result<OrgMemberDto>> Handle(CreateEmployeeCommand req, CancellationToken ct)
    {
        try { var dto = await _service.CreateEmployeeAsync(req.OrganizationId, req.FullName, req.Email, req.Password, req.Role, req.WorkspaceIds, req.CallerId, ct); return Result<OrgMemberDto>.Success(dto); }
        catch (Exception ex) { return Result<OrgMemberDto>.Failure(ex.Message); }
    }
}
public class UpdateEmployeeHandler : IRequestHandler<UpdateEmployeeCommand, Result<OrgMemberDto>>
{
    private readonly IOrganizationService _service;
    public UpdateEmployeeHandler(IOrganizationService service) => _service = service;
    public async Task<Result<OrgMemberDto>> Handle(UpdateEmployeeCommand req, CancellationToken ct)
    {
        try { var dto = await _service.UpdateEmployeeAsync(req.OrganizationId, req.UserId, req.FullName, req.Email, req.Role, req.WorkspaceIds, req.CallerId, ct); return Result<OrgMemberDto>.Success(dto); }
        catch (Exception ex) { return Result<OrgMemberDto>.Failure(ex.Message); }
    }
}
public class DeleteEmployeeHandler : IRequestHandler<DeleteEmployeeCommand, Result>
{
    private readonly IOrganizationService _service;
    public DeleteEmployeeHandler(IOrganizationService service) => _service = service;
    public async Task<Result> Handle(DeleteEmployeeCommand req, CancellationToken ct)
    {
        try { await _service.DeleteEmployeeAsync(req.OrganizationId, req.UserId, req.CallerId, ct); return Result.Success(); }
        catch (Exception ex) { return Result.Failure(ex.Message); }
    }
}
