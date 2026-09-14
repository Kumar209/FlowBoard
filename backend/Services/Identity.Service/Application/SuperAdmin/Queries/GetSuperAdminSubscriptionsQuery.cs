using MediatR;
using SharedKernel;
using Identity.Service.Application.SuperAdmin.DTOs;
using Identity.Service.Application.SuperAdmin.Interfaces;

namespace Identity.Service.Application.SuperAdmin.Queries;

public record GetSuperAdminSubscriptionsQuery(Guid CallerId, string[] CallerRoles) : IRequest<Result<SuperAdminSubscriptionsResponse>>;

public class GetSuperAdminSubscriptionsHandler : IRequestHandler<GetSuperAdminSubscriptionsQuery, Result<SuperAdminSubscriptionsResponse>>
{
    private readonly ISuperAdminService _service;
    public GetSuperAdminSubscriptionsHandler(ISuperAdminService service) => _service = service;

    public async Task<Result<SuperAdminSubscriptionsResponse>> Handle(GetSuperAdminSubscriptionsQuery request, CancellationToken ct)
    {
        if (!Roles.IsSuperAdmin(request.CallerRoles))
            return Result<SuperAdminSubscriptionsResponse>.Failure("Forbidden - SuperAdmin only");
        var dto = await _service.GetSubscriptionsAsync(ct);
        return Result<SuperAdminSubscriptionsResponse>.Success(dto);
    }
}
