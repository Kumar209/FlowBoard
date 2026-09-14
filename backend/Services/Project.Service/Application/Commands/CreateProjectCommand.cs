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
/// CreateProject - only OrgAdmin/SuperAdmin (fixed) can create; custom workspace roles require permission project:create via RolePermissions. Member/Client gets 403 via service check.
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
