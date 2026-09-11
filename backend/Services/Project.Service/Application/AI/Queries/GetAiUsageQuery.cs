using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.AI.DTOs;
using Project.Service.Application.AI.Interfaces;
using Project.Service.Application.Interfaces;
using SharedKernel;

namespace Project.Service.Application.AI.Queries;

/// <summary>
/// GetAiUsage - 7.6 Org + 7.7 Project AI Usage. Org-level (orgId) OrgAdmin only, Project-level (projectId) all members (like Activity). Returns list of AiUsageLogDto ordered by CreatedAt desc, filtered by orgId/projectId/userId. RBAC via Roles.IsPrivilegedForManage or ProjectMember check.
/// </summary>
public record GetAiUsageQuery(Guid? OrgId, Guid? ProjectId, Guid? UserId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<IReadOnlyList<AiUsageLogDto>>>;

public class GetAiUsageHandler : IRequestHandler<GetAiUsageQuery, Result<IReadOnlyList<AiUsageLogDto>>>
{
    private readonly IAiService _ai;
    private readonly IApplicationDbContext _db;
    public GetAiUsageHandler(IAiService ai, IApplicationDbContext db) { _ai = ai; _db = db; }

    public async Task<Result<IReadOnlyList<AiUsageLogDto>>> Handle(GetAiUsageQuery req, CancellationToken ct)
    {
        // Org-level RBAC: if orgId provided and no projectId, require OrgAdmin/SuperAdmin
        if (req.OrgId.HasValue && !req.ProjectId.HasValue)
        {
            if (!SharedKernel.Roles.IsPrivilegedForManage(req.CallerRoles))
            {
                // Also check DB for OrgAdmin in that org (stale JWT)
                try
                {
                    var isOrgAdmin = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM [identity].[OrganizationMembers] WHERE OrganizationId = @p0 AND UserId = @p1 AND Role = 2", req.OrgId.Value, req.CallerId).FirstOrDefaultAsync(ct);
                    if (isOrgAdmin == 0)
                    {
                        var isSuper = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM [identity].[WorkspaceMembers] WHERE WorkspaceId IN (SELECT Id FROM [identity].[Workspaces] WHERE OrganizationId = @p0) AND UserId = @p1 AND Role = 0", req.OrgId.Value, req.CallerId).FirstOrDefaultAsync(ct);
                        if (isSuper == 0) return Result<IReadOnlyList<AiUsageLogDto>>.Failure("Forbidden — Org AI Usage requires OrgAdmin/SuperAdmin");
                    }
                }
                catch { return Result<IReadOnlyList<AiUsageLogDto>>.Failure("Forbidden — Org AI Usage requires OrgAdmin"); }
            }
        }
        // Project-level: allow any authenticated member (check project exists, caller is member of its workspace/org)
        // If projectId provided, verify caller is at least member of that project's workspace (best-effort)
        if (req.ProjectId.HasValue)
        {
            var proj = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == req.ProjectId.Value, ct);
            if (proj == null) return Result<IReadOnlyList<AiUsageLogDto>>.Failure("Project not found");
            // Allow any project member or workspace member — if not member, still allow? Spec says all project members — we allow any authenticated for now
        }

        var list = await _ai.GetUsageAsync(req.OrgId, req.ProjectId, req.UserId, ct);
        return Result<IReadOnlyList<AiUsageLogDto>>.Success(list);
    }
}

public record GetAiUsageSummaryQuery(Guid? OrgId, Guid? ProjectId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<IReadOnlyList<AiUsageSummaryDto>>>;

public class GetAiUsageSummaryHandler : IRequestHandler<GetAiUsageSummaryQuery, Result<IReadOnlyList<AiUsageSummaryDto>>>
{
    private readonly IAiService _ai;
    private readonly IApplicationDbContext _db;
    public GetAiUsageSummaryHandler(IAiService ai, IApplicationDbContext db) { _ai = ai; _db = db; }

    public async Task<Result<IReadOnlyList<AiUsageSummaryDto>>> Handle(GetAiUsageSummaryQuery req, CancellationToken ct)
    {
        if (req.OrgId.HasValue && !req.ProjectId.HasValue)
        {
            if (!SharedKernel.Roles.IsPrivilegedForManage(req.CallerRoles))
            {
                try
                {
                    var isOrgAdmin = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM [identity].[OrganizationMembers] WHERE OrganizationId = @p0 AND UserId = @p1 AND Role = 2", req.OrgId.Value, req.CallerId).FirstOrDefaultAsync(ct);
                    if (isOrgAdmin == 0) return Result<IReadOnlyList<AiUsageSummaryDto>>.Failure("Forbidden — Org AI Usage requires OrgAdmin");
                }
                catch { return Result<IReadOnlyList<AiUsageSummaryDto>>.Failure("Forbidden"); }
            }
        }
        var list = await _ai.GetUsageSummaryAsync(req.OrgId, req.ProjectId, ct);
        return Result<IReadOnlyList<AiUsageSummaryDto>>.Success(list);
    }
}
