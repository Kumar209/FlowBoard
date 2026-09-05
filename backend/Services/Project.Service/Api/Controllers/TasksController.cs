using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.Commands;
using Project.Service.Application.Queries;

namespace Project.Service.Api.Controllers;

/// <summary>
/// Tasks API - thin controllers (MNC-grade pipeline caching for queries, invalidation in handlers). YARP /api/tasks/{**catch-all} -> :5002. Filtering via GetTasksQuery.
/// </summary>
[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;
    public TasksController(IMediator mediator) => _mediator = mediator;

    [HttpPost("api/tasks")]
    [HttpPost("api/projects/{projectId}/tasks")]
    public async Task<IActionResult> Create([FromRoute] Guid? projectId, [FromBody] CreateTaskBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var pid = projectId ?? body.ProjectId;
        var lid = body.ListId;
        if (pid == Guid.Empty || lid == Guid.Empty) return BadRequest(new { error = "ProjectId and ListId required" });
        var result = await _mediator.Send(new CreateTaskCommand(pid, lid, body.Title, body.Description, body.Priority ?? "Medium", body.LabelsJson, body.AssigneeId, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return StatusCode(201, result.Value);
    }

    [HttpGet("api/tasks")]
    public async Task<IActionResult> Get([FromQuery] Guid projectId, [FromQuery] string? search, [FromQuery] Guid? assigneeId, [FromQuery] string? priority, [FromQuery] string? label, [FromQuery] DateTime? dueFrom, [FromQuery] DateTime? dueTo, [FromQuery] string? sortBy, [FromQuery] bool sortDesc = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (projectId == Guid.Empty) return BadRequest(new { error = "projectId query required (?projectId=...)" });
        // MNC-grade: caching via CachingBehavior pipeline (ICacheableRequest), not controller manual Get/Set
        var result = await _mediator.Send(new GetTasksQuery(projectId, search, assigneeId, priority, label, dueFrom, dueTo, sortBy, sortDesc, page, pageSize));
        return Ok(result);
    }

    [HttpGet("api/projects/{projectId}/tasks")]
    public async Task<IActionResult> GetByProject(Guid projectId, [FromQuery] string? search, [FromQuery] Guid? assigneeId, [FromQuery] string? priority, [FromQuery] string? label, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return await Get(projectId, search, assigneeId, priority, label, null, null, null, false, page, pageSize);
    }

    [HttpPut("api/tasks/{taskId}/move")]
    public async Task<IActionResult> Move(Guid taskId, [FromBody] MoveTaskBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new MoveTaskCommand(taskId, body.ToListId, body.NewPosition, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(new { message = "Moved" });
    }

    [HttpPut("api/tasks/{taskId}")]
    public async Task<IActionResult> Update(Guid taskId, [FromBody] UpdateTaskBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new UpdateTaskCommand(taskId, body.Title, body.Description, body.Priority ?? "Medium", body.LabelsJson, body.AssigneeId, body.DueDate, userId.Value, roles));
        if (!result.IsSuccess) return result.Error!.Contains("Forbidden") ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).Distinct().ToList();
}

public record CreateTaskBody(Guid ProjectId, Guid ListId, string Title, string? Description, string? Priority, string? LabelsJson, Guid? AssigneeId);
public record MoveTaskBody(Guid ToListId, int NewPosition);
public record UpdateTaskBody(string Title, string? Description, string? Priority, string? LabelsJson, Guid? AssigneeId, DateTime? DueDate);
