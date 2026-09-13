using Microsoft.EntityFrameworkCore;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;
using SharedKernel;

namespace Identity.Service.Infrastructure.Persistence;

// Single DB flowboard with schema [identity] - 4 schemas total (identity, project, file, notification)
// Each service has its own DbContext with HasDefaultSchema, same ConnectionStrings Default = Server=localhost;Database=flowboard
// Enterprise: Implements IApplicationDbContext (defined in Application) - DIP, testable via mock
public class IdentityDbContext : DbContext, IApplicationDbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<OrganizationWorkspaceRole> OrganizationWorkspaceRoles => Set<OrganizationWorkspaceRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<OrganizationActivity> OrganizationActivities => Set<OrganizationActivity>();
    public DbSet<SubscriptionPlanEntity> SubscriptionPlans => Set<SubscriptionPlanEntity>();
    public DbSet<PendingUserSuspension> PendingUserSuspensions => Set<PendingUserSuspension>();
    public DbSet<PlatformNotice> PlatformNotices => Set<PlatformNotice>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintReply> ComplaintReplies => Set<ComplaintReply>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<OrganizationFeatureFlag> OrganizationFeatureFlags => Set<OrganizationFeatureFlag>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.Ignore<SharedKernel.DomainEvent>();

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.CreatedAt);
            e.Property(x => x.Email).IsRequired().HasMaxLength(256);
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(512);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            e.Property(x => x.AvatarUrl).HasMaxLength(512);
        });

        // SubscriptionPlans - lookup table for billing (MNC enum+table hybrid)
        modelBuilder.Entity<SubscriptionPlanEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).IsRequired().HasMaxLength(50);
            e.Property(x => x.FeaturesJson).HasMaxLength(2000);
        });

        // Organization
        modelBuilder.Entity<Organization>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.OwnerId);
            e.HasIndex(x => x.SubscriptionPlanId);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Slug).IsRequired().HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasOne(x => x.SubscriptionPlan).WithMany().HasForeignKey(x => x.SubscriptionPlanId).OnDelete(DeleteBehavior.Restrict);
        });

        // Workspace
        modelBuilder.Entity<Workspace>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => x.OrganizationId);
            e.HasIndex(x => x.Slug);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Slug).IsRequired().HasMaxLength(100);
            e.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        // WorkspaceMember - Composite PK, Role enum as int + CustomRoleId FK
        modelBuilder.Entity<WorkspaceMember>(e =>
        {
            e.HasKey(x => new { x.WorkspaceId, x.UserId });
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.WorkspaceId);
            e.HasIndex(x => x.CustomRoleId);
            e.Property(x => x.Role).IsRequired();
            e.HasOne(x => x.Workspace).WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CustomRole).WithMany().HasForeignKey(x => x.CustomRoleId).OnDelete(DeleteBehavior.NoAction);
        });

        // OrganizationMember - explicit org-level membership (Member/OrgAdmin/Client)
        modelBuilder.Entity<OrganizationMember>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
            e.HasIndex(x => x.OrganizationId);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Role).IsRequired();
            e.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // OrganizationWorkspaceRole - custom per-org workspace roles
        modelBuilder.Entity<OrganizationWorkspaceRole>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();
            e.HasIndex(x => x.OrganizationId);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        // Permission - fixed catalog
        modelBuilder.Entity<Permission>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => x.Key).IsUnique();
            e.HasIndex(x => x.Group);
            e.Property(x => x.Key).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Group).IsRequired().HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(500);
        });

        // RolePermission - join
        modelBuilder.Entity<RolePermission>(e =>
        {
            e.HasKey(x => new { x.RoleId, x.PermissionId });
            e.HasIndex(x => x.RoleId);
            e.HasIndex(x => x.PermissionId);
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Permission).WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        // OrganizationActivity - org-level audit
        modelBuilder.Entity<OrganizationActivity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => x.OrganizationId);
            e.HasIndex(x => x.ActorUserId);
            e.HasIndex(x => x.OccurredOn);
            e.Property(x => x.Action).IsRequired().HasMaxLength(100);
            e.Property(x => x.PayloadJson).HasMaxLength(4000);
            e.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        // PendingUserSuspension - platform grace period
        modelBuilder.Entity<PendingUserSuspension>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.OrganizationId);
            e.HasIndex(x => x.Status);
            e.Property(x => x.Reason).IsRequired().HasMaxLength(500);
            e.Property(x => x.Message).IsRequired().HasMaxLength(2000);
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        // PlatformNotice - banner per org
        modelBuilder.Entity<PlatformNotice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => x.OrganizationId);
            e.HasIndex(x => x.IsActive);
            e.Property(x => x.Message).IsRequired().HasMaxLength(2000);
            e.Property(x => x.Type).IsRequired().HasMaxLength(30);
            e.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        // Complaint - org to platform
        modelBuilder.Entity<Complaint>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => x.OrganizationId);
            e.HasIndex(x => x.CreatedByUserId);
            e.HasIndex(x => x.Status);
            e.Property(x => x.Subject).IsRequired().HasMaxLength(200);
            e.Property(x => x.Message).IsRequired().HasMaxLength(2000);
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        });

        // ComplaintReply - threaded
        modelBuilder.Entity<ComplaintReply>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => x.ComplaintId);
            e.HasIndex(x => x.AuthorUserId);
            e.Property(x => x.Message).IsRequired().HasMaxLength(2000);
            e.HasOne(x => x.Complaint).WithMany().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.NoAction);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.TokenHash);
            e.Property(x => x.TokenHash).IsRequired().HasMaxLength(512);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // FeatureFlag - platform global flags
        modelBuilder.Entity<FeatureFlag>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Key).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
        });

        // OrganizationFeatureFlag - per-org override
        modelBuilder.Entity<OrganizationFeatureFlag>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => new { x.OrganizationId, x.FlagKey }).IsUnique();
            e.HasIndex(x => x.OrganizationId);
            e.Property(x => x.FlagKey).IsRequired().HasMaxLength(50);
            e.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        // PlatformSetting - global platform config (6 rows: platform/security/tenantDefaults/ai/rateLimits/maintenance)
        modelBuilder.Entity<PlatformSetting>(e =>
        {
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).IsRequired().HasMaxLength(50);
            e.Property(x => x.ValueJson).IsRequired().HasMaxLength(4000);
            e.HasIndex(x => x.Key).IsUnique();
        });
    }
}
