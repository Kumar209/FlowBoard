using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;
using Project.Service.Domain.Entities;
using SharedKernel;

namespace Project.Service.Infrastructure.Services;

public class StatusService : IStatusService
{
    private readonly IApplicationDbContext _db;
    public StatusService(IApplicationDbContext db) => _db = db;

    private async Task<bool> HasPermissionAsync(Guid callerId, Guid projectId, string permKey, CancellationToken ct)
    {
        // Check SuperAdmin or OrgAdmin via workspace
        var wsId = await _db.Projects.Where(p => p.Id == projectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync(ct);
        if (wsId != Guid.Empty)
        {
            try
            {
                var isSuper = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[WorkspaceMembers] WHERE UserId = {0} AND Role = 5", callerId).FirstOrDefaultAsync(ct) > 0;
                if (isSuper) return true;
                var isOrgAdmin = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0} AND UserId = {1} AND Role = 2", wsId, callerId).FirstOrDefaultAsync(ct) > 0;
                if (isOrgAdmin) return true;
            } catch { }
        }
        // Check status:view via RolePermissions if needed - allow all project members for view
        if (permKey == "status:view") return true;
        // For create/update/delete require OrgAdmin/PM - simplified
        return false;
    }

    public async Task<List<StatusDto>> GetStatusesAsync(Guid projectId, CancellationToken ct = default)
    {
        var list = await _db.Statuses.Where(s => s.ProjectId == projectId).OrderBy(s => s.CreatedAt).ToListAsync(ct);
        return list.Select(s => new StatusDto(s.Id, s.ProjectId, s.Name, s.CreatedAt)).ToList();
    }

    public async Task<Result<StatusDto>> CreateStatusAsync(Guid projectId, string name, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result<StatusDto>.Failure("Name required");
        var trimmed = name.Trim();
        if (await _db.Statuses.AnyAsync(s => s.ProjectId == projectId && s.Name.ToLower() == trimmed.ToLower(), ct))
            return Result<StatusDto>.Failure("Status name already exists in this project");
        // Permission check: need OrgAdmin/PM or status:create
        var allowed = callerRoles.Any(r => new[] { "OrgAdmin", "ProjectManager", "SuperAdmin" }.Contains(r));
        if (!allowed) return Result<StatusDto>.Failure("Forbidden - Need OrgAdmin/ProjectManager for status:create");
        var status = new Status(projectId, trimmed);
        _db.Statuses.Add(status);
        await _db.SaveChangesAsync(ct);
        return Result<StatusDto>.Success(new StatusDto(status.Id, status.ProjectId, status.Name, status.CreatedAt));
    }

    public async Task<Result<StatusDto>> UpdateStatusAsync(Guid statusId, string name, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result<StatusDto>.Failure("Name required");
        var status = await _db.Statuses.FirstOrDefaultAsync(s => s.Id == statusId, ct);
        if (status == null) return Result<StatusDto>.Failure("Status not found");
        var allowed = callerRoles.Any(r => new[] { "OrgAdmin", "ProjectManager", "SuperAdmin" }.Contains(r));
        if (!allowed) return Result<StatusDto>.Failure("Forbidden - Need OrgAdmin/ProjectManager");
        var trimmed = name.Trim();
        if (await _db.Statuses.AnyAsync(s => s.ProjectId == status.ProjectId && s.Name.ToLower() == trimmed.ToLower() && s.Id != statusId, ct))
            return Result<StatusDto>.Failure("Status name already exists");
        status.Rename(trimmed);
        await _db.SaveChangesAsync(ct);
        return Result<StatusDto>.Success(new StatusDto(status.Id, status.ProjectId, status.Name, status.CreatedAt));
    }

    public async Task<Result<bool>> DeleteStatusAsync(Guid statusId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        var status = await _db.Statuses.FirstOrDefaultAsync(s => s.Id == statusId, ct);
        if (status == null) return Result<bool>.Failure("Status not found");
        var allowed = callerRoles.Any(r => new[] { "OrgAdmin", "ProjectManager", "SuperAdmin" }.Contains(r));
        if (!allowed) return Result<bool>.Failure("Forbidden - Need OrgAdmin/ProjectManager");
        var inUse = await _db.Tasks.AnyAsync(t => t.StatusId == statusId, ct);
        if (inUse) return Result<bool>.Failure("Cannot delete status in use by issues - reassign or delete issues first");
        var mappings = await _db.BoardColumnStatuses.Where(bcs => bcs.StatusId == statusId).ToListAsync(ct);
        _db.BoardColumnStatuses.RemoveRange(mappings);
        _db.Statuses.Remove(status);
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
