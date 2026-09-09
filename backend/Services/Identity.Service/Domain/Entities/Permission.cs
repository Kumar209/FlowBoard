using SharedKernel;

namespace Identity.Service.Domain.Entities;

/// <summary>
/// Permission - fixed catalog (24 permissions) grouped by module. Seeded on migration.
/// </summary>
public class Permission : BaseEntity, IAggregateRoot
{
    public string Key { get; private set; } = string.Empty; // e.g., org:view, task:create
    public string Name { get; private set; } = string.Empty;
    public string Group { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Permission() { }

    public Permission(string key, string name, string group, string? description = null)
    {
        Key = key;
        Name = name;
        Group = group;
        Description = description;
    }
}
