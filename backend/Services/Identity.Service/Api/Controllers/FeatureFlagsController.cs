using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Identity.Service.Application.SuperAdmin.Interfaces;

namespace Identity.Service.Api.Controllers;

[ApiController]
[Route("api/feature-flags")]
[Authorize]
public class FeatureFlagsController : ControllerBase
{
    private readonly ISuperAdminService _superAdminService;
    public FeatureFlagsController(ISuperAdminService superAdminService) => _superAdminService = superAdminService;

    [HttpGet]
    public async Task<IActionResult> GetForCurrentUser()
    {
        var flags = await _superAdminService.GetFeatureFlagsAsync();
        return Ok(flags);
    }

    [HttpGet("organizations/{orgId}")]
    public async Task<IActionResult> GetForOrganization(Guid orgId)
    {
        var flags = await _superAdminService.GetOrganizationFeatureFlagsAsync(orgId);
        return Ok(flags);
    }
}
