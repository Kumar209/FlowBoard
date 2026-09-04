using System.Security.Cryptography;
using System.Text;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Service.Application.Services;

// Handles refresh token generation (64-byte random, SHA256 hashed), rotation, and revocation
// Enterprise: Implements IRefreshTokenService (Application interface) - DIP, depends on IApplicationDbContext not concrete DbContext
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

    // Rotation: revoke old token, create new one with same user
    public async Task<(RefreshToken NewToken, string RawNew)> RotateAsync(string oldRawToken)
    {
        var oldHash = HashToken(oldRawToken);
        var oldToken = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == oldHash && x.RevokedAt == null);
        if (oldToken == null || oldToken.IsExpired) throw new UnauthorizedAccessException("Invalid refresh token");

        var (newRaw, newHash, newExpires) = GenerateRawToken();
        var newToken = new RefreshToken(oldToken.UserId, newHash, newExpires);

        oldToken.Revoke(newHash);
        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync();

        return (newToken, newRaw);
    }

    // Detect reuse (theft): if old token already revoked, revoke entire family
    public async Task<bool> IsReuseDetectedAsync(string rawToken)
    {
        var hash = HashToken(rawToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
        return token != null && token.IsRevoked;
    }

    public async Task RevokeFamilyAsync(Guid userId)
    {
        var tokens = await _db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync();
        foreach (var t in tokens) t.Revoke();
        await _db.SaveChangesAsync();
    }
}
