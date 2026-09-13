using Microsoft.AspNetCore.Mvc;
using Identity.Service.Application.Interfaces;

namespace Identity.Service.Api.Controllers;

[ApiController]
[Route("api/platform")]
public class PlatformController : ControllerBase
{
    private readonly IPlatformSettingsService _platform;
    public PlatformController(IPlatformSettingsService platform) => _platform = platform;

    [HttpGet("general")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> GetGeneral()
    {
        var dto = await _platform.GetGeneralAsync();
        return Ok(dto);
    }

    [HttpGet("maintenance")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> GetMaintenance()
    {
        var dto = await _platform.GetMaintenanceAsync();
        var isActive = await _platform.IsMaintenanceActiveAsync();
        var retryAfter = 60;
        if (DateTime.TryParse(dto.EndAt, out var end)) retryAfter = Math.Max(60, (int)(end.ToUniversalTime() - DateTime.UtcNow).TotalSeconds);
        return Ok(new { dto.MaintenanceMode, dto.ScheduledAt, dto.EndAt, dto.Announcement, isActive, retryAfter });
    }

    [HttpGet("settings")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> GetSettings()
    {
        var dto = await _platform.GetAllAsync();
        return Ok(dto);
    }

    [HttpGet("rate-limits")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> GetRateLimits()
    {
        var dto = await _platform.GetRateLimitsAsync();
        return Ok(dto);
    }
}
