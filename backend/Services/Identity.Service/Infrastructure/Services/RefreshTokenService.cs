using System.Security.Cryptography;
using System.Text;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;
using SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Identity.Service.Infrastructure.Services;

// Handles refresh token generation (64-byte random, SHA256 hashed), rotation, and revocation
// Implements IRefreshTokenService (Application interface) - DIP, depends on IApplicationDbContext not concrete DbContext
public class RefreshTokenService : IRefreshTokenService
{
    private readonly IApplicationDbContext _db;
    private readonly IConfiguration _config;

    public RefreshTokenService(IApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public (string RawToken, string Hash, DateTime ExpiresAt) GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var raw = Convert.ToBase64String(bytes);
        var hash = HashToken(raw);
        var days = int.TryParse(_config["Jwt:RefreshDays"], out var d) ? d : 7;
        var expiresAt = DateTime.UtcNow.AddDays(days);
        return (raw, hash, expiresAt);
    }

    public string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }

    // Static helper for handlers that need hash without instance (kept for convenience, calls instance logic)
    public static string HashTokenStatic(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }

    // Rotation: revoke old token, create new one with same user — handles concurrent race (grace 60s)
    public async Task<(RefreshToken NewToken, string RawNew)> RotateAsync(string oldRawToken)
    {
        var oldHash = HashToken(oldRawToken);
        var oldToken = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == oldHash && x.RevokedAt == null);
        if (oldToken == null)
        {
            // Concurrent race: old already revoked within grace window — check if it was just rotated
            var revoked = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == oldHash && x.RevokedAt != null);
            if (revoked != null && revoked.RevokedAt.HasValue && revoked.RevokedAt.Value > DateTime.UtcNow.AddSeconds(-60) && !string.IsNullOrEmpty(revoked.ReplacedByTokenHash))
            {
                // Find the replacement token that was just created (by first concurrent call)
                var replacement = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == revoked.ReplacedByTokenHash && x.UserId == revoked.UserId);
                if (replacement != null && !replacement.IsExpired && replacement.IsActive)
                {
                    // Return a fresh token instead of failing — generate another rotation from replacement to keep singleflight idempotent
                    var (newRaw2, newHash2, newExpires2) = GenerateRawToken();
                    var newToken2 = new RefreshToken(replacement.UserId, newHash2, newExpires2);
                    replacement.Revoke(newHash2);
                    _db.RefreshTokens.Add(newToken2);
                    await _db.SaveChangesAsync();
                    return (newToken2, newRaw2);
                }
            }
            throw new UnauthorizedAccessException("Invalid refresh token");
        }
        if (oldToken.IsExpired) throw new UnauthorizedAccessException("Invalid refresh token");

        var (newRaw, newHash, newExpires) = GenerateRawToken();
        var newToken = new RefreshToken(oldToken.UserId, newHash, newExpires);

        oldToken.Revoke(newHash);
        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync();

        return (newToken, newRaw);
    }

    // Detect reuse (theft): if old token already revoked, revoke entire family — grace 60s for concurrent
    public async Task<bool> IsReuseDetectedAsync(string rawToken)
    {
        var hash = HashToken(rawToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
        if (token == null || !token.IsRevoked) return false;
        // Grace: if revoked within 60s and has replacement, it's a concurrent race, not theft
        if (token.RevokedAt.HasValue && token.RevokedAt.Value > DateTime.UtcNow.AddSeconds(-60) && !string.IsNullOrEmpty(token.ReplacedByTokenHash))
            return false;
        return true;
    }

    public async Task RevokeFamilyAsync(Guid userId)
    {
        var tokens = await _db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync();
        foreach (var t in tokens) t.Revoke();
        await _db.SaveChangesAsync();
    }
}

