using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Identity.Service.Api.DTOs;
using Identity.Service.Application.Commands;
using Identity.Service.Application.Interfaces;
using Identity.Service.Application.Queries;

namespace Identity.Service.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/auth/register - Anon, creates User + default Org/Workspace + tokens, sets HttpOnly refresh cookie
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _mediator.Send(new RegisterCommand(request.Email, request.Password, request.FullName, request.CompanyName, request.CompanyDescription));
        if (result.IsFailure)
        {
            if (result.Error!.Contains("already exists", StringComparison.OrdinalIgnoreCase) || result.Error.Contains("already registered", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }

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

    // POST /api/auth/refresh - reads refresh from HttpOnly cookie or body, rotates, sets new cookie (handles duplicate Path cookies + Base64 +/== encoding)
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? body)
    {
        var rawToken = body?.RefreshToken;
        if (string.IsNullOrEmpty(rawToken))
        {
            // Try manual Cookie header first to handle duplicate Path=/ and old Path=/api/auth (take last)
            if (Request.Headers.TryGetValue("Cookie", out var cookieHeader))
            {
                var cookies = cookieHeader.ToString().Split(';', StringSplitOptions.TrimEntries);
                var matches = cookies.Where(c => c.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase)).Select(c => c.Substring("refreshToken=".Length).Trim().Trim('"')).Select(v => { try { return Uri.UnescapeDataString(v); } catch { return v; } }).ToList();
                if (matches.Any()) rawToken = matches.Last();
            }
            // Fallback to parsed cookies (already UrlDecoded by ASP.NET)
            rawToken ??= Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(rawToken))
            {
                rawToken = rawToken.Trim().Trim('"');
                try { rawToken = Uri.UnescapeDataString(rawToken); } catch { }
            }
        }
        if (string.IsNullOrEmpty(rawToken)) return Unauthorized(new { error = "Refresh token missing" });
        rawToken = rawToken.Trim();

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

        var result = await _mediator.Send(new GetCurrentUserQuery(guid));
        if (result.IsFailure) return result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(new
        {
            user = result.Value.User,
            workspaces = result.Value.Memberships.Select(m => new { workspaceId = m.WorkspaceId, role = m.Role })
        });
    }

    // POST /api/auth/logout - revoke refresh cookie (delete both Path=/ and old Path=/api/auth with all SameSite combos)
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var isHttps = Request.IsHttps || Request.Headers["X-Forwarded-Proto"] == "https";
        var secure = isHttps && Request.Host.Host != "localhost";
        // Delete new Path=/ Lax
        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax, Secure = secure, HttpOnly = true });
        // Delete old Path=/api/auth Strict (original) and Lax (after fix)
        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth", SameSite = SameSiteMode.Strict, Secure = false, HttpOnly = true });
        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth", SameSite = SameSiteMode.Lax, Secure = secure, HttpOnly = true });
        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth", SameSite = SameSiteMode.Strict, Secure = secure, HttpOnly = true });
        Response.Cookies.Append("refreshToken", "", new CookieOptions { Path = "/", Expires = DateTimeOffset.UnixEpoch, HttpOnly = true, SameSite = SameSiteMode.Lax, Secure = secure });
        Response.Cookies.Append("refreshToken", "", new CookieOptions { Path = "/api/auth", Expires = DateTimeOffset.UnixEpoch, HttpOnly = true, SameSite = SameSiteMode.Strict, Secure = false });
        Response.Cookies.Append("refreshToken", "", new CookieOptions { Path = "/api/auth", Expires = DateTimeOffset.UnixEpoch, HttpOnly = true, SameSite = SameSiteMode.Lax, Secure = secure });
        return Ok(new { message = "Logged out" });
    }

    private void SetRefreshCookie(string token, DateTime expiresAt)
    {
        var isHttps = Request.IsHttps || Request.Headers["X-Forwarded-Proto"] == "https";
        var secure = isHttps && Request.Host.Host != "localhost";
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Expires = expiresAt,
            Path = "/"
        };
        Response.Cookies.Append("refreshToken", token, options);
        // Clear stale cookie with old Path=/api/auth (was Strict, Secure false) to avoid duplicate Cookie header with revoked token
        try
        {
            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth", SameSite = SameSiteMode.Strict, Secure = false, HttpOnly = true });
            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth", SameSite = SameSiteMode.Lax, Secure = secure, HttpOnly = true });
            Response.Cookies.Append("refreshToken", "", new CookieOptions { Path = "/api/auth", Expires = DateTimeOffset.UnixEpoch, HttpOnly = true, SameSite = SameSiteMode.Strict, Secure = false });
            Response.Cookies.Append("refreshToken", "", new CookieOptions { Path = "/api/auth", Expires = DateTimeOffset.UnixEpoch, HttpOnly = true, SameSite = SameSiteMode.Lax, Secure = secure });
        } catch { }
    }
}
