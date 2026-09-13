using SharedKernel;

namespace Identity.Service.Domain.Entities;

/// <summary>
/// SubscriptionPlans table [identity].SubscriptionPlans — MNC-grade lookup for billing.
/// Seeded Free/Pro/Business/Enterprise via IdentitySeeder. Organizations.SubscriptionPlanId FK.
/// Enum SubscriptionPlan (SharedKernel) is code single source, table holds mutable limits.
/// </summary>
public class SubscriptionPlanEntity : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int MaxUsers { get; private set; }
    public int MaxWorkspaces { get; private set; }
    public int MaxProjects { get; private set; }
    public int StorageGB { get; private set; }
    public int AiRequests { get; private set; }
    public int ApiLimit { get; private set; }
    public string FeaturesJson { get; private set; } = "[]";

    private SubscriptionPlanEntity() { }

    public SubscriptionPlanEntity(Guid id, string name, decimal price, int maxUsers, int maxWorkspaces, int maxProjects, int storageGB, int aiRequests, int apiLimit, string featuresJson)
    {
        Id = id;
        Name = name;
        Price = price;
        MaxUsers = maxUsers;
        MaxWorkspaces = maxWorkspaces;
        MaxProjects = maxProjects;
        StorageGB = storageGB;
        AiRequests = aiRequests;
        ApiLimit = apiLimit;
        FeaturesJson = featuresJson;
    }

    public void Update(decimal price, int maxUsers, int maxWorkspaces, int maxProjects, int storageGB, int aiRequests, int apiLimit, string featuresJson)
    {
        Price = price;
        MaxUsers = maxUsers;
        MaxWorkspaces = maxWorkspaces;
        MaxProjects = maxProjects;
        StorageGB = storageGB;
        AiRequests = aiRequests;
        ApiLimit = apiLimit;
        FeaturesJson = featuresJson;
        Touch();
    }
}
