using MediatR;
using SharedKernel;
using Identity.Service.Application.SuperAdmin.DTOs;
using Identity.Service.Application.SuperAdmin.Interfaces;

namespace Identity.Service.Application.SuperAdmin.Queries;

public record GetSuperAdminOrganizationsQuery(Guid CallerId, string[] CallerRoles, string? Search, int Page, int PageSize) : IRequest<Result<SuperAdminOrgsResponse>>;

public class GetSuperAdminOrganizationsHandler : IRequestHandler<GetSuperAdminOrganizationsQuery, Result<SuperAdminOrgsResponse>>
{
    private readonly ISuperAdminService _service;
    public GetSuperAdminOrganizationsHandler(ISuperAdminService service) => _service = service;

    public async Task<Result<SuperAdminOrgsResponse>> Handle(GetSuperAdminOrganizationsQuery request, CancellationToken ct)
    {
        if (!Roles.IsSuperAdmin(request.CallerRoles))
            return Result<SuperAdminOrgsResponse>.Failure("Forbidden - SuperAdmin only");
        if (request.Page < 1) return Result<SuperAdminOrgsResponse>.Failure("Page must be >= 1");
        if (request.PageSize < 1 || request.PageSize > 100) return Result<SuperAdminOrgsResponse>.Failure("PageSize must be 1-100");
        var dto = await _service.GetOrganizationsAsync(request.Search, request.Page, request.PageSize, ct);
        return Result<SuperAdminOrgsResponse>.Success(dto);
    }
}
