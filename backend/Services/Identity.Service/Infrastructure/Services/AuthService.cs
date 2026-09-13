using Microsoft.EntityFrameworkCore;
using Identity.Service.Application.DTOs;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;
using SharedKernel;

namespace Identity.Service.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtProvider _jwt;
    private readonly IRefreshTokenService _refreshService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPlatformSettingsService _platformSettings;

    public AuthService(IApplicationDbContext db, IJwtProvider jwt, IRefreshTokenService refreshService, IPasswordHasher passwordHasher, IPlatformSettingsService platformSettings)
    {
        _db = db;
        _jwt = jwt;
        _refreshService = refreshService;
        _passwordHasher = passwordHasher;
        _platformSettings = platformSettings;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(string email, string password, string fullName, string companyName, string? companyDescription, CancellationToken ct = default)
    {
        var exists = await _db.Users.AnyAsync(x => x.Email == email.ToLowerInvariant(), ct);
        if (exists) return Result<AuthResponse>.Failure("Email already registered");
        if (string.IsNullOrWhiteSpace(companyName)) return Result<AuthResponse>.Failure("Company name required");
        var hash = _passwordHasher.Hash(password);
        var user = new User(email, hash, fullName);
        _db.Users.Add(user);
        var orgSlug = companyName.ToLowerInvariant().Replace(" ", "-") + "-" + Guid.NewGuid().ToString()[..6];
        string defaultPlanIdStr;
        try { defaultPlanIdStr = await _platformSettings.GetTenantDefaultPlanIdAsync(ct); } catch { defaultPlanIdStr = "a0000000-0000-0000-0000-000000000010"; }
        var planId = Guid.TryParse(defaultPlanIdStr, out var pid) ? pid : Guid.Parse("a0000000-0000-0000-0000-000000000010");
        var planExists = await _db.SubscriptionPlans.AnyAsync(x => x.Id == planId, ct);
        if (!planExists)
        {
            var freePlan = await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Name == "Free", ct);
            planId = freePlan?.Id ?? Guid.Parse("a0000000-0000-0000-0000-000000000010");
        }
        var org = new Organization(companyName, orgSlug, user.Id, companyDescription, planId);
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync(ct);
        var orgMember = new OrganizationMember(org.Id, user.Id, 2); // OrgAdmin
        _db.OrganizationMembers.Add(orgMember);
        var workspace = new Workspace(org.Id, "General", "general-" + Guid.NewGuid().ToString()[..4]);
        _db.Workspaces.Add(workspace);
        await _db.SaveChangesAsync(ct);
        var member = new WorkspaceMember(workspace.Id, user.Id, Roles.OrgAdminValue);
        _db.WorkspaceMembers.Add(member);
        var memberships = new[] { (workspace.Id, Roles.GetLabel(Roles.OrgAdminValue)) };
        var (accessToken, accessExpires) = _jwt.GenerateAccessToken(user, memberships);
        var (rawRefresh, hashRefresh, refreshExpires) = _refreshService.GenerateRawToken();
        var refreshToken = new RefreshToken(user.Id, hashRefresh, refreshExpires);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);
        return Result<AuthResponse>.Success(new AuthResponse(user.Id, user.Email, user.FullName, accessToken, rawRefresh, accessExpires, refreshExpires));
    }

    public async Task<Result<AuthResponse>> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        // Maintenance block - superadmin can still login to disable maintenance
        var isMaintenance = false;
        try { isMaintenance = await _platformSettings.IsMaintenanceActiveAsync(ct); } catch { }
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email.ToLowerInvariant(), ct);
        if (user == null || !user.IsActive) return Result<AuthResponse>.Failure("Invalid credentials");
        if (isMaintenance && user != null && !user.IsSuperAdmin)
        {
            var m = await _platformSettings.GetMaintenanceAsync(ct);
            var retry = "60";
            if (DateTime.TryParse(m.EndAt, out var end)) retry = Math.Max(60, (int)(end.ToUniversalTime() - DateTime.UtcNow).TotalSeconds).ToString();
            return Result<AuthResponse>.Failure($"Platform under maintenance — please try again after {retry}s");
        }
        if (!_passwordHasher.Verify(password, user.PasswordHash)) return Result<AuthResponse>.Failure("Invalid credentials");

        // Enforce pending suspensions with deadline passed (grace period)
        await EnforcePendingForUserAsync(user.Id, ct);
        // Reload user after possible suspension
        user = await _db.Users.FirstOrDefaultAsync(x => x.Id == user.Id, ct);
        if (user == null || !user.IsActive) return Result<AuthResponse>.Failure("Account suspended - contact platform support");

        // Block if any of user's organizations is suspended
        var userOrgIds = await _db.OrganizationMembers.Where(m => m.UserId == user.Id).Select(m => m.OrganizationId)
            .Union(_db.Organizations.Where(o => o.OwnerId == user.Id).Select(o => o.Id))
            .Union(_db.WorkspaceMembers.Where(wm => wm.UserId == user.Id).Join(_db.Workspaces, wm => wm.WorkspaceId, w => w.Id, (wm, w) => w.OrganizationId))
            .Distinct().ToListAsync(ct);
        if (userOrgIds.Any())
        {
            var suspendedOrg = await _db.Organizations.Where(o => userOrgIds.Contains(o.Id) && !o.IsActive).AnyAsync(ct);
            if (suspendedOrg) return Result<AuthResponse>.Failure("Organization suspended - contact platform support at superadmin@flowboard.local");
        }

        var membershipsRaw = await _db.WorkspaceMembers.Where(x => x.UserId == user.Id).Select(x => new { x.WorkspaceId, x.Role }).ToListAsync(ct);
        var memberships = membershipsRaw.Select(x => new ValueTuple<Guid, string>(x.WorkspaceId, Roles.GetLabel(x.Role))).ToList();
        // Include org-level OrgAdmin as synthetic workspace membership so isOrgAdmin works
        var isOrgAdmin = await _db.OrganizationMembers.AnyAsync(m => m.UserId == user.Id && m.Role == Roles.OrgAdminValue, ct) || await _db.Organizations.AnyAsync(o => o.OwnerId == user.Id, ct);
        if (isOrgAdmin && !memberships.Any(m => m.Item2 == Roles.OrgAdmin))
        {
            var wsId = await _db.Workspaces.Where(w => userOrgIds.Contains(w.OrganizationId)).Select(w => w.Id).FirstOrDefaultAsync(ct);
            memberships.Add((wsId, Roles.OrgAdmin));
        }
        // MNC-grade: global SuperAdmin via Users.IsSuperAdmin flag, not workspace membership
        if (user.IsSuperAdmin && !memberships.Any(m => m.Item2 == Roles.SuperAdmin))
            memberships.Add((Guid.Empty, Roles.SuperAdmin));
        var (accessToken, accessExpires) = _jwt.GenerateAccessToken(user, memberships);
        var (rawRefresh, hashRefresh, refreshExpires) = _refreshService.GenerateRawToken();
        var refreshToken = new RefreshToken(user.Id, hashRefresh, refreshExpires);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);
        return Result<AuthResponse>.Success(new AuthResponse(user.Id, user.Email, user.FullName, accessToken, rawRefresh, accessExpires, refreshExpires));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var isReuse = await _refreshService.IsReuseDetectedAsync(refreshToken);
        if (isReuse)
        {
            var hash = _refreshService.HashToken(refreshToken);
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
            if (token != null) await _refreshService.RevokeFamilyAsync(token.UserId);
            return Result<AuthResponse>.Failure("Refresh token reuse detected - all tokens revoked");
        }
        var (newToken, rawNew) = await _refreshService.RotateAsync(refreshToken);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == newToken.UserId, ct);
        if (user == null) return Result<AuthResponse>.Failure("User not found");
        var membershipsRaw2 = await _db.WorkspaceMembers.Where(x => x.UserId == user.Id).Select(x => new { x.WorkspaceId, x.Role }).ToListAsync(ct);
        var memberships = membershipsRaw2.Select(x => new ValueTuple<Guid, string>(x.WorkspaceId, Roles.GetLabel(x.Role))).ToList();
        var isOrgAdmin2 = await _db.OrganizationMembers.AnyAsync(m => m.UserId == user.Id && m.Role == Roles.OrgAdminValue, ct) || await _db.Organizations.AnyAsync(o => o.OwnerId == user.Id, ct);
        if (isOrgAdmin2 && !memberships.Any(m => m.Item2 == Roles.OrgAdmin))
        {
            var wsId2 = await _db.Workspaces.Where(w => _db.Organizations.Where(o => o.OwnerId == user.Id || _db.OrganizationMembers.Any(m => m.OrganizationId == o.Id && m.UserId == user.Id)).Select(o => o.Id).Contains(w.OrganizationId)).Select(w => w.Id).FirstOrDefaultAsync(ct);
            memberships.Add((wsId2, Roles.OrgAdmin));
        }
        if (user.IsSuperAdmin && !memberships.Any(m => m.Item2 == Roles.SuperAdmin))
            memberships.Add((Guid.Empty, Roles.SuperAdmin));
        var (accessToken, accessExpires) = _jwt.GenerateAccessToken(user, memberships);
        return Result<AuthResponse>.Success(new AuthResponse(user.Id, user.Email, user.FullName, accessToken, rawNew, accessExpires, newToken.ExpiresAt));
    }

    public async Task<Result<(UserDto User, List<(Guid WorkspaceId, string Role)> Memberships)>> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user == null) return Result<(UserDto, List<(Guid, string)>)>.Failure("User not found");
        var membershipsRaw = await _db.WorkspaceMembers.Where(x => x.UserId == userId).Select(x => new { x.WorkspaceId, x.Role }).ToListAsync(ct);
        var memberships = membershipsRaw.Select(x => new ValueTuple<Guid, string>(x.WorkspaceId, Roles.GetLabel(x.Role))).ToList();
        var isOrgAdminMe = await _db.OrganizationMembers.AnyAsync(m => m.UserId == userId && m.Role == Roles.OrgAdminValue, ct) || await _db.Organizations.AnyAsync(o => o.OwnerId == userId, ct);
        if (isOrgAdminMe && !memberships.Any(m => m.Item2 == Roles.OrgAdmin))
        {
            var wsIdMe = await _db.Workspaces.Where(w => _db.Organizations.Where(o => o.OwnerId == userId || _db.OrganizationMembers.Any(m => m.OrganizationId == o.Id && m.UserId == userId)).Select(o => o.Id).Contains(w.OrganizationId)).Select(w => w.Id).FirstOrDefaultAsync(ct);
            memberships.Add((wsIdMe, Roles.OrgAdmin));
        }
        if (user.IsSuperAdmin && !memberships.Any(m => m.Item2 == Roles.SuperAdmin))
            memberships.Add((Guid.Empty, Roles.SuperAdmin));
        var userResponse = new UserDto(user.Id, user.Email, user.FullName, user.AvatarUrl);
        return Result<(UserDto, List<(Guid, string)>)>.Success((userResponse, memberships));
    }

    private async Task EnforcePendingForUserAsync(Guid userId, CancellationToken ct)
    {
        var pendings = await _db.PendingUserSuspensions.Where(p => p.UserId == userId && p.Status == "Pending" && p.DeadlineAt <= DateTime.UtcNow).ToListAsync(ct);
        if (!pendings.Any()) return;
        foreach (var p in pendings)
        {
            var hasOtherAdmin = await _db.OrganizationMembers.AnyAsync(m => m.OrganizationId == p.OrganizationId && m.Role == 2 && m.UserId != userId, ct);
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == p.OrganizationId, ct);
            if (org != null && org.OwnerId != userId) hasOtherAdmin = true;
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user != null) user.Deactivate();
            p.Execute();
            if (!hasOtherAdmin && org != null && org.IsActive)
            {
                org.Deactivate();
                var notice = new PlatformNotice(p.OrganizationId, $"Organization suspended due to orgadmin {user?.FullName} suspension with no replacement.", "Suspension", p.CreatedBy);
                _db.PlatformNotices.Add(notice);
            }
            var warns = await _db.PlatformNotices.Where(n => n.OrganizationId == p.OrganizationId && n.IsActive && n.Type == "SuspensionWarning").ToListAsync(ct);
            foreach (var w in warns) w.Dismiss();
        }
        await _db.SaveChangesAsync(ct);
    }
}
