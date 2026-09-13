using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Identity.Service.Application.Interfaces;
using Identity.Service.Application.SuperAdmin.DTOs;
using Identity.Service.Domain.Entities;

namespace Identity.Service.Infrastructure.Services;

public class PlatformSettingsService : IPlatformSettingsService
{
    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<PlatformSettingsService> _logger;
    private readonly Cloudinary? _cloudinary;

    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };
    private const string CachePrefix = "platform:";

    public PlatformSettingsService(IApplicationDbContext db, IMemoryCache cache, IConfiguration config, ILogger<PlatformSettingsService> logger)
    {
        _db = db;
        _cache = cache;
        _config = config;
        _logger = logger;
        var cloudName = config["Cloudinary:CloudName"] ?? config["Cloudinary__CloudName"];
        var apiKey = config["Cloudinary:ApiKey"] ?? config["Cloudinary__ApiKey"];
        var apiSecret = config["Cloudinary:ApiSecret"] ?? config["Cloudinary__ApiSecret"];
        if (!string.IsNullOrWhiteSpace(cloudName) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiSecret))
        {
            _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        }
    }

    private async Task<T> GetOrSeedAsync<T>(string key, T defaultValue, CancellationToken ct)
    {
        var cacheKey = CachePrefix + key;
        if (_cache.TryGetValue(cacheKey, out T? cached) && cached != null) return cached;
        var row = await _db.PlatformSettings.FirstOrDefaultAsync(x => x.Key == key, ct);
        if (row == null)
        {
            var json = JsonSerializer.Serialize(defaultValue, _jsonOpts);
            row = new PlatformSetting(key, json, null);
            _db.PlatformSettings.Add(row);
            await _db.SaveChangesAsync(ct);
            _cache.Set(cacheKey, defaultValue, TimeSpan.FromSeconds(30));
            return defaultValue;
        }
        try
        {
            var dto = JsonSerializer.Deserialize<T>(row.ValueJson, _jsonOpts);
            if (dto == null) return defaultValue;
            _cache.Set(cacheKey, dto, TimeSpan.FromSeconds(30));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PlatformSettings deserialize failed for {Key}", key);
            return defaultValue;
        }
    }

    private async Task SetAsync<T>(string key, T dto, Guid updatedBy, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(dto, _jsonOpts);
        var row = await _db.PlatformSettings.FirstOrDefaultAsync(x => x.Key == key, ct);
        if (row == null)
        {
            row = new PlatformSetting(key, json, updatedBy);
            _db.PlatformSettings.Add(row);
        }
        else row.Update(json, updatedBy);
        await _db.SaveChangesAsync(ct);
        _cache.Remove(CachePrefix + key);
        // also audit as OrganizationActivity with synthetic org
        try
        {
            var firstOrg = await _db.Organizations.FirstOrDefaultAsync(ct);
            if (firstOrg != null)
            {
                _db.OrganizationActivities.Add(new OrganizationActivity(firstOrg.Id, updatedBy, "PlatformSettingsUpdated", $"{{\"key\":\"{key}\"}}"));
                await _db.SaveChangesAsync(ct);
            }
        }
        catch { }
    }

    public Task<GeneralSettingsDto> GetGeneralAsync(CancellationToken ct = default) =>
        GetOrSeedAsync("platform", new GeneralSettingsDto("FlowBoard", "/assets/logo.svg", "support@flowboard.local", "en", "UTC"), ct);

    public Task SetGeneralAsync(GeneralSettingsDto dto, Guid updatedBy, CancellationToken ct = default) =>
        SetAsync("platform", dto, updatedBy, ct);

    public Task<SecuritySettingsDto> GetSecurityAsync(CancellationToken ct = default) =>
        GetOrSeedAsync("security", new SecuritySettingsDto(false, "30m", "Min 8 chars, uppercase + lowercase + number + symbol", 5, 15), ct);

    public Task SetSecurityAsync(SecuritySettingsDto dto, Guid updatedBy, CancellationToken ct = default) =>
        SetAsync("security", dto, updatedBy, ct);

    public async Task<string> GetTenantDefaultPlanIdAsync(CancellationToken ct = default)
    {
        var dto = await GetOrSeedAsync("tenantDefaults", new TenantDefaultsDto("a0000000-0000-0000-0000-000000000010"), ct);
        return dto.DefaultPlanId;
    }

    public async Task SetTenantDefaultsAsync(string planId, Guid updatedBy, CancellationToken ct = default)
    {
        if (!Guid.TryParse(planId, out var gid) || await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == gid, ct) == null)
            throw new InvalidOperationException("Plan not found");
        await SetAsync("tenantDefaults", new TenantDefaultsDto(planId), updatedBy, ct);
    }

    public Task<AiSettingsDto> GetAiAsync(CancellationToken ct = default) =>
        GetOrSeedAsync("ai", new AiSettingsDto(new List<AiProviderConfigDto>
        {
            new("Gemini", true, true, false, new[] { "gemini-3.5-flash", "gemini-1.5-pro" }, 30000, 3),
            new("Groq", true, false, true, new[] { "llama-3.1-8b-instant", "llama-3.3-70b-versatile" }, 15000, 2)
        }), ct);

    public Task SetAiAsync(AiSettingsDto dto, Guid updatedBy, CancellationToken ct = default) =>
        SetAsync("ai", dto, updatedBy, ct);

    public Task<RateLimitsSettingsDto> GetRateLimitsAsync(CancellationToken ct = default) =>
        GetOrSeedAsync("rateLimits", new RateLimitsSettingsDto(60, 200, 3, 30, 100, "Redis Sliding Window Counter via Upstash Lua (IP 200/min, User 300/min)"), ct);

    public Task SetRateLimitsAsync(RateLimitsSettingsDto dto, Guid updatedBy, CancellationToken ct = default) =>
        SetAsync("rateLimits", dto, updatedBy, ct);

    public Task<MaintenanceSettingsDto> GetMaintenanceAsync(CancellationToken ct = default) =>
        GetOrSeedAsync("maintenance", new MaintenanceSettingsDto(false, null, null, null), ct);

    public Task SetMaintenanceAsync(MaintenanceSettingsDto dto, Guid updatedBy, CancellationToken ct = default) =>
        SetAsync("maintenance", dto, updatedBy, ct);

    public async Task<PlatformSettingsResponse> GetAllAsync(CancellationToken ct = default)
    {
        var general = await GetGeneralAsync(ct);
        var security = await GetSecurityAsync(ct);
        var ai = await GetAiAsync(ct);
        var rateLimits = await GetRateLimitsAsync(ct);
        var maintenance = await GetMaintenanceAsync(ct);
        var tenantDefaults = await GetOrSeedAsync("tenantDefaults", new TenantDefaultsDto("a0000000-0000-0000-0000-000000000010"), ct);
        var auth = new AuthSettingsDto(true, false, false, true);
        var email = new EmailSettingsDto("Brevo", "no-reply@flowboard.local", "FlowBoard", true, true);
        var storage = new StorageSettingsDto("Cloudinary", 25, new[] { "image/jpeg", "image/png", "image/webp", "application/pdf", "text/csv" });
        var notifications = new NotificationSettingsDto(true, true, "/hubs/board", "Healthy");
        return new PlatformSettingsResponse(general, security, auth, email, ai, storage, notifications, rateLimits, maintenance, tenantDefaults);
    }

    public async Task<bool> IsMaintenanceActiveAsync(CancellationToken ct = default)
    {
        var m = await GetMaintenanceAsync(ct);
        if (!m.MaintenanceMode) return false;
        // If scheduledAt is set, check window
        if (!string.IsNullOrWhiteSpace(m.ScheduledAt) && DateTime.TryParse(m.ScheduledAt, out var start))
        {
            if (DateTime.UtcNow < start.ToUniversalTime()) return false;
        }
        if (!string.IsNullOrWhiteSpace(m.EndAt) && DateTime.TryParse(m.EndAt, out var end))
        {
            if (DateTime.UtcNow > end.ToUniversalTime()) return false;
        }
        return true;
    }

    public async Task<string> UploadLogoAsync(string fileName, Stream stream, string contentType, CancellationToken ct = default)
    {
        if (_cloudinary == null) throw new InvalidOperationException("Cloudinary not configured");
        var sanitized = System.Text.RegularExpressions.Regex.Replace(fileName, "[^a-zA-Z0-9_\\-\\.]", "_");
        if (sanitized.Length > 40) sanitized = sanitized[..40];
        var folder = "flowboard/platform";
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(sanitized, stream),
            Folder = folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };
        var result = await _cloudinary.UploadAsync(uploadParams);
        if (result.Error != null) throw new InvalidOperationException(result.Error.Message);
        return result.SecureUrl?.ToString() ?? result.Url.ToString();
    }
}
