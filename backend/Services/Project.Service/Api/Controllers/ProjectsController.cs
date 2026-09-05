using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Project.Service.Api.Controllers;

[ApiController]
[Authorize]
public class ProjectsController : ControllerBase
{
    // POST /api/workspaces/{workspaceId}/projects - Only OrgAdmin, ProjectManager, SuperAdmin can create (Task 1.5 + 2.2)
    [HttpPost("api/workspaces/{workspaceId}/projects")]
    public IActionResult CreateProject(Guid workspaceId, [FromBody] CreateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { error = "Name required" });
        if (!IsInRole("OrgAdmin", "ProjectManager", "SuperAdmin")) return StatusCode(403, new { error = "Forbidden - Need OrgAdmin/ProjectManager. Your roles: " + string.Join(",", GetRoles()) + " (Client/Member/Viewer cannot create)" });

        // Stub - no DB yet (Task 2.1 will add EF Core). Return 201 to prove RBAC works.
        var projectId = Guid.NewGuid();
        return StatusCode(201, new { id = projectId, workspaceId, name = request.Name, key = request.Name[..Math.Min(3, request.Name.Length)].ToUpper() + "-1", message = "Project created (stub - Task 2.1 will persist to [project] schema)" });
    }

    // GET /api/workspaces/{workspaceId}/projects - Any member can view assigned workspace projects
    [HttpGet("api/workspaces/{workspaceId}/projects")]
    public IActionResult GetProjects(Guid workspaceId)
    {
        // Check authenticated - all 6 roles can view (filtered by assignment later)
        if (!User.Identity?.IsAuthenticated ?? true) return Unauthorized();
        return Ok(new[] { new { id = Guid.NewGuid(), workspaceId, name = "Demo Project (stub)", key = "DEM-1" } });
    }

    private bool IsInRole(params string[] roles) => roles.Any(r => User.IsInRole(r) || GetRoles().Contains(r));
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).Concat(User.FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(c => c.Value)).Distinct().ToList();
}

public record CreateProjectRequest(string Name, string? Description);
