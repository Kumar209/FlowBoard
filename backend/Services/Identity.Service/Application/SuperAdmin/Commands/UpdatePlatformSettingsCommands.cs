using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Identity.Service.Application.Interfaces;
using Identity.Service.Application.SuperAdmin.DTOs;

namespace Identity.Service.Application.SuperAdmin.Commands;

public record UpdateGeneralSettingsCommand(Guid CallerId, string[] CallerRoles, GeneralSettingsDto Dto) : IRequest<Result<GeneralSettingsDto>>;
public record UpdateSecuritySettingsCommand(Guid CallerId, string[] CallerRoles, SecuritySettingsDto Dto) : IRequest<Result<SecuritySettingsDto>>;
public record UpdateTenantDefaultsCommand(Guid CallerId, string[] CallerRoles, TenantDefaultsDto Dto) : IRequest<Result<TenantDefaultsDto>>;
public record UpdateAiSettingsCommand(Guid CallerId, string[] CallerRoles, AiSettingsDto Dto) : IRequest<Result<AiSettingsDto>>;
public record UpdateRateLimitsCommand(Guid CallerId, string[] CallerRoles, RateLimitsSettingsDto Dto) : IRequest<Result<RateLimitsSettingsDto>>;
public record UpdateMaintenanceCommand(Guid CallerId, string[] CallerRoles, MaintenanceSettingsDto Dto) : IRequest<Result<MaintenanceSettingsDto>>;

public class UpdateGeneralSettingsValidator : AbstractValidator<UpdateGeneralSettingsCommand>
{
    public UpdateGeneralSettingsValidator()
    {
        RuleFor(x => x.Dto.PlatformName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dto.SupportEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Dto.Language).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Dto.Timezone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Dto.LogoUrl).MaximumLength(512);
    }
}
public class UpdateSecuritySettingsValidator : AbstractValidator<UpdateSecuritySettingsCommand>
{
    public UpdateSecuritySettingsValidator()
    {
        RuleFor(x => x.Dto.SessionTimeout).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Dto.PasswordPolicy).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Dto.MaxLoginAttempts).InclusiveBetween(3, 20);
        RuleFor(x => x.Dto.LockoutMinutes).InclusiveBetween(5, 120);
    }
}
public class UpdateTenantDefaultsValidator : AbstractValidator<UpdateTenantDefaultsCommand>
{
    public UpdateTenantDefaultsValidator()
    {
        RuleFor(x => x.Dto.DefaultPlanId).NotEmpty().Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid plan id");
    }
}
public class UpdateAiSettingsValidator : AbstractValidator<UpdateAiSettingsCommand>
{
    public UpdateAiSettingsValidator()
    {
        RuleFor(x => x.Dto.Providers).NotEmpty();
        RuleForEach(x => x.Dto.Providers).ChildRules(p =>
        {
            p.RuleFor(v => v.Provider).NotEmpty();
            p.RuleFor(v => v.TimeoutMs).InclusiveBetween(5000, 120000);
            p.RuleFor(v => v.MaxRetries).InclusiveBetween(0, 5);
        });
    }
}
public class UpdateRateLimitsValidator : AbstractValidator<UpdateRateLimitsCommand>
{
    public UpdateRateLimitsValidator()
    {
        RuleFor(x => x.Dto.AuthRequestsPerMinute).InclusiveBetween(10, 1000);
        RuleFor(x => x.Dto.ApiRequestsPerMinute).InclusiveBetween(50, 10000);
        RuleFor(x => x.Dto.AiRequestsPerMinute).InclusiveBetween(1, 100);
        RuleFor(x => x.Dto.FileRequestsPerMinute).InclusiveBetween(10, 500);
        RuleFor(x => x.Dto.NotificationRequestsPerMinute).InclusiveBetween(10, 1000);
    }
}
public class UpdateMaintenanceValidator : AbstractValidator<UpdateMaintenanceCommand>
{
    public UpdateMaintenanceValidator()
    {
        RuleFor(x => x.Dto.Announcement).MaximumLength(500);
        RuleFor(x => x.Dto.ScheduledAt).Must(s => s == null || DateTime.TryParse(s, out _)).WithMessage("Invalid scheduledAt");
        RuleFor(x => x.Dto.EndAt).Must(s => s == null || DateTime.TryParse(s, out _)).WithMessage("Invalid endAt");
    }
}

