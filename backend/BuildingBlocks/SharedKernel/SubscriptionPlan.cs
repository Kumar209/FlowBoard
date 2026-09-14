namespace SharedKernel;

/// <summary>
/// Single source for plan names. Values align with [identity].SubscriptionPlans seed (Free=0..Enterprise=3).
/// </summary>
public enum SubscriptionPlan
{
    Free = 0,
    Pro = 1,
    Business = 2,
    Enterprise = 3
}
