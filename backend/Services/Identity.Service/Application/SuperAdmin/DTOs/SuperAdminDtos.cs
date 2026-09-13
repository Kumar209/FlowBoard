namespace Identity.Service.Application.SuperAdmin.DTOs;

public record SuperAdminKpisDto(
    int TotalOrganizations,
    int TotalUsers,
    int ActiveUsers,
    int TotalWorkspaces,
    int TotalProjects,
    double StorageUsedGb
);

public record ServiceHealthDto(
    string Name,
    string Status,
    int LatencyMs,
    string Uptime
);

public record GrowthChartDto(
    string[] Labels,
    int[] Organizations,
    int[] Users,
    int[] Workspaces,
    int[] Projects,
    double[] Storage,
    double[] Revenue
);

public record SuperAdminDashboardDto(
    SuperAdminKpisDto Kpis,
    List<ServiceHealthDto> Health,
    GrowthChartDto Growth
);

public record SuperAdminOrgRowDto(
    Guid Id,
    string Name,
    string Slug,
    Guid OwnerId,
    string OwnerName,
    string OwnerEmail,
    Guid PlanId,
    string PlanName,
    bool IsActive,
    int Users,
    int Workspaces,
    int Projects,
    DateTime CreatedAt
);

public record SuperAdminOrgsResponse(
    List<SuperAdminOrgRowDto> Items,
    int Total
);

public record SuperAdminUserRowDto(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive,
    bool IsPendingSuspension,
    DateTime? PendingDeadline,
    string? PendingReason,
    int OrgCount,
    Guid OrganizationId,
    string OrganizationName,
    string OrgRole,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record SuperAdminUsersResponse(
    List<SuperAdminUserRowDto> Items,
    int Total
);

public record SuperAdminOrgMemberRowDto(
    Guid UserId,
    string FullName,
    string Email,
    string OrgRole,
    bool IsActive,
    DateTime JoinedAt
);

public record SuperAdminOrgMembersResponse(
    List<SuperAdminOrgMemberRowDto> Items,
    int Total
);

public record SubscriptionOverviewDto(
    decimal Mrr,
    decimal Arr,
    int ActiveSubscriptions,
    decimal ChurnRate,
    decimal AvgRevenuePerOrg
);

public record SubscriptionRowDto(
    Guid OrganizationId,
    string OrganizationName,
    string PlanName,
    string BillingCycle,
    string Status,
    decimal Amount,
    DateTime RenewalAt
);

public record PlanConfigDto(
    Guid Id,
    string Name,
    decimal Price,
    int MaxUsers,
    int MaxWorkspaces,
    int MaxProjects,
    int StorageGB,
    int AiRequests,
    int ApiLimit,
    string FeaturesJson
);

public record SuperAdminSubscriptionsResponse(
    SubscriptionOverviewDto Overview,
    List<SubscriptionRowDto> Subscriptions,
    List<PlanConfigDto> Plans,
    double[] RevenueHistory
);

public record ComplaintDto(
    Guid Id,
    Guid OrganizationId,
    string OrganizationName,
    Guid CreatedByUserId,
    string CreatedByName,
    string CreatedByEmail,
    string CreatedByRole,
    string Subject,
    string Message,
    string Status,
    DateTime CreatedAt
);

public record ComplaintReplyDto(
    Guid Id,
    Guid ComplaintId,
    Guid AuthorUserId,
    string AuthorName,
    string AuthorEmail,
    string AuthorRole,
    string Message,
    bool IsSuperAdminReply,
    DateTime CreatedAt
);

public record ComplaintDetailDto(
    ComplaintDto Complaint,
    List<ComplaintReplyDto> Replies
);

public record PlatformActivityDto(
    Guid Id,
    DateTime OccurredOn,
    Guid OrganizationId,
    string OrganizationName,
    Guid ActorUserId,
    string ActorName,
    string ActorEmail,
    string ActorRole,
    string Action,
    string Resource,
    string Result,
    string? PayloadJson
);

public record PlatformActivitiesResponse(
    List<PlatformActivityDto> Items,
    int Total,
    int Page,
    int PageSize
);

public record SystemHealthDto(
    string Service,
    string Status,
    int LatencyMs,
    string Uptime,
    string Version,
    string Details
);

public record SystemStatusDto(
    List<SystemHealthDto> Services,
    DateTime CheckedAt,
    string Environment,
    int TotalServices,
    int HealthyCount
);

public record FeatureFlagDto(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    bool IsEnabled,
    DateTime UpdatedAt
);

public record AiPlatformOverviewDto(
    int TotalRequests,
    int SuccessRequests,
    int FailedRequests,
    int TotalTokens,
    decimal EstimatedCost,
    double AvgLatencyMs,
    int FallbackCount
);

public record AiProviderUsageDto(
    string Provider,
    int Requests,
    int TotalTokens,
    int SuccessCount,
    int FailedCount,
    double AvgLatencyMs,
    decimal TotalCost
);

public record AiModelUsageDto(
    string Model,
    string Provider,
    int Requests,
    int InputTokens,
    int OutputTokens,
    int TotalTokens,
    decimal TotalCost
);

public record AiOrgUsageDto(
    Guid OrganizationId,
    string OrganizationName,
    int Requests,
    int TotalTokens,
    decimal TotalCost
);

public record AiOperationUsageDto(
    string Operation,
    int Requests,
    int TotalTokens,
    decimal TotalCost
);

public record AiFailureDto(
    string Reason,
    int Count
);

public record AiPlatformUsageResponse(
    AiPlatformOverviewDto Overview,
    List<AiProviderUsageDto> Providers,
    List<AiModelUsageDto> Models,
    List<AiOrgUsageDto> Organizations,
    List<AiOperationUsageDto> Operations,
    List<AiFailureDto> Failures
);