public class UpdateGeneralHandler : IRequestHandler<UpdateGeneralSettingsCommand, Result<GeneralSettingsDto>>
{
    private readonly IPlatformSettingsService _svc;
    public UpdateGeneralHandler(IPlatformSettingsService svc) => _svc = svc;
    public async Task<Result<GeneralSettingsDto>> Handle(UpdateGeneralSettingsCommand r, CancellationToken ct)
    {
        if (!Roles.IsSuperAdmin(r.CallerRoles)) return Result<GeneralSettingsDto>.Failure("Forbidden - SuperAdmin only");
        await _svc.SetGeneralAsync(r.Dto, r.CallerId, ct);
        return Result<GeneralSettingsDto>.Success(r.Dto);
    }
}
public class UpdateSecurityHandler : IRequestHandler<UpdateSecuritySettingsCommand, Result<SecuritySettingsDto>>
{
    private readonly IPlatformSettingsService _svc;
    public UpdateSecurityHandler(IPlatformSettingsService svc) => _svc = svc;
    public async Task<Result<SecuritySettingsDto>> Handle(UpdateSecuritySettingsCommand r, CancellationToken ct)
    {
        if (!Roles.IsSuperAdmin(r.CallerRoles)) return Result<SecuritySettingsDto>.Failure("Forbidden - SuperAdmin only");
        await _svc.SetSecurityAsync(r.Dto, r.CallerId, ct);
        return Result<SecuritySettingsDto>.Success(r.Dto);
    }
}
public class UpdateTenantDefaultsHandler : IRequestHandler<UpdateTenantDefaultsCommand, Result<TenantDefaultsDto>>
{
    private readonly IPlatformSettingsService _svc;
    public UpdateTenantDefaultsHandler(IPlatformSettingsService svc) => _svc = svc;
    public async Task<Result<TenantDefaultsDto>> Handle(UpdateTenantDefaultsCommand r, CancellationToken ct)
    {
        if (!Roles.IsSuperAdmin(r.CallerRoles)) return Result<TenantDefaultsDto>.Failure("Forbidden - SuperAdmin only");
        await _svc.SetTenantDefaultsAsync(r.Dto.DefaultPlanId, r.CallerId, ct);
        return Result<TenantDefaultsDto>.Success(r.Dto);
    }
}
public class UpdateAiHandler : IRequestHandler<UpdateAiSettingsCommand, Result<AiSettingsDto>>
{
    private readonly IPlatformSettingsService _svc;
    public UpdateAiHandler(IPlatformSettingsService svc) => _svc = svc;
    public async Task<Result<AiSettingsDto>> Handle(UpdateAiSettingsCommand r, CancellationToken ct)
    {
        if (!Roles.IsSuperAdmin(r.CallerRoles)) return Result<AiSettingsDto>.Failure("Forbidden - SuperAdmin only");
        await _svc.SetAiAsync(r.Dto, r.CallerId, ct);
        return Result<AiSettingsDto>.Success(r.Dto);
    }
}
public class UpdateRateLimitsHandler : IRequestHandler<UpdateRateLimitsCommand, Result<RateLimitsSettingsDto>>
{
    private readonly IPlatformSettingsService _svc;
    public UpdateRateLimitsHandler(IPlatformSettingsService svc) => _svc = svc;
    public async Task<Result<RateLimitsSettingsDto>> Handle(UpdateRateLimitsCommand r, CancellationToken ct)
    {
        if (!Roles.IsSuperAdmin(r.CallerRoles)) return Result<RateLimitsSettingsDto>.Failure("Forbidden - SuperAdmin only");
        await _svc.SetRateLimitsAsync(r.Dto, r.CallerId, ct);
        return Result<RateLimitsSettingsDto>.Success(r.Dto);
    }
}
public class UpdateMaintenanceHandler : IRequestHandler<UpdateMaintenanceCommand, Result<MaintenanceSettingsDto>>
{
    private readonly IPlatformSettingsService _svc;
    private readonly IApplicationDbContext _db;
    public UpdateMaintenanceHandler(IPlatformSettingsService svc, IApplicationDbContext db) { _svc = svc; _db = db; }
    public async Task<Result<MaintenanceSettingsDto>> Handle(UpdateMaintenanceCommand r, CancellationToken ct)
    {
        if (!Roles.IsSuperAdmin(r.CallerRoles)) return Result<MaintenanceSettingsDto>.Failure("Forbidden - SuperAdmin only");
        var prev = await _svc.GetMaintenanceAsync(ct);
        await _svc.SetMaintenanceAsync(r.Dto, r.CallerId, ct);
        // If toggling to active/scheduled, create PlatformNotices for all orgs
        if (r.Dto.MaintenanceMode && !prev.MaintenanceMode)
        {
            var orgs = await _db.Organizations.Select(o => o.Id).ToListAsync(ct);
            foreach (var orgId in orgs)
            {
                var msg = string.IsNullOrWhiteSpace(r.Dto.Announcement) ? $"Platform maintenance scheduled from {r.Dto.ScheduledAt} to {r.Dto.EndAt}" : r.Dto.Announcement!;
                _db.PlatformNotices.Add(new Domain.Entities.PlatformNotice(orgId, msg, "Maintenance", r.CallerId));
            }
            await _db.SaveChangesAsync(ct);
        }
        else if (!r.Dto.MaintenanceMode && prev.MaintenanceMode)
        {
            // Dismiss maintenance notices
            var notices = await _db.PlatformNotices.Where(n => n.Type == "Maintenance" && n.IsActive).ToListAsync(ct);
            foreach (var n in notices) n.Dismiss();
            await _db.SaveChangesAsync(ct);
        }
        return Result<MaintenanceSettingsDto>.Success(r.Dto);
    }
}
