using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;

namespace Identity.Service.Application.Services;

// Generates JWT access token (15m) with claims: sub, email, orgIds, workspaceIds, roles
// Enterprise: Implements IJwtProvider (Application interface) - DIP, mockable
public class JwtProvider : IJwtProvider
{
    private readonly IConfiguration _config;

    public JwtProvider(IConfiguration config) => _config = config;

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(User user, IEnumerable<(Guid WorkspaceId, string Role)> memberships)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        var issuer = _config["Jwt:Issuer"] ?? "FlowBoard.Identity";
        var audience = _config["Jwt:Audience"] ?? "FlowBoard.Gateway";
        var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var m) ? m : 15;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add workspace memberships as claims (for tenant isolation + RBAC)
        foreach (var (workspaceId, role) in memberships)
        {
            claims.Add(new Claim("workspace_id", workspaceId.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiresAt);
    }
}
