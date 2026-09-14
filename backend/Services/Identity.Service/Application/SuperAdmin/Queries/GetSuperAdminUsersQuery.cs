using MediatR;
using SharedKernel;
using Identity.Service.Application.SuperAdmin.DTOs;
using Identity.Service.Application.SuperAdmin.Interfaces;

namespace Identity.Service.Application.SuperAdmin.Queries;

public record GetSuperAdminUsersQuery(Guid CallerId, string[] CallerRoles, string? Search, int Page, int PageSize) : IRequest<Result<SuperAdminUsersResponse>>;

public class GetSuperAdminUsersHandler : IRequestHandler<GetSuperAdminUsersQuery, Result<SuperAdminUsersResponse>>
{
    private readonly ISuperAdminService _service;
    public GetSuperAdminUsersHandler(ISuperAdminService service) => _service = service;

    public async Task<Result<SuperAdminUsersResponse>> Handle(GetSuperAdminUsersQuery request, CancellationToken ct)
    {
        if (!Roles.IsSuperAdmin(request.CallerRoles))
            return Result<SuperAdminUsersResponse>.Failure("Forbidden - SuperAdmin only");
        if (request.Page < 1) return Result<SuperAdminUsersResponse>.Failure("Page must be >= 1");
        if (request.PageSize < 1 || request.PageSize > 100) return Result<SuperAdminUsersResponse>.Failure("PageSize must be 1-100");
        var dto = await _service.GetUsersAsync(request.Search, request.Page, request.PageSize, ct);
        return Result<SuperAdminUsersResponse>.Success(dto);
    }
}
