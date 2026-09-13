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

public record GeneralSettingsDto(
    string PlatformName,
    string LogoUrl,
    string SupportEmail,
    string Language,
    string Timezone
);

public record SecuritySettingsDto(
    bool MfaEnabled,
    string SessionTimeout,
    string PasswordPolicy,
    int MaxLoginAttempts,
    int LockoutMinutes
);

public record AuthSettingsDto(
    bool EmailVerificationRequired,
    bool GoogleEnabled,
    bool MicrosoftEnabled,
    bool PasswordEnabled
);

public record EmailSettingsDto(
    string Provider,
    string SenderEmail,
    string SenderName,
    bool VerificationEnabled,
    bool ResetEnabled
);

public record AiProviderConfigDto(
    string Provider,
    bool Enabled,
    bool IsDefault,
    bool IsFallback,
    string[] Models,
    int TimeoutMs,
    int MaxRetries
);

public record AiSettingsDto(
    List<AiProviderConfigDto> Providers
);

public record StorageSettingsDto(
    string Provider,
    int MaxFileSizeMb,
    string[] AllowedTypes
);

public record NotificationSettingsDto(
    bool EmailEnabled,
    bool InAppEnabled,
    string SignalRHub,
    string RabbitMqStatus
);

public record RateLimitsSettingsDto(
    int AuthRequestsPerMinute,
    int ApiRequestsPerMinute,
    int AiRequestsPerMinute,
    int FileRequestsPerMinute,
    int NotificationRequestsPerMinute,
    string EnforcedVia
);

public record MaintenanceSettingsDto(
    bool MaintenanceMode,
    string? ScheduledAt,
    string? EndAt,
    string? Announcement
);

public record TenantDefaultsDto(
    string DefaultPlanId
);

public record PlatformSettingsResponse(
    GeneralSettingsDto General,
    SecuritySettingsDto Security,
    AuthSettingsDto Authentication,
    EmailSettingsDto Email,
    AiSettingsDto Ai,
    StorageSettingsDto Storage,
    NotificationSettingsDto Notifications,
    RateLimitsSettingsDto RateLimits,
    MaintenanceSettingsDto Maintenance,
    TenantDefaultsDto TenantDefaults
);
