using Microsoft.EntityFrameworkCore;
using Identity.Service.Application.Interfaces;
using Identity.Service.Domain.Entities;
using Identity.Service.Domain.Enums;

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
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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

        // Organization
        modelBuilder.Entity<Organization>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DomainEvents);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.OwnerId);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Slug).IsRequired().HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(1000);
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

        // WorkspaceMember - Composite PK, Role enum as int
        modelBuilder.Entity<WorkspaceMember>(e =>
        {
            e.HasKey(x => new { x.WorkspaceId, x.UserId });
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.WorkspaceId);
            e.Property(x => x.Role).HasConversion<int>().IsRequired();
            e.HasOne(x => x.Workspace).WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
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
    }
}
