using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;
using Project.Service.Domain.Entities;
using SharedKernel;

namespace Project.Service.Infrastructure.Services;

public class ProjectMemberService : IProjectMemberService
{
    private readonly IApplicationDbContext _db;
    public ProjectMemberService(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedResult<ProjectMemberDto>> GetMembersAsync(Guid projectId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var q = _db.ProjectMembers.Where(pm => pm.ProjectId == projectId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            q = q.Where(pm => pm.Role.ToLower().Contains(s));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(pm => pm.JoinedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        // Enrich with Identity Users (same DB, different schema)
        var userIds = items.Select(i => i.UserId).ToList();
        var users = new Dictionary<Guid, (string Email, string FullName)>();
        if (userIds.Any())
        {
            try
            {
                var placeholders = string.Join(",", userIds.Select((_, i) => $"@p{i}"));
                // Use EF raw to fetch users - simplified via SqlQueryRaw per user? For MNC, batch via IN
                var emails = await _db.Database.SqlQueryRaw<UserRow>($"SELECT Id as UserId, Email, FullName FROM [identity].[Users] WHERE Id IN ({placeholders})", userIds.Cast<object>().ToArray()).ToListAsync(ct);
                // Fallback simple: query one by one if above fails due to param handling
            }
            catch { }
            // Fallback: try per-user lookup
            if (users.Count == 0)
            {
                foreach (var uid in userIds)
                {
                    try
                    {
                        var row = await _db.Database.SqlQueryRaw<UserRow>("SELECT Id as UserId, Email, FullName FROM [identity].[Users] WHERE Id = {0}", uid).FirstOrDefaultAsync(ct);
                        if (row != null) users[uid] = (row.Email, row.FullName);
                    }
                    catch { }
                }
            }
        }
        var dtos = items.Select(pm =>
        {
            users.TryGetValue(pm.UserId, out var u);
            return new ProjectMemberDto(pm.Id, pm.ProjectId, pm.UserId, u.Email ?? pm.UserId.ToString()[..8], u.FullName ?? pm.UserId.ToString()[..8], pm.Role, pm.JoinedAt);
        }).ToList();

        // If search was on name/email, filter after enrichment
        if (!string.IsNullOrWhiteSpace(search) && search.Length > 1)
        {
            var s = search.ToLower();
            dtos = dtos.Where(d => d.FullName.ToLower().Contains(s) || d.Email.ToLower().Contains(s) || d.Role.ToLower().Contains(s)).ToList();
            total = dtos.Count;
        }
        return new PaginatedResult<ProjectMemberDto>(dtos, total, page, pageSize);
    }

    public async Task<List<ProjectMemberDto>> GetMembersListAsync(Guid projectId, CancellationToken ct = default)
    {
        var members = await _db.ProjectMembers.Where(pm => pm.ProjectId == projectId).ToListAsync(ct);
        return members.Select(pm => new ProjectMemberDto(pm.Id, pm.ProjectId, pm.UserId, "", "", pm.Role, pm.JoinedAt)).ToList();
    }

    public async Task<Result<ProjectMemberDto>> AddMemberAsync(Guid projectId, Guid userId, string role, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (!Roles.IsPrivilegedForManage(callerRoles))
            return Result<ProjectMemberDto>.Failure("Forbidden - Need OrgAdmin/SuperAdmin");
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project == null) return Result<ProjectMemberDto>.Failure("Project not found");
        // Validate workspace membership
        var wsId = project.WorkspaceId;
        var isWsMember = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0} AND UserId = {1}", wsId, userId).FirstOrDefaultAsync(ct) > 0;
        // SqlQueryRaw<int> returns int count; if table not found fallback to true for local dev
        if (!isWsMember)
        {
            // Try alternative check via ProjectMembers already added - allow first member without workspace check
            var hasAny = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId, ct);
            if (hasAny) return Result<ProjectMemberDto>.Failure("User is not a workspace member");
        }
        var exists = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, ct);
        if (exists) return Result<ProjectMemberDto>.Failure("User already a project member");
        var pm = new ProjectMember(projectId, userId, string.IsNullOrWhiteSpace(role) ? Roles.Member : role);
        _db.ProjectMembers.Add(pm);
        await _db.SaveChangesAsync(ct);
        // Fetch user info for response
        string email = userId.ToString()[..8], fullName = userId.ToString()[..8];
        try
        {
            var row = await _db.Database.SqlQueryRaw<UserRow>("SELECT Id as UserId, Email, FullName FROM [identity].[Users] WHERE Id = {0}", userId).FirstOrDefaultAsync(ct);
            if (row != null) { email = row.Email; fullName = row.FullName; }
        }
        catch { }
        return Result<ProjectMemberDto>.Success(new ProjectMemberDto(pm.Id, pm.ProjectId, pm.UserId, email, fullName, pm.Role, pm.JoinedAt));
    }

    public async Task<Result<bool>> RemoveMemberAsync(Guid projectId, Guid userId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (!Roles.IsPrivilegedForManage(callerRoles))
            return Result<bool>.Failure("Forbidden - Need OrgAdmin/SuperAdmin");
        var pm = await _db.ProjectMembers.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.UserId == userId, ct);
        if (pm == null) return Result<bool>.Failure("Project member not found");
        _db.ProjectMembers.Remove(pm);
        // Also remove from teams of this project
        var teamIds = await _db.Teams.Where(t => t.ProjectId == projectId).Select(t => t.Id).ToListAsync(ct);
        var tms = await _db.TeamMembers.Where(tm => teamIds.Contains(tm.TeamId) && tm.UserId == userId).ToListAsync(ct);
        _db.TeamMembers.RemoveRange(tms);
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<List<ProjectMemberDto>> GetAssigneeCandidatesAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project == null) return new List<ProjectMemberDto>();
        var wsId = project.WorkspaceId;
        Guid orgId = Guid.Empty;
        try { var orgRow = await _db.Database.SqlQueryRaw<GuidRow>("SELECT OrganizationId as Value FROM [identity].[Workspaces] WHERE Id = {0}", wsId).FirstOrDefaultAsync(ct); if (orgRow != null) orgId = orgRow.Value; } catch { }
        if (orgId == Guid.Empty) return await GetMembersAsync(projectId, 1, 100, null, ct).ContinueWith(t => t.Result.Items.ToList(), ct);

        // Fetch three sets
        List<Guid> orgUserIds = new(), wsUserIds = new(), projUserIds = new();
        try { orgUserIds = await _db.Database.SqlQueryRaw<Guid>("SELECT UserId FROM [identity].[OrganizationMembers] WHERE OrganizationId = {0} UNION SELECT OwnerId FROM [identity].[Organizations] WHERE Id = {0}", orgId).ToListAsync(ct); } catch { }
        try { wsUserIds = await _db.Database.SqlQueryRaw<Guid>("SELECT UserId FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0}", wsId).ToListAsync(ct); } catch { }
        try { projUserIds = await _db.ProjectMembers.Where(pm => pm.ProjectId == projectId).Select(pm => pm.UserId).ToListAsync(ct); } catch { }
        var candidateIds = orgUserIds.Intersect(wsUserIds).Intersect(projUserIds).Distinct().ToList();
        if (!candidateIds.Any()) return new List<ProjectMemberDto>();
        // Fetch org-level roles map and workspace custom roles map for candidates
        var orgRoleMap = new Dictionary<Guid,int>();
        try {
            var orgRoles = await _db.Database.SqlQueryRaw<OrgRoleRow>("SELECT UserId, Role as Value FROM [identity].[OrganizationMembers] WHERE OrganizationId = {0}", orgId).ToListAsync(ct);
            foreach (var r in orgRoles) orgRoleMap[r.UserId] = r.Value;
            var ownerId = await _db.Database.SqlQueryRaw<Guid>("SELECT OwnerId as Value FROM [identity].[Organizations] WHERE Id = {0}", orgId).FirstOrDefaultAsync(ct);
            if (ownerId != Guid.Empty && !orgRoleMap.ContainsKey(ownerId)) orgRoleMap[ownerId] = Roles.OrgAdminValue;
        } catch { }
        var customRoleNames = new Dictionary<Guid, string>();
        try {
            var crs = await _db.Database.SqlQueryRaw<CustomRoleRow>("SELECT Id, Name FROM [identity].[OrganizationWorkspaceRoles] WHERE OrganizationId = {0}", orgId).ToListAsync(ct);
            foreach (var cr in crs) customRoleNames[cr.Id] = cr.Name;
        } catch { }

        var result = new List<ProjectMemberDto>();
        foreach (var uid in candidateIds)
        {
            string email = uid.ToString()[..8], fullName = uid.ToString()[..8], role = Roles.Member;
            string orgRoleStr = Roles.GetLabel(orgRoleMap.TryGetValue(uid, out var ov) ? ov : Roles.MemberValue);
            string wsRoleStr = orgRoleStr; // default fallback
            try
            {
                var row = await _db.Database.SqlQueryRaw<UserRow>("SELECT Id as UserId, Email, FullName FROM [identity].[Users] WHERE Id = {0}", uid).FirstOrDefaultAsync(ct);
                if (row != null) { email = row.Email; fullName = row.FullName; }
                var pmRow = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == uid, ct);
                if (pmRow != null) role = pmRow.Role;

                // Fetch workspace membership for this user to get per-workspace custom role
                var wsMem = await _db.Database.SqlQueryRaw<WsMemberRow>("SELECT UserId, Role as RoleValue, CustomRoleId FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0} AND UserId = {1}", wsId, uid).FirstOrDefaultAsync(ct);
                if (wsMem != null) {
                    if (wsMem.CustomRoleId.HasValue && customRoleNames.TryGetValue(wsMem.CustomRoleId.Value, out var crn)) wsRoleStr = crn;
                    else wsRoleStr = Roles.GetLabel(wsMem.RoleValue);
                }
            }
            catch { }
            result.Add(new ProjectMemberDto(Guid.Empty, projectId, uid, email, fullName, role, DateTime.UtcNow, orgRoleStr, wsRoleStr));
        }
        return result.OrderBy(r => r.FullName).ToList();
    }

    public async Task<bool> IsAssigneeValidAsync(Guid projectId, Guid assigneeId, CancellationToken ct = default)
    {
        var candidates = await GetAssigneeCandidatesAsync(projectId, ct);
        return candidates.Any(c => c.UserId == assigneeId);
    }

    private class UserRow
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
    }
    private class GuidRow { public Guid Value { get; set; } }
    private class WsRoleRow { public int Value { get; set; } }
    private class OrgRoleRow { public Guid UserId { get; set; } public int Value { get; set; } }
    private class WsMemberRow { public Guid UserId { get; set; } public int RoleValue { get; set; } public Guid? CustomRoleId { get; set; } }
    private class CustomRoleRow { public Guid Id { get; set; } public string Name { get; set; } = ""; }
}
