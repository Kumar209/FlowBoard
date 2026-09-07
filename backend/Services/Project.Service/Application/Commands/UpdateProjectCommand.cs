using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// UpdateProject - PM/OrgAdmin/SuperAdmin can update name/description. Member/Client/Viewer 403.
/// </summary>
public record UpdateProjectCommand(Guid ProjectId, string Name, string? Description, string? Slug, Guid CallerId, List<string> CallerRoles) : IRequest<Result<ProjectDto>>;

public class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).Matches(@"^[a-z0-9-]+$").When(x => !string.IsNullOrWhiteSpace(x.Slug)).WithMessage("Slug must be lowercase a-z 0-9 -");
    }
}

public class UpdateProjectHandler : IRequestHandler<UpdateProjectCommand, Result<ProjectDto>>
{
    private readonly IProjectService _service;
    public UpdateProjectHandler(IProjectService service) => _service = service;
    public Task<Result<ProjectDto>> Handle(UpdateProjectCommand req, CancellationToken ct)
        => _service.UpdateProjectAsync(req.ProjectId, req.Name, req.Description, req.Slug, req.CallerId, req.CallerRoles, ct);
}
