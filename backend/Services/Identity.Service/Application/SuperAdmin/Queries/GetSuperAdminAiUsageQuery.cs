using MediatR;
using SharedKernel;
using Identity.Service.Application.SuperAdmin.DTOs;
using Identity.Service.Application.SuperAdmin.Interfaces;

namespace Identity.Service.Application.SuperAdmin.Queries;

public record GetSuperAdminAiUsageQuery(Guid CallerId, string[] CallerRoles) : IRequest<Result<AiPlatformUsageResponse>>;

public class GetSuperAdminAiUsageHandler : IRequestHandler<GetSuperAdminAiUsageQuery, Result<AiPlatformUsageResponse>>
{
    private readonly ISuperAdminService _service;
    public GetSuperAdminAiUsageHandler(ISuperAdminService service) => _service = service;

    public async Task<Result<AiPlatformUsageResponse>> Handle(GetSuperAdminAiUsageQuery request, CancellationToken ct)
    {
        if (!Roles.IsSuperAdmin(request.CallerRoles))
            return Result<AiPlatformUsageResponse>.Failure("Forbidden - SuperAdmin only");
        var dto = await _service.GetAiPlatformUsageAsync(ct);
        return Result<AiPlatformUsageResponse>.Success(dto);
    }
}
