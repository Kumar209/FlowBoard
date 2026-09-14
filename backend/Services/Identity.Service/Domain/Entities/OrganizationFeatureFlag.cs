using SharedKernel;

namespace Identity.Service.Domain.Entities;

public class OrganizationFeatureFlag : BaseEntity, IAggregateRoot
{
    public Guid OrganizationId { get; private set; }
    public string FlagKey { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }

    private OrganizationFeatureFlag() { }

    public OrganizationFeatureFlag(Guid organizationId, string flagKey, bool isEnabled)
    {
        OrganizationId = organizationId;
        FlagKey = flagKey.ToLowerInvariant();
        IsEnabled = isEnabled;
    }

    public void SetEnabled(bool enabled) { IsEnabled = enabled; Touch(); }
}
