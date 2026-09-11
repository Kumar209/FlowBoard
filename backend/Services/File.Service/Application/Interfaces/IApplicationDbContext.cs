using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using File.Service.Domain.Entities;

namespace File.Service.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Attachment> Attachments { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
