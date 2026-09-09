using Microsoft.EntityFrameworkCore;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;

namespace Identity.Service.Infrastructure.Services;

public class OrganizationActivityService : IOrganizationActivityService
{
    private readonly IApplicationDbContext _db;
    public OrganizationActivityService(IApplicationDbContext db) => _db = db;

    private async Task<bool> CanViewAsync(Guid organizationId, Guid callerId, CancellationToken ct)
    {
        if (await _db.WorkspaceMembers.AnyAsync(m => m.UserId == callerId && m.Role == Domain.Enums.WorkspaceRole.SuperAdmin, ct)) return true;
        if (await _db.Organizations.AnyAsync(o => o.Id == organizationId && o.OwnerId == callerId, ct)) return true;
        if (await _db.OrganizationMembers.AnyAsync(m => m.OrganizationId == organizationId && m.UserId == callerId && m.Role == 2, ct)) return true;
        // check custom role permission activity:view:org
        var customRoleIds = await _db.WorkspaceMembers.Where(m => m.UserId == callerId && m.CustomRoleId != null).Select(m => m.CustomRoleId!.Value).ToListAsync(ct);
        if (customRoleIds.Any())
        {
            var hasPerm = await _db.Permissions.Where(p => p.Key == "activity:view:org")
                .Join(_db.RolePermissions.Where(rp => customRoleIds.Contains(rp.RoleId)), p => p.Id, rp => rp.PermissionId, (p, rp) => p)
                .AnyAsync(ct);
            if (hasPerm) return true;
        }
        // also check if OrgAdmin via WorkspaceMembers join
        var isOrgAdmin = await _db.WorkspaceMembers.Where(m => m.UserId == callerId)
            .Join(_db.Workspaces.Where(w => w.OrganizationId == organizationId), m => m.WorkspaceId, w => w.Id, (m, w) => m)
            .AnyAsync(m => m.Role == Domain.Enums.WorkspaceRole.OrgAdmin, ct);
        return isOrgAdmin;
    }

    public async Task<(List<OrganizationActivityDto> Items, int Total)> GetActivitiesAsync(Guid organizationId, int page, int pageSize, Guid callerId, CancellationToken ct = default)
    {
        if (!await _db.Organizations.AnyAsync(o => o.Id == organizationId, ct)) throw new NotFoundException("Organization not found");
        if (!await CanViewAsync(organizationId, callerId, ct)) throw new ForbiddenException("Forbidden - Need activity:view:org (OrgAdmin)");
        var q = _db.OrganizationActivities.Where(a => a.OrganizationId == organizationId).OrderByDescending(a => a.OccurredOn);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        // enrich actor names
        var actorIds = items.Select(i => i.ActorUserId).Distinct().ToList();
        var actorMap = new Dictionary<Guid, string>();
        foreach (var aid in actorIds)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == aid, ct);
            if (user != null) actorMap[aid] = user.FullName;
        }
        var dtos = items.Select(a => new OrganizationActivityDto(a.Id, a.OrganizationId, a.ActorUserId, a.Action, a.PayloadJson, a.OccurredOn, actorMap.TryGetValue(a.ActorUserId, out var n) ? n : null)).ToList();
        return (dtos, total);
    }

    public async Task LogAsync(Guid organizationId, Guid actorUserId, string action, string? payloadJson, CancellationToken ct = default)
    {
        var act = new OrganizationActivity(organizationId, actorUserId, action, payloadJson);
        _db.OrganizationActivities.Add(act);
        await _db.SaveChangesAsync(ct);
    }
}
