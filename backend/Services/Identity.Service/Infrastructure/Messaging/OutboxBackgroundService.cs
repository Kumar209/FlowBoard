using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MassTransit;
using Identity.Service.Infrastructure.Persistence;
using Shared.Contracts.Events;

namespace Identity.Service.Infrastructure.Messaging;

public class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxBackgroundService> _logger;

    public OutboxBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OutboxBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Identity Outbox] BackgroundService started - polling every 2s (CloudAMQP same key local/prod)");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                var messages = await db.OutboxMessages
                    .Where(m => m.ProcessedAt == null)
                    .OrderBy(m => m.OccurredOn)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                foreach (var msg in messages)
                {
                    try
                    {
                        switch (msg.Type)
                        {
                            case "ComplaintCreated":
                                {
                                    var evt = JsonSerializer.Deserialize<ComplaintCreatedEvent>(msg.Payload, jsonOpts);
                                    if (evt != null) await publisher.Publish(evt, stoppingToken);
                                    else _logger.LogWarning("[Identity Outbox] ComplaintCreated payload null {Id}", msg.Id);
                                    break;
                                }
                            default:
                                _logger.LogWarning("[Identity Outbox] Unknown Type {Type} Id {Id} - marking processed to avoid poison", msg.Type, msg.Id);
                                break;
                        }

                        msg.MarkProcessed();
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("[Identity Outbox] Published {Type} {Id} -> CloudAMQP flowboard.events", msg.Type, msg.Id);
                    }
                    catch (Exception ex)
                    {
                        msg.MarkFailed(ex.Message);
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogError(ex, "[Identity Outbox] Publish failed {Type} {Id} - will retry, _error queue after 3x", msg.Type, msg.Id);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Identity Outbox] Polling iteration failed - will retry in 2s");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
        _logger.LogInformation("[Identity Outbox] BackgroundService stopped");
    }
}
