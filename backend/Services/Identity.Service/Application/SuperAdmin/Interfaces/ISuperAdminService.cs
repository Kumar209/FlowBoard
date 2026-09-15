using Identity.Service.Application.SuperAdmin.DTOs;
using Identity.Service.Domain.Entities;

namespace Identity.Service.Application.SuperAdmin.Interfaces;

public interface ISuperAdminService
{
    Task<SuperAdminDashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<SuperAdminOrgsResponse> GetOrganizationsAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<SuperAdminUsersResponse> GetUsersAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<SuperAdminSubscriptionsResponse> GetSubscriptionsAsync(CancellationToken ct = default);
    Task<SuperAdminOrgMembersResponse> GetOrgMembersAsync(Guid orgId, string? search, int page, int pageSize, CancellationToken ct = default);
    Task SuspendOrganizationAsync(Guid orgId, string reason, string? message, Guid actorId, CancellationToken ct = default);
    Task ActivateOrganizationAsync(Guid orgId, Guid actorId, CancellationToken ct = default);
    Task DeleteOrganizationAsync(Guid orgId, Guid actorId, CancellationToken ct = default);
    Task SuspendUserWithGraceAsync(Guid userId, Guid organizationId, string reason, string message, DateTime deadlineAt, Guid actorId, CancellationToken ct = default);
    Task ReactivateUserAsync(Guid userId, Guid actorId, CancellationToken ct = default);
    Task DeleteUserAsync(Guid userId, Guid actorId, CancellationToken ct = default);
    Task<List<PlatformNotice>> GetActiveNoticesAsync(Guid organizationId, CancellationToken ct = default);
    Task<ComplaintsResponse> GetComplaintsAsync(Guid? organizationId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<ComplaintDto> CreateComplaintAsync(Guid organizationId, Guid userId, string subject, string message, CancellationToken ct = default);
    Task<ComplaintReplyDto> ReplyToComplaintAsync(Guid complaintId, Guid authorId, string message, bool isSuperAdmin, CancellationToken ct = default);
    Task<ComplaintDetailDto> GetComplaintDetailAsync(Guid complaintId, CancellationToken ct = default);
    Task DeleteComplaintAsync(Guid complaintId, Guid actorId, CancellationToken ct = default);
    Task EnforcePendingSuspensionsAsync(CancellationToken ct = default);
    Task<PlatformActivitiesResponse> GetPlatformActivitiesAsync(string? search, string? action, int page, int pageSize, CancellationToken ct = default);
    Task<SystemStatusDto> GetSystemStatusAsync(CancellationToken ct = default);
    Task<List<FeatureFlagDto>> GetFeatureFlagsAsync(CancellationToken ct = default);
    Task<FeatureFlagDto> ToggleFeatureFlagAsync(string key, CancellationToken ct = default);
    Task<List<FeatureFlagDto>> GetOrganizationFeatureFlagsAsync(Guid organizationId, CancellationToken ct = default);
    Task<FeatureFlagDto> ToggleOrganizationFeatureFlagAsync(Guid organizationId, string key, CancellationToken ct = default);
    Task<AiPlatformUsageResponse> GetAiPlatformUsageAsync(CancellationToken ct = default);
    Task<PlatformSettingsResponse> GetSettingsAsync(CancellationToken ct = default);
    Task<PlanConfigDto> UpdatePlanAsync(Guid planId, PlanConfigDto dto, Guid actorId, CancellationToken ct = default);
    Task AssignPlanAsync(Guid organizationId, Guid planId, Guid actorId, CancellationToken ct = default);
}
