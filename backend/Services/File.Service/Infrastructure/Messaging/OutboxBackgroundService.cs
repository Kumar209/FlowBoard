using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using File.Service.Infrastructure.Persistence;
using MassTransit;
using Shared.Contracts.Events;
using System.Text.Json;

namespace File.Service.Infrastructure.Messaging;

public class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceProvider _sp;
    public OutboxBackgroundService(IServiceProvider sp) => _sp = sp;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FileDbContext>();
                var publish = scope.ServiceProvider.GetService<IPublishEndpoint>();

                var msgs = await db.OutboxMessages.Where(m => m.ProcessedAt == null).OrderBy(m => m.OccurredOn).Take(10).ToListAsync(ct);
                foreach (var msg in msgs)
                {
                    try
                    {
                        if (publish != null)
                        {
                            if (msg.Type == "FileUploaded")
                            {
                                var evt = JsonSerializer.Deserialize<FileUploadedEvent>(msg.Payload);
                                if (evt != null) await publish.Publish(evt, ct);
                            }
                        }
                        msg.MarkProcessed();
                        await db.SaveChangesAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        msg.MarkFailed(ex.Message);
                        await db.SaveChangesAsync(ct);
                    }
                }
            }
            catch { }
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }
}
