using FluentValidation;
using MediatR;
using SharedKernel;
using Identity.Service.Application.SuperAdmin.DTOs;
using Identity.Service.Application.SuperAdmin.Interfaces;

namespace Identity.Service.Application.SuperAdmin.Commands;

public record UpdatePlanCommand(Guid CallerId, string[] CallerRoles, Guid PlanId, PlanConfigDto Dto) : IRequest<Result<PlanConfigDto>>;
public record AssignPlanCommand(Guid CallerId, string[] CallerRoles, Guid OrganizationId, Guid PlanId) : IRequest<Result<string>>;

public class UpdatePlanValidator : AbstractValidator<UpdatePlanCommand>
{
    public UpdatePlanValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.Dto.Price).InclusiveBetween(0, 10000);
        RuleFor(x => x.Dto.MaxUsers).InclusiveBetween(1, 10000);
        RuleFor(x => x.Dto.MaxWorkspaces).InclusiveBetween(1, 1000);
        RuleFor(x => x.Dto.MaxProjects).InclusiveBetween(1, 10000);
        RuleFor(x => x.Dto.StorageGB).InclusiveBetween(1, 10000);
        RuleFor(x => x.Dto.AiRequests).InclusiveBetween(0, 1000000);
        RuleFor(x => x.Dto.ApiLimit).InclusiveBetween(100, 10000000);
        RuleFor(x => x.Dto.FeaturesJson).MaximumLength(2000);
    }
}
public class AssignPlanValidator : AbstractValidator<AssignPlanCommand>
{
    public AssignPlanValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.PlanId).NotEmpty();
    }
}

public class UpdatePlanHandler : IRequestHandler<UpdatePlanCommand, Result<PlanConfigDto>>
{
    private readonly ISuperAdminService _svc;
    public UpdatePlanHandler(ISuperAdminService svc) => _svc = svc;
    public async Task<Result<PlanConfigDto>> Handle(UpdatePlanCommand r, CancellationToken ct)
    {
        if (!Roles.IsSuperAdmin(r.CallerRoles)) return Result<PlanConfigDto>.Failure("Forbidden - SuperAdmin only");
        try { var res = await _svc.UpdatePlanAsync(r.PlanId, r.Dto, r.CallerId, ct); return Result<PlanConfigDto>.Success(res); }
        catch (Exception ex) { return Result<PlanConfigDto>.Failure(ex.Message); }
    }
}
public class AssignPlanHandler : IRequestHandler<AssignPlanCommand, Result<string>>
{
    private readonly ISuperAdminService _svc;
    public AssignPlanHandler(ISuperAdminService svc) => _svc = svc;
    public async Task<Result<string>> Handle(AssignPlanCommand r, CancellationToken ct)
    {
        if (!Roles.IsSuperAdmin(r.CallerRoles)) return Result<string>.Failure("Forbidden - SuperAdmin only");
        try { await _svc.AssignPlanAsync(r.OrganizationId, r.PlanId, r.CallerId, ct); return Result<string>.Success("Assigned"); }
        catch (Exception ex) { return Result<string>.Failure(ex.Message); }
    }
}
