using SharedKernel;

namespace Identity.Service.Domain.Entities;

// Rich Domain Model (DDD) - Entity owns its business rules via methods (UpdateProfile, Deactivate) with private setters
// This encapsulates invariants (e.g., Email lowercased, Touch() updates UpdatedAt) and prevents anemic setters like user.FullName = "..." from anywhere
// Your previous work: Controllers -> Services (interfaces) -> Repositories (interfaces) with anemic entities (public setters, logic in Service) is also valid (Anemic model) - common in CRUD apps
// Microservices often use Rich model (like here) to keep business logic inside the entity/aggregate (e.g., Task.Move() validates, adds DomainEvent, touches timestamp) so services stay thin and logic isn't scattered - but both patterns work, Rich is preferred for DDD/microservices where invariants matter
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
