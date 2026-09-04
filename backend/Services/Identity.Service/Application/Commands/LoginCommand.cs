using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Identity.Service.Application.DTOs;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;

namespace Identity.Service.Application.Commands;

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtProvider _jwt;
    private readonly IRefreshTokenService _refreshService;
    private readonly IPasswordHasher _passwordHasher;

    public LoginCommandHandler(IApplicationDbContext db, IJwtProvider jwt, IRefreshTokenService refreshService, IPasswordHasher passwordHasher)
    {
        _db = db;
        _jwt = jwt;
        _refreshService = refreshService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email.ToLowerInvariant(), ct);
        if (user == null || !user.IsActive) return Result<AuthResponse>.Failure("Invalid credentials");
        if (!_passwordHasher.Verify(request.Password, user.PasswordHash)) return Result<AuthResponse>.Failure("Invalid credentials");

        var memberships = await _db.WorkspaceMembers
            .Where(x => x.UserId == user.Id)
            .Select(x => new ValueTuple<Guid, string>(x.WorkspaceId, x.Role.ToString()))
            .ToListAsync(ct);

        var (accessToken, accessExpires) = _jwt.GenerateAccessToken(user, memberships);
        var (rawRefresh, hashRefresh, refreshExpires) = _refreshService.GenerateRawToken();
        var refreshToken = new RefreshToken(user.Id, hashRefresh, refreshExpires);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);

        var response = new AuthResponse(user.Id, user.Email, user.FullName, accessToken, rawRefresh, accessExpires, refreshExpires);
        return Result<AuthResponse>.Success(response);
    }
}
