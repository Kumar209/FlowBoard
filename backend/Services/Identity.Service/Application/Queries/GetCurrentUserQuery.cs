using MediatR;
using SharedKernel;
using Identity.Service.Application.DTOs;
using Identity.Service.Application.Interfaces;

namespace Identity.Service.Application.Queries;

public record GetCurrentUserQuery(Guid UserId) : IRequest<Result<(UserDto User, List<(Guid WorkspaceId, string Role)> Memberships)>>;

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, Result<(UserDto User, List<(Guid WorkspaceId, string Role)> Memberships)>>
{
    private readonly IAuthService _authService;
    public GetCurrentUserHandler(IAuthService authService) => _authService = authService;
    public async Task<Result<(UserDto User, List<(Guid WorkspaceId, string Role)> Memberships)>> Handle(GetCurrentUserQuery req, CancellationToken ct)
    {
        var result = await _authService.GetMeAsync(req.UserId, ct);
        if (result.IsFailure) return Result<(UserDto, List<(Guid, string)>)>.Failure(result.Error!);
        return Result<(UserDto, List<(Guid, string)>)>.Success(result.Value);
    }
}
