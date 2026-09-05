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
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public UpdateProjectHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }
    public async Task<Result<ProjectDto>> Handle(UpdateProjectCommand req, CancellationToken ct)
    {
        var allowed = new[] { "OrgAdmin", "ProjectManager", "SuperAdmin" };
        if (!req.CallerRoles.Any(r => allowed.Contains(r)))
            return Result<ProjectDto>.Failure("Forbidden - Need OrgAdmin/ProjectManager");
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, ct);
        if (project == null) return Result<ProjectDto>.Failure("Project not found");
        project.Update(req.Name, req.Description);
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveByPrefixAsync($"projects:{project.WorkspaceId}:");
        await _cache.RemoveAsync($"board:{project.Id}");
        var dto = new ProjectDto(project.Id, project.WorkspaceId, project.Name, project.Key, project.Description, project.OwnerId, project.CreatedAt);
        return Result<ProjectDto>.Success(dto);
    }
}
