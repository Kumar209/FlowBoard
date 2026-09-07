using Identity.Service.Application.DTOs;
using SharedKernel;

namespace Identity.Service.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(string email, string password, string fullName, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<(UserDto User, List<(Guid WorkspaceId, string Role)> Memberships)>> GetMeAsync(Guid userId, CancellationToken ct = default);
}
