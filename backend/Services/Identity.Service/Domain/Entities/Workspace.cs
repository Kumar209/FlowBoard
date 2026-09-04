using SharedKernel;

namespace Identity.Service.Domain.Entities;

public class Workspace : BaseEntity, IAggregateRoot
{
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    private Workspace() { }

    public Workspace(Guid organizationId, string name, string slug)
    {
        OrganizationId = organizationId;
        Name = name;
        Slug = slug.ToLowerInvariant();
    }

    public void Update(string name) { Name = name; Touch(); }
}
