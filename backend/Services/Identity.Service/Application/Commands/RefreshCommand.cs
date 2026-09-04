using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Identity.Service.Application.DTOs;
using Identity.Service.Application.Interfaces;

namespace Identity.Service.Application.Commands;

public record RefreshCommand(string RefreshToken) : IRequest<Result<AuthResponse>>;

public class RefreshCommandHandler : IRequestHandler<RefreshCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtProvider _jwt;
    private readonly IRefreshTokenService _refreshService;

    public RefreshCommandHandler(IApplicationDbContext db, IJwtProvider jwt, IRefreshTokenService refreshService)
    {
        _db = db;
        _jwt = jwt;
        _refreshService = refreshService;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshCommand request, CancellationToken ct)
    {
        // Check reuse (theft) - if token already revoked, revoke entire family
        var isReuse = await _refreshService.IsReuseDetectedAsync(request.RefreshToken);
        if (isReuse)
        {
            // Try to find user from hash and revoke all tokens (security)
            var hash = _refreshService.HashToken(request.RefreshToken);
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
            if (token != null) await _refreshService.RevokeFamilyAsync(token.UserId);
            return Result<AuthResponse>.Failure("Refresh token reuse detected - all tokens revoked");
        }

        // Rotate
        var (newToken, rawNew) = await _refreshService.RotateAsync(request.RefreshToken);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == newToken.UserId, ct);
        if (user == null) return Result<AuthResponse>.Failure("User not found");

        var memberships = await _db.WorkspaceMembers
            .Where(x => x.UserId == user.Id)
            .Select(x => new ValueTuple<Guid, string>(x.WorkspaceId, x.Role.ToString()))
            .ToListAsync(ct);

        var (accessToken, accessExpires) = _jwt.GenerateAccessToken(user, memberships);
        var response = new AuthResponse(user.Id, user.Email, user.FullName, accessToken, rawNew, accessExpires, newToken.ExpiresAt);
        return Result<AuthResponse>.Success(response);
    }
}
