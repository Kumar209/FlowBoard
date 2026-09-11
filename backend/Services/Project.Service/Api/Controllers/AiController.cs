using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Service.Application.AI.Commands;

namespace Project.Service.Api.Controllers;

/// <summary>
/// AiController - dedicated AI bounded context (Option A, 7.1 separate AI folder). All AI ops via /api/ai/* → project-cluster :5002 (yarp.json ai-route). Thin DIP: IMediator only → Command/Query → IAiService → Infrastructure/AI (Gemini/Groq + Redis + AiUsageLogs). Ready to extract to microservice.
/// 7.2 Draft (A): POST /api/ai/draft {prompt,model,projectId} → JSON {title,description,checklist,labels,priority,issueType,storyPoints} isDraft preview (no Id) → frontend Create POST /tasks.
/// 7.3 Enhance (B): POST /api/ai/enhance {taskId,title,description,model,projectId} → Gemini {title,description} diff Current vs AI Apply pending until Save PUT.
/// 7.4 Criteria (C): POST /api/ai/criteria {taskId,title,description,model,projectId} → Gemini {criteria:string[4-6]} checkbox editable Apply pending until Save PUT acceptanceCriteriaJson nullable.
/// 7.5 Breakdown (D): POST /api/ai/breakdown {taskId,title,description,model,projectId} → Gemini {subtasks:string[3-6]} checkbox editable Create Selected pending until Save PUT+POST batch.
/// Rate limit 3/min per ai:{userId}:{model} + 5 RPM global via AiService → 429 + RetryAfter.
/// </summary>
[ApiController]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IMediator _mediator;
    public AiController(IMediator mediator) => _mediator = mediator;

    [HttpPost("api/ai/draft")]
    public async Task<IActionResult> Draft([FromBody] DraftBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var cmd = new GenerateDraftCommand(body.Prompt?.Trim() ?? "", body.Model?.Trim() ?? "gemini-3.5-flash", userId.Value, body.ProjectId);
        var result = await _mediator.Send(cmd);
        if (!result.IsSuccess)
        {
            var err = result.Error ?? "AI failed";
            if (err.Contains("Too Many Requests") || err.Contains("Rate limit") || err.Contains("429"))
            {
                var retryAfter = ExtractRetryAfter(err);
                Response.Headers.Append("Retry-After", retryAfter.ToString());
                return StatusCode(429, new { error = err, retryAfter });
            }
            return BadRequest(new { error = err });
        }
        return Ok(result.Value);
    }

    [HttpPost("api/ai/enhance")]
    public async Task<IActionResult> Enhance([FromBody] EnhanceBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var cmd = new GenerateEnhanceCommand(body.Title?.Trim() ?? "", body.Description ?? "", body.Model?.Trim() ?? "gemini-3.5-flash", userId.Value, body.ProjectId, body.TaskId);
        var result = await _mediator.Send(cmd);
        if (!result.IsSuccess)
        {
            var err = result.Error ?? "AI failed";
            if (err.Contains("Too Many Requests") || err.Contains("Rate limit") || err.Contains("429"))
            {
                var retryAfter = ExtractRetryAfter(err);
                Response.Headers.Append("Retry-After", retryAfter.ToString());
                return StatusCode(429, new { error = err, retryAfter });
            }
            return BadRequest(new { error = err });
        }
        return Ok(result.Value);
    }

    [HttpPost("api/ai/criteria")]
    public async Task<IActionResult> Criteria([FromBody] CriteriaBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var cmd = new GenerateCriteriaCommand(body.Title?.Trim() ?? "", body.Description ?? "", body.Model?.Trim() ?? "gemini-3.5-flash", userId.Value, body.ProjectId, body.TaskId);
        var result = await _mediator.Send(cmd);
        if (!result.IsSuccess)
        {
            var err = result.Error ?? "AI failed";
            if (err.Contains("Too Many Requests") || err.Contains("Rate limit") || err.Contains("429"))
            {
                var retryAfter = ExtractRetryAfter(err);
                Response.Headers.Append("Retry-After", retryAfter.ToString());
                return StatusCode(429, new { error = err, retryAfter });
            }
            return BadRequest(new { error = err });
        }
        return Ok(result.Value);
    }

    [HttpPost("api/ai/breakdown")]
    public async Task<IActionResult> Breakdown([FromBody] BreakdownBody body)
    {
        var userId = GetUserId(); if (userId == null) return Unauthorized();
        var cmd = new GenerateBreakdownCommand(body.Title?.Trim() ?? "", body.Description ?? "", body.Model?.Trim() ?? "gemini-3.5-flash", userId.Value, body.ProjectId, body.TaskId);
        var result = await _mediator.Send(cmd);
        if (!result.IsSuccess)
        {
            var err = result.Error ?? "AI failed";
            if (err.Contains("Too Many Requests") || err.Contains("Rate limit") || err.Contains("429"))
            {
                var retryAfter = ExtractRetryAfter(err);
                Response.Headers.Append("Retry-After", retryAfter.ToString());
                return StatusCode(429, new { error = err, retryAfter });
            }
            return BadRequest(new { error = err });
        }
        return Ok(result.Value);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var g) ? g : null;
    }
    private static int ExtractRetryAfter(string err)
    {
        var idx = err.IndexOf("Retry-After:");
        if (idx >= 0)
        {
            var sub = err[(idx + "Retry-After:".Length)..];
            var num = new string(sub.TakeWhile(c => char.IsDigit(c)).ToArray());
            if (int.TryParse(num, out var r)) return r;
        }
        // fallback from "Retry after Xs"
        idx = err.IndexOf("Retry after");
        if (idx >= 0)
        {
            var sub = err[idx..];
            var num = new string(sub.Where(char.IsDigit).TakeWhile(char.IsDigit).ToArray());
            // simpler: extract first int after "after"
            var after = err.Substring(idx);
            var digits = System.Text.RegularExpressions.Regex.Match(after, @"\d+");
            if (digits.Success && int.TryParse(digits.Value, out var v)) return v;
        }
        return 60;
    }
}

public record DraftBody(string Prompt, string? Model, Guid? ProjectId);
public record EnhanceBody(string Title, string? Description, string? Model, Guid? ProjectId, Guid? TaskId);
public record CriteriaBody(string Title, string? Description, string? Model, Guid? ProjectId, Guid? TaskId);
public record BreakdownBody(string Title, string? Description, string? Model, Guid? ProjectId, Guid? TaskId);
