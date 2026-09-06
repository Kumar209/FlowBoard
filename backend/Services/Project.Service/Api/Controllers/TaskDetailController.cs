using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.Queries;

namespace Project.Service.Api.Controllers;

/// <summary>
/// TaskDetail API - Jira-style aggregated fetch for modal: task + subtasks + comments.
/// </summary>
[ApiController]
[Authorize]
public class TaskDetailController : ControllerBase
{
    private readonly IMediator _mediator;
    public TaskDetailController(IMediator mediator) => _mediator = mediator;

    [HttpGet("api/tasks/{taskId}/detail")]
    public async Task<IActionResult> GetDetail(Guid taskId)
    {
        var detail = await _mediator.Send(new GetTaskDetailQuery(taskId));
        return Ok(detail);
    }
}
