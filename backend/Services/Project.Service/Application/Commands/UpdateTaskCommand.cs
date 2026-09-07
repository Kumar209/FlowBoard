using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// UpdateTask - edit title/description/priority/labels/assignee/dueDate + IssueType/Epic/StoryPoints/StartDate/Environment/Sprint/Parent/Time/Watches. Allowed Member+ (Client 403 for title change but can comment via AddComment). Validates Title required.
/// </summary>
public record UpdateTaskCommand(Guid TaskId, string Title, string? Description, string Priority, string? LabelsJson, Guid? AssigneeId, DateTime? DueDate, Guid CallerId, List<string> CallerRoles, string? IssueType = null, string? Epic = null, int? StoryPoints = null, DateTime? StartDate = null, string? Environment = null, Guid? ParentIssueId = null, Guid? SprintId = null, string? WatchersJson = null, string? LinkedIssuesJson = null, int? TimeEstimated = null, int? TimeSpent = null, int? TimeRemaining = null, Guid? TeamId = null, Guid? ListId = null, string? Status = null) : IRequest<Result<TaskDto>>;

public class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Priority).Must(p => new[] { "Low","Medium","High","Urgent" }.Contains(p)).When(x => !string.IsNullOrEmpty(x.Priority));
    }
}

public class UpdateTaskHandler : IRequestHandler<UpdateTaskCommand, Result<TaskDto>>
{
    private readonly ITaskService _service;
    public UpdateTaskHandler(ITaskService service) => _service = service;
    public Task<Result<TaskDto>> Handle(UpdateTaskCommand req, CancellationToken ct)
        => _service.UpdateTaskAsync(req.TaskId, req.Title, req.Description, req.Priority, req.LabelsJson, req.AssigneeId, req.DueDate, req.IssueType, req.Epic, req.StoryPoints, req.StartDate, req.Environment, req.ParentIssueId, req.SprintId, req.WatchersJson, req.LinkedIssuesJson, req.TimeEstimated, req.TimeSpent, req.TimeRemaining, req.TeamId, req.ListId, req.Status, req.CallerId, req.CallerRoles, ct);
}
