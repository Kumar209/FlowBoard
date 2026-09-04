using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Identity.Service.Application.DTOs;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;

namespace Identity.Service.Application.Commands;

public record RegisterCommand(string Email, string Password, string FullName) : IRequest<Result<AuthResponse>>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtProvider _jwt;
    private readonly IRefreshTokenService _refreshService;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandler(IApplicationDbContext db, IJwtProvider jwt, IRefreshTokenService refreshService, IPasswordHasher passwordHasher)
    {
        _db = db;
        _jwt = jwt;
        _refreshService = refreshService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var exists = await _db.Users.AnyAsync(x => x.Email == request.Email.ToLowerInvariant(), ct);
        if (exists) return Result<AuthResponse>.Failure("Email already registered");

        var hash = _passwordHasher.Hash(request.Password);
        var user = new User(request.Email, hash, request.FullName);
        _db.Users.Add(user);

        // Create default Org + Workspace for new user (so they have a tenant immediately)
        var org = new Organization($"{request.FullName}'s Org", $"{user.Id.ToString()[..8]}-org", user.Id);
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync(ct); // need org.Id

        var workspace = new Workspace(org.Id, "Personal Workspace", "personal");
        _db.Workspaces.Add(workspace);
        await _db.SaveChangesAsync(ct);

        var member = new WorkspaceMember(workspace.Id, user.Id, Domain.Enums.WorkspaceRole.OrgAdmin);
        _db.WorkspaceMembers.Add(member);

        // Generate tokens
        var memberships = new[] { (workspace.Id, Domain.Enums.WorkspaceRole.OrgAdmin.ToString()) };
        var (accessToken, accessExpires) = _jwt.GenerateAccessToken(user, memberships);
        var (rawRefresh, hashRefresh, refreshExpires) = _refreshService.GenerateRawToken();
        var refreshToken = new RefreshToken(user.Id, hashRefresh, refreshExpires);
        _db.RefreshTokens.Add(refreshToken);

        await _db.SaveChangesAsync(ct);

        var response = new AuthResponse(user.Id, user.Email, user.FullName, accessToken, rawRefresh, accessExpires, refreshExpires);
        return Result<AuthResponse>.Success(response);
    }
}
