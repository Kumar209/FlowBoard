using SharedKernel;

namespace Identity.Service.Domain.Entities;

public class Organization : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Guid OwnerId { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Organization() { }

    public Organization(string name, string slug, Guid ownerId)
    {
        Name = name;
        Slug = slug.ToLowerInvariant();
        OwnerId = ownerId;
    }

    public void Update(string name) { Name = name; Touch(); }
    public void Deactivate() => IsActive = false;
}
