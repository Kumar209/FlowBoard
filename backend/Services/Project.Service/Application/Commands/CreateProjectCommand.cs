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
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public CreateProjectHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }

    public async Task<Result<ProjectDto>> Handle(CreateProjectCommand req, CancellationToken ct)
    {
        // Policy: Only OrgAdmin, ProjectManager, SuperAdmin can create (Task 2.2 spec) - Member/Client/Viewer 403
        var allowed = new[] { "OrgAdmin", "ProjectManager", "SuperAdmin" };
        if (!req.CallerRoles.Any(r => allowed.Contains(r)))
            return Result<ProjectDto>.Failure("Forbidden - Need OrgAdmin/ProjectManager. Your roles: " + string.Join(",", req.CallerRoles));

        // Generate Key like FB-3: first 2-3 letters of name uppercase + count
        var prefix = new string(req.Name.Where(char.IsLetter).Take(3).ToArray()).ToUpperInvariant();
        if (prefix.Length < 2) prefix = "PRJ";
        var count = await _db.Projects.CountAsync(p => p.WorkspaceId == req.WorkspaceId, ct);
        var key = $"{prefix}-{count + 1}";

        // Ensure unique WorkspaceId+Key (index unique)
        var exists = await _db.Projects.AnyAsync(p => p.WorkspaceId == req.WorkspaceId && p.Key == key, ct);
        if (exists) key = $"{prefix}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

        var project = new ProjectEntity(req.WorkspaceId, req.Name, key, req.CallerId, req.Description);
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        // Create 4 default lists: To Do, In Progress, In Review, Done (Position 0-3) - Jira pattern
        var lists = new[]
        {
            new Domain.Entities.BoardList(project.Id, "To Do", 0),
            new Domain.Entities.BoardList(project.Id, "In Progress", 1),
            new Domain.Entities.BoardList(project.Id, "In Review", 2),
            new Domain.Entities.BoardList(project.Id, "Done", 3)
        };
        _db.BoardLists.AddRange(lists);
        await _db.SaveChangesAsync(ct);

        // Activity log
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(project.Id, null, req.CallerId, "ProjectCreated", $"{{\"name\":\"{req.Name}\",\"key\":\"{key}\"}}"));
        await _db.SaveChangesAsync(ct);

        // MNC-grade: invalidate workspace projects cache (pipeline will MISS next GET)
        await _cache.RemoveByPrefixAsync($"projects:{req.WorkspaceId}:");

        var dto = new ProjectDto(project.Id, project.WorkspaceId, project.Name, project.Key, project.Description, project.OwnerId, project.CreatedAt);
        return Result<ProjectDto>.Success(dto);
    }
}
