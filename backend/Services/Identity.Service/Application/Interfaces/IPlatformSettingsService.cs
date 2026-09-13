using Identity.Service.Application.SuperAdmin.DTOs;

namespace Identity.Service.Application.Interfaces;

public interface IPlatformSettingsService
{
    Task<GeneralSettingsDto> GetGeneralAsync(CancellationToken ct = default);
    Task SetGeneralAsync(GeneralSettingsDto dto, Guid updatedBy, CancellationToken ct = default);
    Task<SecuritySettingsDto> GetSecurityAsync(CancellationToken ct = default);
    Task SetSecurityAsync(SecuritySettingsDto dto, Guid updatedBy, CancellationToken ct = default);
    Task<string> GetTenantDefaultPlanIdAsync(CancellationToken ct = default);
    Task SetTenantDefaultsAsync(string planId, Guid updatedBy, CancellationToken ct = default);
    Task<AiSettingsDto> GetAiAsync(CancellationToken ct = default);
    Task SetAiAsync(AiSettingsDto dto, Guid updatedBy, CancellationToken ct = default);
    Task<RateLimitsSettingsDto> GetRateLimitsAsync(CancellationToken ct = default);
    Task SetRateLimitsAsync(RateLimitsSettingsDto dto, Guid updatedBy, CancellationToken ct = default);
    Task<MaintenanceSettingsDto> GetMaintenanceAsync(CancellationToken ct = default);
    Task SetMaintenanceAsync(MaintenanceSettingsDto dto, Guid updatedBy, CancellationToken ct = default);
    Task<PlatformSettingsResponse> GetAllAsync(CancellationToken ct = default);
    Task<bool> IsMaintenanceActiveAsync(CancellationToken ct = default);
    Task<string> UploadLogoAsync(string fileName, Stream stream, string contentType, CancellationToken ct = default);
}
