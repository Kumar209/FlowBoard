using MediatR;
using SharedKernel;
using Identity.Service.Application.SuperAdmin.DTOs;
using Identity.Service.Application.SuperAdmin.Interfaces;

namespace Identity.Service.Application.SuperAdmin.Queries;

public record GetSuperAdminDashboardQuery(Guid CallerId, string[] CallerRoles) : IRequest<Result<SuperAdminDashboardDto>>;

public class GetSuperAdminDashboardHandler : IRequestHandler<GetSuperAdminDashboardQuery, Result<SuperAdminDashboardDto>>
{
    private readonly ISuperAdminService _service;
    public GetSuperAdminDashboardHandler(ISuperAdminService service) => _service = service;

    public async Task<Result<SuperAdminDashboardDto>> Handle(GetSuperAdminDashboardQuery request, CancellationToken ct)
    {
        if (!SharedKernel.Roles.IsSuperAdmin(request.CallerRoles))
            return Result<SuperAdminDashboardDto>.Failure("Forbidden - SuperAdmin only");
        var dto = await _service.GetDashboardAsync(ct);
        return Result<SuperAdminDashboardDto>.Success(dto);
    }
}
