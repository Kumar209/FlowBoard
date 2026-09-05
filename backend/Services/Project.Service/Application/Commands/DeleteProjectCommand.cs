using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// DeleteProject - only OrgAdmin/SuperAdmin can delete. Cascades lists/tasks via FK.
/// </summary>
public record DeleteProjectCommand(Guid ProjectId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<bool>>;

public class DeleteProjectHandler : IRequestHandler<DeleteProjectCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public DeleteProjectHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }
    public async Task<Result<bool>> Handle(DeleteProjectCommand req, CancellationToken ct)
    {
        var allowed = new[] { "OrgAdmin", "SuperAdmin" };
        if (!req.CallerRoles.Any(r => allowed.Contains(r)))
            return Result<bool>.Failure("Forbidden - Need OrgAdmin/SuperAdmin to delete project");
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, ct);
        if (project == null) return Result<bool>.Failure("Project not found");
        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveByPrefixAsync($"projects:{project.WorkspaceId}:");
        await _cache.RemoveAsync($"board:{project.Id}");
        await _cache.RemoveByPrefixAsync($"tasks:{project.Id}:");
        return Result<bool>.Success(true);
    }
}
