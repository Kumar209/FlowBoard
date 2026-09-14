using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.Commands;
using Project.Service.Application.Interfaces;
using Project.Service.Application.Queries;
using SharedKernel;

namespace Project.Service.Api.Controllers;

/// <summary>
/// Projects API — thin controllers, caching via pipeline. YARP routes /api/workspaces/{wid}/projects -> :5002.
/// </summary>
[ApiController]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _db;
    public ProjectsController(IMediator mediator, IApplicationDbContext db) { _mediator = mediator; _db = db; }

    [HttpPost("api/workspaces/{workspaceId}/projects")]
    public async Task<IActionResult> Create(Guid workspaceId, [FromBody] CreateProjectBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var role = GetRoleForWorkspace(workspaceId) ?? await GetRoleForWorkspaceDbAsync(workspaceId, userId.Value);
        if (role == null) return StatusCode(403, new { error = "Forbidden - Not a member of this workspace" });
        if (!Roles.IsPrivilegedForManage(new[] { role })) return StatusCode(403, new { error = $"Forbidden - Need OrgAdmin/SuperAdmin. Your role in this workspace: {role}" });
        var roles = GetRoles();
        var result = await _mediator.Send(new CreateProjectCommand(workspaceId, body.Name, body.Description, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpGet("api/workspaces/{workspaceId}/projects")]
    public async Task<IActionResult> GetByWorkspace(Guid workspaceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Clamp(page, 1, 1000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new GetProjectsQuery(workspaceId, page, pageSize));
        Response.Headers.Append("X-Total-Count", result.Total.ToString());
        return Ok(new { items = result.Items, total = result.Total, page, pageSize });
    }

    [HttpGet("api/projects/{projectId}")]
    public async Task<IActionResult> GetOne(Guid projectId)
    {
        try
        {
            var board = await _mediator.Send(new GetBoardQuery(projectId));
            if (board?.Project == null) return NotFound(new { error = "Project not found" });
            return Ok(board.Project);
        }
        catch (Exception ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { error = "Project not found" });
        }
    }

    [HttpGet("api/projects/{projectId}/board")]
    public async Task<IActionResult> GetBoard(Guid projectId, [FromQuery] Guid? boardId)
    {
        var board = await _mediator.Send(new GetBoardQuery(projectId, boardId));
        var etag = GenerateETag(board);
        Response.Headers["ETag"] = etag;
        if (Request.Headers.TryGetValue("If-None-Match", out var inm) && inm == etag) return StatusCode(304);
        Response.Headers["Cache-Control"] = "no-cache";
        return Ok(board);
    }
    private static string GenerateETag(object obj) { var json = System.Text.Json.JsonSerializer.Serialize(obj); var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)); return "\"" + Convert.ToHexString(hash)[..16] + "\""; }

    [HttpPut("api/projects/{projectId}")]
    public async Task<IActionResult> Update(Guid projectId, [FromBody] UpdateProjectBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        // Need workspaceId for role check - fetch project first
        var board = await _mediator.Send(new GetBoardQuery(projectId));
        if (board?.Project == null) return NotFound(new { error = "Project not found" });
        var role = GetRoleForWorkspace(board.Project.WorkspaceId) ?? await GetRoleForWorkspaceDbAsync(board.Project.WorkspaceId, userId.Value);
        if (role == null) return StatusCode(403, new { error = "Forbidden - Not a member of this workspace" });
        if (!Roles.IsPrivilegedForManage(new[] { role })) return StatusCode(403, new { error = $"Forbidden - Need OrgAdmin/SuperAdmin. Your role in this workspace: {role}" });
        var roles = GetRoles();
        var result = await _mediator.Send(new UpdateProjectCommand(projectId, body.Name, body.Description, body.Slug, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("api/projects/{projectId}/stats")]
    public async Task<IActionResult> GetStats(Guid projectId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new GetProjectStatsQuery(projectId, userId.Value));
        if (!result.IsSuccess) return result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("api/projects/{projectId}/chart-data")]
    public async Task<IActionResult> GetChartData(Guid projectId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new GetProjectChartDataQuery(projectId, userId.Value));
        if (!result.IsSuccess) return result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("api/projects/{projectId}/burndown")]
    public async Task<IActionResult> GetBurndown(Guid projectId, [FromQuery] Guid? sprintId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var result = await _mediator.Send(new GetBurndownQuery(projectId, sprintId, userId.Value));
        if (!result.IsSuccess) return result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(new { error = result.Error }) : result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("api/projects/{projectId}")]
    public async Task<IActionResult> Delete(Guid projectId)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var board = await _mediator.Send(new GetBoardQuery(projectId));
        if (board?.Project == null) return NotFound(new { error = "Project not found" });
        var role = GetRoleForWorkspace(board.Project.WorkspaceId) ?? await GetRoleForWorkspaceDbAsync(board.Project.WorkspaceId, userId.Value);
        if (role == null) return StatusCode(403, new { error = "Forbidden - Not a member of this workspace" });
        if (!Roles.IsPrivilegedForManage(new[] { role })) return StatusCode(403, new { error = $"Forbidden - Need OrgAdmin/SuperAdmin to delete. Your role: {role}" });
        var roles = GetRoles();
        var result = await _mediator.Send(new DeleteProjectCommand(projectId, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(new { message = "Deleted", projectId });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).Distinct().ToList();
    private string? GetRoleForWorkspace(Guid workspaceId)
    {
        var wids = User.FindAll("workspace_id").Select(c => c.Value).ToList();
        var roles = User.FindAll(ClaimTypes.Role).Concat(User.FindAll("role")).Select(c => c.Value).ToList();
        for (int i = 0; i < wids.Count && i < roles.Count; i++)
            if (Guid.TryParse(wids[i], out var g) && g == workspaceId) return roles[i];
        return null;
    }
    private async Task<string?> GetRoleForWorkspaceDbAsync(Guid workspaceId, Guid userId)
    {
        try
        {
            // Global SuperAdmin via IsSuperAdmin
            try { var isSuper = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM [identity].[Users] WHERE Id = {0} AND IsSuperAdmin = 1", userId).FirstOrDefaultAsync(); if (isSuper > 0) return Roles.SuperAdmin; } catch { }
            // Org-level OrgAdmin via OrganizationMembers or Owner
            try
            {
                var orgId = await _db.Database.SqlQueryRaw<Guid>("SELECT OrganizationId FROM [identity].[Workspaces] WHERE Id = {0}", workspaceId).FirstOrDefaultAsync();
                if (orgId != Guid.Empty)
                {
                    var isOwner = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM [identity].[Organizations] WHERE Id = {0} AND OwnerId = {1}", orgId, userId).FirstOrDefaultAsync();
                    if (isOwner > 0) return Roles.OrgAdmin;
                    var isOrgAdmin = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM [identity].[OrganizationMembers] WHERE OrganizationId = {0} AND UserId = {1} AND Role = 2", orgId, userId).FirstOrDefaultAsync();
                    if (isOrgAdmin > 0) return Roles.OrgAdmin;
                }
            } catch { }
            // Fallback to workspace membership
            var roleInt = await _db.Database.SqlQueryRaw<int?>("SELECT Role FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0} AND UserId = {1}", workspaceId, userId).FirstOrDefaultAsync();
            if (roleInt == null) return null;
            return roleInt.Value switch { 0 => Roles.SuperAdmin, 1 => Roles.Member, 2 => Roles.OrgAdmin, 3 => Roles.Client, _ => Roles.GetLabel(roleInt.Value) };
        }
        catch { return null; }
    }
}

public record CreateProjectBody(string Name, string? Description);
public record UpdateProjectBody(string Name, string? Description, string? Slug);
