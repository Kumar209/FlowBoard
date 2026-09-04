using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Identity.Service.Api.DTOs;
using Identity.Service.Application.Commands;
using Identity.Service.Application.Interfaces;

namespace Identity.Service.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _db;

    public AuthController(IMediator mediator, IApplicationDbContext db)
    {
        _mediator = mediator;
        _db = db;
    }

    // POST /api/auth/register - Anon, creates User + default Org/Workspace + tokens, sets HttpOnly refresh cookie
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _mediator.Send(new RegisterCommand(request.Email, request.Password, request.FullName));
        if (result.IsFailure) return BadRequest(new { error = result.Error });

        SetRefreshCookie(result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return Ok(new
        {
            user = new { id = result.Value.UserId, email = result.Value.Email, fullName = result.Value.FullName },
            accessToken = result.Value.AccessToken,
            accessTokenExpiresAt = result.Value.AccessTokenExpiresAt
        });
    }

    // POST /api/auth/login - Anon, verifies BCrypt, returns tokens + sets cookie
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password));
        if (result.IsFailure) return Unauthorized(new { error = result.Error });

        SetRefreshCookie(result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return Ok(new
        {
            user = new { id = result.Value.UserId, email = result.Value.Email, fullName = result.Value.FullName },
            accessToken = result.Value.AccessToken,
            accessTokenExpiresAt = result.Value.AccessTokenExpiresAt
        });
    }

    // POST /api/auth/refresh - reads refresh from HttpOnly cookie or body, rotates, sets new cookie
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? body)
    {
        var rawToken = Request.Cookies["refreshToken"] ?? body?.RefreshToken;
        if (string.IsNullOrEmpty(rawToken)) return Unauthorized(new { error = "Refresh token missing" });

        var result = await _mediator.Send(new RefreshCommand(rawToken));
        if (result.IsFailure) return Unauthorized(new { error = result.Error });

        SetRefreshCookie(result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return Ok(new
        {
            accessToken = result.Value.AccessToken,
            accessTokenExpiresAt = result.Value.AccessTokenExpiresAt
        });
    }

    // GET /api/auth/me - requires JWT, returns current user + memberships
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userId == null || !Guid.TryParse(userId, out var guid)) return Unauthorized(new { error = "Invalid token" });

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == guid);
        if (user == null) return NotFound(new { error = "User not found" });

        var memberships = await _db.WorkspaceMembers.Where(x => x.UserId == guid).ToListAsync();
        return Ok(new
        {
            user = new UserResponse(user.Id, user.Email, user.FullName, user.AvatarUrl),
            workspaces = memberships.Select(m => new { workspaceId = m.WorkspaceId, role = m.Role.ToString() })
        });
    }

    // POST /api/auth/logout - revoke refresh cookie
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "Logged out" });
    }

    private void SetRefreshCookie(string token, DateTime expiresAt)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Secure in production (HTTPS), will be false for localhost http in dev - set via config
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt,
            Path = "/api/auth"
        };
        // For localhost http dev, Secure should be false - adjust via env
        if (Request.Host.Host == "localhost") options.Secure = false;
        Response.Cookies.Append("refreshToken", token, options);
    }
}
