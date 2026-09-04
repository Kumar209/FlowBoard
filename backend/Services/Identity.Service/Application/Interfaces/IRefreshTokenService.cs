using Identity.Service.Domain.Entities;

namespace Identity.Service.Application.Interfaces;

public interface IRefreshTokenService
{
    (string RawToken, string Hash, DateTime ExpiresAt) GenerateRawToken();
    Task<(RefreshToken NewToken, string RawNew)> RotateAsync(string oldRawToken);
    Task<bool> IsReuseDetectedAsync(string rawToken);
    Task RevokeFamilyAsync(Guid userId);
    string HashToken(string rawToken);
}
