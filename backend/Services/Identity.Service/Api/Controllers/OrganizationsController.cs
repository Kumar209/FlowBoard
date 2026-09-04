using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;

namespace Identity.Service.Api.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public OrganizationsController(IApplicationDbContext db) => _db = db;

    // GET /api/organizations - list orgs where user is member of any workspace in org
    [HttpGet]
    public async Task<IActionResult> GetMyOrganizations()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var workspaceOrgIds = await _db.WorkspaceMembers
            .Where(m => m.UserId == userId.Value)
            .Select(m => m.Workspace)
            .Where(w => w != null)
            .Select(w => w!.OrganizationId)
            .Distinct()
            .ToListAsync();

        var orgs = await _db.Organizations
            .Where(o => workspaceOrgIds.Contains(o.Id) || o.OwnerId == userId.Value)
            .Select(o => new { o.Id, o.Name, o.Slug, o.OwnerId, o.CreatedAt })
            .ToListAsync();

        return Ok(orgs);
    }

    // POST /api/organizations - any authenticated user can create org (becomes Owner)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrgRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { error = "Name required" });

        var slug = request.Name.ToLowerInvariant().Replace(" ", "-") + "-" + Guid.NewGuid().ToString()[..6];
        var org = new Organization(request.Name, slug, userId.Value);
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyOrganizations), new { id = org.Id }, new { org.Id, org.Name, org.Slug });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
}

public record CreateOrgRequest(string Name);
