using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Interfaces;
using Project.Service.Domain.Entities;
using ProjectEntity = Project.Service.Domain.Entities.Project;

namespace Project.Service.Infrastructure.Persistence;

public class ProjectDbContext : DbContext, IApplicationDbContext
{
    public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options) { }

    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<BoardList> BoardLists => Set<BoardList>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<SubTask> SubTasks => Set<SubTask>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("project");
        b.Ignore<DomainEvent>();

        // Project
        b.Entity<ProjectEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Key).HasMaxLength(20).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasIndex(x => new { x.WorkspaceId, x.Key }).IsUnique();
            e.HasIndex(x => x.OwnerId);
            e.Ignore(x => x.DomainEvents);
            e.HasMany(x => x.Lists).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Tasks).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        // BoardList
        b.Entity<BoardList>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.ProjectId, x.Position });
            e.HasIndex(x => x.ProjectId);
            e.Ignore(x => x.DomainEvents);
        });

        // TaskItem
        b.Entity<TaskItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasMaxLength(5000);
            e.Property(x => x.LabelsJson).HasMaxLength(1000);
            e.HasIndex(x => new { x.ListId, x.Position });
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.AssigneeId);
            e.HasIndex(x => x.ListId);
            // FULLTEXT for Title search will be added via raw SQL in migration (SqlServer CONTAINS)
            e.Ignore(x => x.DomainEvents);
            e.HasOne(x => x.List).WithMany(x => x.Tasks).HasForeignKey(x => x.ListId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Project).WithMany(x => x.Tasks).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        // SubTask
        b.Entity<SubTask>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.HasIndex(x => x.TaskId);
            e.Ignore(x => x.DomainEvents);
            e.HasOne(x => x.Task).WithMany(x => x.SubTasks).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
        });

        // Comment
        b.Entity<Comment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Content).HasMaxLength(5000).IsRequired();
            e.HasIndex(x => x.TaskId);
            e.HasIndex(x => x.AuthorId);
            e.Ignore(x => x.DomainEvents);
            e.HasOne(x => x.Task).WithMany(x => x.Comments).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
        });

        // ActivityLog
        b.Entity<ActivityLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.PayloadJson).HasMaxLength(4000);
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.TaskId);
            e.HasIndex(x => x.ActorId);
            e.HasIndex(x => x.OccurredAt);
            e.Ignore(x => x.DomainEvents);
        });

        // OutboxMessage
        b.Entity<OutboxMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(200).IsRequired();
            e.Property(x => x.Payload).HasMaxLength(8000).IsRequired();
            e.Property(x => x.Error).HasMaxLength(2000);
            e.HasIndex(x => x.ProcessedAt);
            e.HasIndex(x => x.OccurredOn);
            e.Ignore(x => x.DomainEvents);
        });
    }
}
