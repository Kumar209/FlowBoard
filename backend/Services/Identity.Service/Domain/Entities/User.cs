using SharedKernel;

namespace Identity.Service.Domain.Entities;

public class User : BaseEntity, IAggregateRoot
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    private User() { } // EF Core

    public User(string email, string passwordHash, string fullName, string? avatarUrl = null)
    {
        Email = email.ToLowerInvariant();
        PasswordHash = passwordHash;
        FullName = fullName;
        AvatarUrl = avatarUrl;
    }

    public void UpdateProfile(string fullName, string? avatarUrl)
    {
        FullName = fullName;
        AvatarUrl = avatarUrl;
        Touch();
    }

    public void Deactivate() => IsActive = false;
}
