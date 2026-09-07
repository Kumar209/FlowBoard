using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;
using ProjectEntity = Project.Service.Domain.Entities.Project;

namespace Project.Service.Application.Commands;

/// <summary>
/// CreateProject - only OrgAdmin/ProjectManager/SuperAdmin can create (Task 1.5 verified PM 201). Member/Client/Viewer gets 403 via handler Role check (not just OrgAdmin). Uses JWT CallerRoles from controller (ClaimTypes.Role).
/// </summary>
public record CreateProjectCommand(Guid WorkspaceId, string Name, string? Description, Guid CallerId, List<string> CallerRoles) : IRequest<Result<ProjectDto>>;

public class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty().WithMessage("WorkspaceId required");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage("Name required max 200");
        RuleFor(x => x.CallerId).NotEmpty();
    }
}

public class CreateProjectHandler : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
{
    private readonly IProjectService _service;
    public CreateProjectHandler(IProjectService service) => _service = service;
    public Task<Result<ProjectDto>> Handle(CreateProjectCommand req, CancellationToken ct)
        => _service.CreateProjectAsync(req.WorkspaceId, req.Name, req.Description, req.CallerId, req.CallerRoles, ct);
}
