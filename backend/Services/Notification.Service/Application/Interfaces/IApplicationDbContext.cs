using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NotificationEntity = Notification.Service.Domain.Entities.Notification;

namespace Notification.Service.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<NotificationEntity> Notifications { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
