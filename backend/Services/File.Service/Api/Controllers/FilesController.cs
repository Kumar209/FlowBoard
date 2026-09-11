using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using File.Service.Application.Commands;
using File.Service.Application.Queries;

namespace File.Service.Api.Controllers;

[ApiController]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;
    public FilesController(IMediator mediator) => _mediator = mediator;

    // POST /api/files/upload (multipart form: taskId + file)
    [HttpPost("api/files/upload")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] UploadForm form, CancellationToken ct)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        if (form.File == null || form.File.Length == 0) return BadRequest(new { error = "File required" });
        if (form.TaskId == Guid.Empty) return BadRequest(new { error = "TaskId required" });
        if (form.File.Length > 10 * 1024 * 1024) return BadRequest(new { error = "File too large >10MB" });

        using var stream = form.File.OpenReadStream();
        var cmd = new UploadAttachmentCommand(form.TaskId, form.File.FileName, form.File.ContentType ?? "application/octet-stream", form.File.Length, stream, userId.Value, roles);
        var result = await _mediator.Send(cmd, ct);
        if (!result.IsSuccess)
        {
            if (result.Error!.Contains("Forbidden") || result.Error.Contains("Not a")) return StatusCode(403, new { error = result.Error });
            if (result.Error.Contains("not found") || result.Error.Contains("Task not found")) return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return StatusCode(201, result.Value);
    }

    // GET /api/tasks/{taskId}/attachments
    [HttpGet("api/tasks/{taskId}/attachments")]
    public async Task<IActionResult> GetByTask(Guid taskId, CancellationToken ct)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        try
        {
            var result = await _mediator.Send(new GetAttachmentsQuery(taskId, userId.Value, roles), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
    }

    // DELETE /api/files/{id}
    [HttpDelete("api/files/{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var roles = GetRoles();
        var result = await _mediator.Send(new DeleteAttachmentCommand(id, userId.Value, roles), ct);
        if (!result.IsSuccess)
        {
            if (result.Error!.Contains("Forbidden") || result.Error.Contains("Not a")) return StatusCode(403, new { error = result.Error });
            if (result.Error.Contains("not found")) return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(new { message = "Deleted" });
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private List<string> GetRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(User.FindAll("role").Select(c => c.Value)).Distinct().ToList();
}

public class UploadForm
{
    public Guid TaskId { get; set; }
    public IFormFile? File { get; set; }
}
