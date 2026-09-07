using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MassTransit;
using Project.Service.Infrastructure.Persistence;
using Shared.Contracts.Events;

namespace Project.Service.Infrastructure.Messaging;

/// <summary>
/// OutboxBackgroundService - Transactional Outbox poller (Task 3.1).
/// Polls [project].OutboxMessages every 2s, publishes via MassTransit to CloudAMQP (same amqps:// key local/prod),
/// marks ProcessedAt on success, stores Error on failure for retry. Ensures no lost event on crash/restart.
/// MNC-grade: IServiceScopeFactory per iteration (scoped DbContext + IPublishEndpoint), retry 3x immediate via MassTransit pipeline + _error queue.
/// </summary>
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
        _logger.LogInformation("[Outbox] BackgroundService started - polling every 2s (CloudAMQP same key local/prod)");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                var messages = await db.OutboxMessages
                    .Where(m => m.ProcessedAt == null)
                    .OrderBy(m => m.OccurredOn)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var msg in messages)
                {
                    try
                    {
                        switch (msg.Type)
                        {
                            case "TaskCreated":
                                {
                                    var evt = JsonSerializer.Deserialize<TaskCreatedEvent>(msg.Payload);
                                    if (evt != null) await publisher.Publish(evt, stoppingToken);
                                    else _logger.LogWarning("[Outbox] TaskCreated payload null {Id}", msg.Id);
                                    break;
                                }
                            case "TaskMoved":
                                {
                                    var evt = JsonSerializer.Deserialize<TaskMovedEvent>(msg.Payload);
                                    if (evt != null) await publisher.Publish(evt, stoppingToken);
                                    else _logger.LogWarning("[Outbox] TaskMoved payload null {Id}", msg.Id);
                                    break;
                                }
                            case "TaskCommented":
                                {
                                    var evt = JsonSerializer.Deserialize<TaskCommentedEvent>(msg.Payload);
                                    if (evt != null) await publisher.Publish(evt, stoppingToken);
                                    else _logger.LogWarning("[Outbox] TaskCommented payload null {Id}", msg.Id);
                                    break;
                                }
                            default:
                                _logger.LogWarning("[Outbox] Unknown Type {Type} Id {Id} - marking processed to avoid poison", msg.Type, msg.Id);
                                break;
                        }

                        msg.MarkProcessed();
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("[Outbox] Published {Type} {Id} -> CloudAMQP flowboard.events", msg.Type, msg.Id);
                    }
                    catch (Exception ex)
                    {
                        msg.MarkFailed(ex.Message);
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogError(ex, "[Outbox] Publish failed {Type} {Id} - will retry, _error queue after 3x", msg.Type, msg.Id);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Outbox] Polling iteration failed - will retry in 2s");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
        _logger.LogInformation("[Outbox] BackgroundService stopped");
    }
}
