namespace Identity.Service.Api.DTOs;

public record RegisterRequest(string Email, string Password, string FullName);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string? RefreshToken); // From body or HttpOnly cookie
public record UserResponse(Guid Id, string Email, string FullName, string? AvatarUrl);
