using SharedKernel;

namespace Identity.Service.Domain.Entities;

public class FeatureFlag : BaseEntity, IAggregateRoot
{
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsEnabled { get; private set; }
    public string? OrgOverridesJson { get; private set; }

    private FeatureFlag() { }

    public FeatureFlag(string key, string name, string? description, bool isEnabled)
    {
        Key = key.ToLowerInvariant();
        Name = name;
        Description = description;
        IsEnabled = isEnabled;
    }

    public void Toggle() => IsEnabled = !IsEnabled;
    public void SetEnabled(bool enabled) => IsEnabled = enabled;
    public void Update(string name, string? description) { Name = name; Description = description; Touch(); }
}
