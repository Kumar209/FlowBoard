using Microsoft.EntityFrameworkCore;
using File.Service.Application.Interfaces;
using File.Service.Domain.Entities;
using SharedKernel;

namespace File.Service.Infrastructure.Persistence;

public class FileDbContext : DbContext, IApplicationDbContext
{
    public FileDbContext(DbContextOptions<FileDbContext> options) : base(options) { }

    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("file");
        b.Ignore<DomainEvent>();

        b.Entity<Attachment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(300).IsRequired();
            e.Property(x => x.Url).HasMaxLength(1000).IsRequired();
            e.Property(x => x.PublicId).HasMaxLength(500).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.TaskId);
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.UploaderId);
            e.HasIndex(x => x.WorkspaceId);
            e.HasIndex(x => x.CreatedAt);
            e.Ignore(x => x.DomainEvents);
        });

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
