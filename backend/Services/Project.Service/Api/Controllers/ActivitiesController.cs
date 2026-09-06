using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.Queries;

namespace Project.Service.Api.Controllers;

/// <summary>
/// Activities API - GET /api/projects/{projectId}/activities paginated timeline for burndown (Task 4.4).
/// </summary>
[ApiController]
[Authorize]
public class ActivitiesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ActivitiesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("api/projects/{projectId}/activities")]
    public async Task<IActionResult> Get(Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? taskId = null)
    {
        var q = new GetActivitiesQuery(projectId, page, pageSize, taskId);
        var (items, total) = await _mediator.Send(q);
        Response.Headers.Append("X-Total-Count", total.ToString());
        return Ok(new { items, total, page, pageSize });
    }
}
