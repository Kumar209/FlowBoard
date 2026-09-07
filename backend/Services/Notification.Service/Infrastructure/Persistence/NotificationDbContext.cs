using Microsoft.EntityFrameworkCore;
using SharedKernel;
using NotificationEntity = Notification.Service.Domain.Entities.Notification;

namespace Notification.Service.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("notification");
        b.Ignore<DomainEvent>();

        b.Entity<NotificationEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.PayloadJson).HasMaxLength(4000);
            e.Property(x => x.WorkspaceId).HasMaxLength(100);
            e.HasIndex(x => x.EventId).IsUnique();
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.TaskId);
            e.HasIndex(x => x.ActorId);
            e.HasIndex(x => x.WorkspaceId);
            e.HasIndex(x => x.IsRead);
            e.HasIndex(x => x.OccurredOn);
            e.Ignore(x => x.DomainEvents);
        });
    }
}
