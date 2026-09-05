using SharedKernel;

namespace Identity.Service.Domain.Entities;

/// <summary>
/// RefreshToken - HttpOnly cookie rotation store. UserId FK, TokenHash (SHA256 of 64-byte random), ExpiresAt 7d, RevokedAt, ReplacedByTokenHash. Methods IsExpired/IsRevoked/IsActive + Revoke(). Handled by RefreshTokenService RotateAsync (revoke old + new) + IsReuseDetectedAsync (revoke family on theft). Supports sliding 15m JWT + 7d refresh.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    private RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAt)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsExpired && !IsRevoked;

    public void Revoke(string? replacedByHash = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByHash;
    }
}
