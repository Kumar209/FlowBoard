namespace Identity.Service.Application.DTOs;

public record AuthResponse(
    Guid UserId,
    string Email,
    string FullName,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt
);

public record UserDto(Guid Id, string Email, string FullName, string? AvatarUrl);
