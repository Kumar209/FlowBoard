using Microsoft.EntityFrameworkCore;
using Identity.Service.Application.DTOs;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;
using SharedKernel;

namespace Identity.Service.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtProvider _jwt;
    private readonly IRefreshTokenService _refreshService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IApplicationDbContext db, IJwtProvider jwt, IRefreshTokenService refreshService, IPasswordHasher passwordHasher)
    {
        _db = db;
        _jwt = jwt;
        _refreshService = refreshService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(string email, string password, string fullName, CancellationToken ct = default)
    {
        var exists = await _db.Users.AnyAsync(x => x.Email == email.ToLowerInvariant(), ct);
        if (exists) return Result<AuthResponse>.Failure("Email already registered");
        var hash = _passwordHasher.Hash(password);
        var user = new User(email, hash, fullName);
        _db.Users.Add(user);
        var org = new Organization($"{fullName}'s Org", $"{user.Id.ToString()[..8]}-org", user.Id);
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync(ct);
        var workspace = new Workspace(org.Id, "Personal Workspace", "personal");
        _db.Workspaces.Add(workspace);
        await _db.SaveChangesAsync(ct);
        var member = new WorkspaceMember(workspace.Id, user.Id, Domain.Enums.WorkspaceRole.OrgAdmin);
        _db.WorkspaceMembers.Add(member);
        var memberships = new[] { (workspace.Id, Domain.Enums.WorkspaceRole.OrgAdmin.ToString()) };
        var (accessToken, accessExpires) = _jwt.GenerateAccessToken(user, memberships);
        var (rawRefresh, hashRefresh, refreshExpires) = _refreshService.GenerateRawToken();
        var refreshToken = new RefreshToken(user.Id, hashRefresh, refreshExpires);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);
        return Result<AuthResponse>.Success(new AuthResponse(user.Id, user.Email, user.FullName, accessToken, rawRefresh, accessExpires, refreshExpires));
    }

    public async Task<Result<AuthResponse>> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email.ToLowerInvariant(), ct);
        if (user == null || !user.IsActive) return Result<AuthResponse>.Failure("Invalid credentials");
        if (!_passwordHasher.Verify(password, user.PasswordHash)) return Result<AuthResponse>.Failure("Invalid credentials");
        var memberships = await _db.WorkspaceMembers.Where(x => x.UserId == user.Id).Select(x => new ValueTuple<Guid, string>(x.WorkspaceId, x.Role.ToString())).ToListAsync(ct);
        var (accessToken, accessExpires) = _jwt.GenerateAccessToken(user, memberships);
        var (rawRefresh, hashRefresh, refreshExpires) = _refreshService.GenerateRawToken();
        var refreshToken = new RefreshToken(user.Id, hashRefresh, refreshExpires);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);
        return Result<AuthResponse>.Success(new AuthResponse(user.Id, user.Email, user.FullName, accessToken, rawRefresh, accessExpires, refreshExpires));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var isReuse = await _refreshService.IsReuseDetectedAsync(refreshToken);
        if (isReuse)
        {
            var hash = _refreshService.HashToken(refreshToken);
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
            if (token != null) await _refreshService.RevokeFamilyAsync(token.UserId);
            return Result<AuthResponse>.Failure("Refresh token reuse detected - all tokens revoked");
        }
        var (newToken, rawNew) = await _refreshService.RotateAsync(refreshToken);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == newToken.UserId, ct);
        if (user == null) return Result<AuthResponse>.Failure("User not found");
        var memberships = await _db.WorkspaceMembers.Where(x => x.UserId == user.Id).Select(x => new ValueTuple<Guid, string>(x.WorkspaceId, x.Role.ToString())).ToListAsync(ct);
        var (accessToken, accessExpires) = _jwt.GenerateAccessToken(user, memberships);
        return Result<AuthResponse>.Success(new AuthResponse(user.Id, user.Email, user.FullName, accessToken, rawNew, accessExpires, newToken.ExpiresAt));
    }

    public async Task<Result<(UserDto User, List<(Guid WorkspaceId, string Role)> Memberships)>> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user == null) return Result<(UserDto, List<(Guid, string)>)>.Failure("User not found");
        var memberships = await _db.WorkspaceMembers.Where(x => x.UserId == userId).Select(x => new ValueTuple<Guid, string>(x.WorkspaceId, x.Role.ToString())).ToListAsync(ct);
        var userResponse = new UserDto(user.Id, user.Email, user.FullName, user.AvatarUrl);
        return Result<(UserDto, List<(Guid, string)>)>.Success((userResponse, memberships));
    }
}
