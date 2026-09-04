using Identity.Service.Domain.Entities;

namespace Identity.Service.Application.Interfaces;

public interface IJwtProvider
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user, IEnumerable<(Guid WorkspaceId, string Role)> memberships);
}
