using SharedKernel;

namespace Identity.Service.Domain.Entities;

public class PlatformSetting : IAggregateRoot
{
    public string Key { get; private set; } = string.Empty;
    public string ValueJson { get; private set; } = string.Empty;
    public Guid? UpdatedBy { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PlatformSetting() { }

    public PlatformSetting(string key, string valueJson, Guid? updatedBy = null)
    {
        Key = key.ToLowerInvariant();
        ValueJson = valueJson;
        UpdatedBy = updatedBy;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string valueJson, Guid? updatedBy)
    {
        ValueJson = valueJson;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
