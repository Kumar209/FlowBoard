using MediatR;
using SharedKernel;
using Identity.Service.Application.SuperAdmin.DTOs;
using Identity.Service.Application.SuperAdmin.Interfaces;

namespace Identity.Service.Application.SuperAdmin.Queries;

public record GetSuperAdminSettingsQuery(Guid CallerId, string[] CallerRoles) : IRequest<Result<PlatformSettingsResponse>>;

public class GetSuperAdminSettingsHandler : IRequestHandler<GetSuperAdminSettingsQuery, Result<PlatformSettingsResponse>>
{
    private readonly ISuperAdminService _service;
    public GetSuperAdminSettingsHandler(ISuperAdminService service) => _service = service;

    public async Task<Result<PlatformSettingsResponse>> Handle(GetSuperAdminSettingsQuery request, CancellationToken ct)
    {
        if (!Roles.IsSuperAdmin(request.CallerRoles))
            return Result<PlatformSettingsResponse>.Failure("Forbidden - SuperAdmin only");
        var dto = await _service.GetSettingsAsync(ct);
        return Result<PlatformSettingsResponse>.Success(dto);
    }
}
