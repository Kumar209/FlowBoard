using Microsoft.EntityFrameworkCore;
using Notification.Service.Application.Interfaces;
using SharedKernel;
using NotificationEntity = Notification.Service.Domain.Entities.Notification;

namespace Notification.Service.Infrastructure.Persistence;

public class NotificationDbContext : DbContext, IApplicationDbContext
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
            e.HasIndex(x => new { x.RecipientUserId, x.EventId }).IsUnique();
            e.HasIndex(x => x.RecipientUserId);
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.TaskId);
            e.HasIndex(x => x.ActorUserId);
            e.HasIndex(x => x.WorkspaceId);
            e.HasIndex(x => x.IsRead);
            e.HasIndex(x => x.OccurredOnUtc);
            e.Ignore(x => x.DomainEvents);
        });
    }
}
