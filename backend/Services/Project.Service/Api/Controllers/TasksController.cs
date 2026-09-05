using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Project.Service.Api.Controllers;

[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    // POST /api/tasks - Create task (Member, PM, OrgAdmin, SuperAdmin can, Client/Viewer cannot - Task 1.5)
    [HttpPost("api/tasks")]
    public IActionResult CreateTask([FromBody] CreateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { error = "Title required" });
        var roles = GetRoles();
        if (roles.Contains("Client") || roles.Contains("Viewer")) return StatusCode(403, new { error = "Forbidden - Client/Viewer cannot create tasks. Only Member/PM/OrgAdmin/SuperAdmin can." });
        // Allow Member, ProjectManager, OrgAdmin, SuperAdmin
        if (!IsInRole("Member", "ProjectManager", "OrgAdmin", "SuperAdmin")) return StatusCode(403, new { error = "Forbidden - need Member+ role" });

        return StatusCode(201, new { id = Guid.NewGuid(), title = request.Title, message = "Task created stub (Task 2.1 will persist)" });
    }

    // POST /api/projects/{projectId}/tasks - alternative route for same check
    [HttpPost("api/projects/{projectId}/tasks")]
    public IActionResult CreateTaskInProject(Guid projectId, [FromBody] CreateTaskRequest request)
    {
        return CreateTask(request);
    }

    // GET /api/tasks - any authenticated can view (filtered later)
    [HttpGet("api/tasks")]
    public IActionResult GetTasks() => Ok(new[] { new { id = Guid.NewGuid(), title = "Sample Task (stub)" } });

    private bool IsInRole(params string[] roles) => roles.Any(r => User.IsInRole(r) || GetRoles().Contains(r));
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).Distinct().ToList();
}

public record CreateTaskRequest(string Title, string? Description);
