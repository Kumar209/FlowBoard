using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;
using Project.Service.Domain.Entities;
using SharedKernel;
using System.Text.RegularExpressions;

namespace Project.Service.Infrastructure.Services;

public class StatusService : IStatusService
{
    private readonly IApplicationDbContext _db;
    public StatusService(IApplicationDbContext db) => _db = db;

    private static string Normalize(string name)
    {
        var trimmed = name.Trim();
        // Replace hyphens/underscores with space, collapse multiple spaces, lower for comparison but keep display as Title Case
        var spaced = Regex.Replace(trimmed, @"[-_]+", " ");
        spaced = Regex.Replace(spaced, @"\s+", " ");
        return spaced;
    }
    private static string DisplayName(string normalized)
    {
        // Title Case: In Progress
        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts.Select(p => char.ToUpper(p[0]) + p.Substring(1).ToLower()));
    }
    private static string NormalizedKey(string name) => Normalize(name).ToLowerInvariant();

    private async Task<bool> HasPermissionAsync(Guid callerId, Guid projectId, string permKey, CancellationToken ct)
    {
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
        if (permKey == "status:view") return true;
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
        var normalized = Normalize(name);
        var display = DisplayName(normalized);
        var key = NormalizedKey(name);
        var existing = await _db.Statuses.Where(s => s.ProjectId == projectId).ToListAsync(ct);
        if (existing.Any(s => NormalizedKey(s.Name) == key))
            return Result<StatusDto>.Failure($"Status '{display}' already exists (maybe as '{existing.First(s => NormalizedKey(s.Name)==key).Name}')");
        var allowed = callerRoles.Any(r => new[] { "OrgAdmin", "ProjectManager", "SuperAdmin" }.Contains(r));
        if (!allowed) return Result<StatusDto>.Failure("Forbidden - Need OrgAdmin/ProjectManager for status:create");
        var status = new Status(projectId, display);
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
        var normalized = Normalize(name);
        var display = DisplayName(normalized);
        var key = NormalizedKey(name);
        var existing = await _db.Statuses.Where(s => s.ProjectId == status.ProjectId && s.Id != statusId).ToListAsync(ct);
        if (existing.Any(s => NormalizedKey(s.Name) == key))
            return Result<StatusDto>.Failure($"Status '{display}' already exists as '{existing.First(s => NormalizedKey(s.Name)==key).Name}'");
        status.Rename(display);
        await _db.SaveChangesAsync(ct);
        return Result<StatusDto>.Success(new StatusDto(status.Id, status.ProjectId, status.Name, status.CreatedAt));
    }

    public async Task<Result<bool>> DeleteStatusAsync(Guid statusId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        var status = await _db.Statuses.FirstOrDefaultAsync(s => s.Id == statusId, ct);
        if (status == null) return Result<bool>.Failure("Status not found");
        var allowed = callerRoles.Any(r => new[] { "OrgAdmin", "ProjectManager", "SuperAdmin" }.Contains(r));
        if (!allowed) return Result<bool>.Failure("Forbidden - Need OrgAdmin/ProjectManager");
        var taskCount = await _db.Tasks.CountAsync(t => t.StatusId == statusId, ct);
        if (taskCount > 0) return Result<bool>.Failure($"Cannot delete status '{status.Name}' — {taskCount} issue(s) still use it. Reassign them to another status first.");
        var mappingCount = await _db.BoardColumnStatuses.CountAsync(bcs => bcs.StatusId == statusId, ct);
        if (mappingCount > 0) return Result<bool>.Failure($"Cannot delete status '{status.Name}' — {mappingCount} board column(s) still map to it. Remove mapping first.");
        _db.Statuses.Remove(status);
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
