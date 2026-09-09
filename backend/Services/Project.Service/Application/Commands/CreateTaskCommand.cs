using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;
using System.Text.Json;

namespace Project.Service.Application.Commands;

/// <summary>
/// CreateTask - card in BoardList. Title required, Priority Medium default, LabelsJson JSON array, AssigneeId optional. Allowed Member/PM/OrgAdmin/SuperAdmin (Client/Viewer 403). Publishes TaskCreated via Outbox (Task 3.1) for SignalR.
/// </summary>
public record CreateTaskCommand(Guid ProjectId, Guid? ListId, string Title, string? Description, string Priority, string? LabelsJson, Guid? AssigneeId, DateTime? DueDate, string? IssueType, string? Epic, int? StoryPoints, DateTime? StartDate, string? Environment, Guid? ParentIssueId, Guid? SprintId, Guid CallerId, List<string> CallerRoles, Guid? TeamId = null, Guid? StatusId = null) : IRequest<Result<TaskDto>>;

public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Priority).Must(p => new[] { "Low","Medium","High","Urgent" }.Contains(p)).When(x => !string.IsNullOrEmpty(x.Priority)).WithMessage("Priority must be Low/Medium/High/Urgent");
    }
}

public class CreateTaskHandler : IRequestHandler<CreateTaskCommand, Result<TaskDto>>
{
    private readonly ITaskService _service;
    public CreateTaskHandler(ITaskService service) => _service = service;
    public Task<Result<TaskDto>> Handle(CreateTaskCommand req, CancellationToken ct)
        => _service.CreateTaskAsync(req.ProjectId, req.ListId, req.Title, req.Description, req.Priority, req.LabelsJson, req.AssigneeId, req.DueDate, req.IssueType, req.Epic, req.StoryPoints, req.StartDate, req.Environment, req.ParentIssueId, req.SprintId, req.TeamId, req.CallerId, req.CallerRoles, ct, req.StatusId);
}
