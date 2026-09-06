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
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardList> BoardLists => Set<BoardList>(); // BoardColumn - UI Column (To Do etc.)
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>(); // Issue (Story/Task/Bug) - see TaskItem for hierarchy
    public DbSet<SubTask> SubTasks => Set<SubTask>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProjectEnvironment> Environments => Set<ProjectEnvironment>();

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

        // Board (Enterprise: Project → Boards as views, same Issues) + FilterJson
        b.Entity<Board>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Type).HasMaxLength(20).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.FilterJson).HasMaxLength(2000);
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => new { x.ProjectId, x.Position });
            e.Ignore(x => x.DomainEvents);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        // BoardList = Column (Enterprise: Board → Columns To Do etc., terminology Column in UI)
        b.Entity<BoardList>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.ProjectId, x.Position });
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.BoardId);
            e.Ignore(x => x.DomainEvents);
            e.HasOne(x => x.Board).WithMany().HasForeignKey(x => x.BoardId).OnDelete(DeleteBehavior.NoAction);
        });

        // Sprint (Enterprise: Project-owned, Board filters by Sprint)
        b.Entity<Sprint>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.BoardId);
            e.HasIndex(x => new { x.BoardId, x.StartDate });
            e.Ignore(x => x.DomainEvents);
            e.HasOne(x => x.Board).WithMany().HasForeignKey(x => x.BoardId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        // Team (Project → Teams)
        b.Entity<Team>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique();
            e.Ignore(x => x.DomainEvents);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TeamMember>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TeamId);
            e.HasIndex(x => new { x.TeamId, x.UserId }).IsUnique();
            e.HasOne(x => x.Team).WithMany(x => x.Members).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Cascade);
        });

        // TaskItem (Status = visual column name)
        b.Entity<TaskItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasMaxLength(5000);
            e.Property(x => x.LabelsJson).HasMaxLength(1000);
            e.Property(x => x.IssueType).HasMaxLength(20).HasDefaultValue("Task");
            e.Property(x => x.Epic).HasMaxLength(100);
            e.Property(x => x.Environment).HasMaxLength(50);
            e.Property(x => x.WatchersJson).HasMaxLength(2000);
            e.Property(x => x.LinkedIssuesJson).HasMaxLength(2000);
            e.Property(x => x.Status).HasMaxLength(100).HasDefaultValue("To Do");
            e.HasIndex(x => new { x.ListId, x.Position });
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.AssigneeId);
            e.HasIndex(x => x.ListId);
            e.HasIndex(x => x.SprintId);
            e.HasIndex(x => x.TeamId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.ParentIssueId);
            // FULLTEXT for Title search will be added via raw SQL in migration (SqlServer CONTAINS)
            e.Ignore(x => x.DomainEvents);
            e.HasOne(x => x.List).WithMany(x => x.Tasks).HasForeignKey(x => x.ListId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Project).WithMany(x => x.Tasks).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        // Environment (Deployment targets: Development, QA, Staging, Production)
        b.Entity<ProjectEnvironment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Url).HasMaxLength(500).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique();
            e.Ignore(x => x.DomainEvents);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
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
