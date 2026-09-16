using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Notification.Service.Application.Interfaces;
using Notification.Service.Hubs;
using Shared.Contracts.Events;

namespace Notification.Service.Consumers;

public class ComplaintCreatedConsumer : IConsumer<ComplaintCreatedEvent>
{
    private readonly INotificationService _service;
    private readonly IHubContext<BoardHub> _hub;
    private readonly ILogger<ComplaintCreatedConsumer> _logger;
    public ComplaintCreatedConsumer(INotificationService service, IHubContext<BoardHub> hub, ILogger<ComplaintCreatedConsumer> logger) { _service = service; _hub = hub; _logger = logger; }
    public async Task Consume(ConsumeContext<ComplaintCreatedEvent> ctx)
    {
        var msg = ctx.Message;
        var result = await _service.PersistComplaintCreatedAsync(msg.EventId, msg.OrganizationId, msg.ComplaintId, msg.Subject, msg.ActorId, msg.RecipientUserIds ?? new List<Guid>(), msg.OccurredOnUtc);
        if (!result.IsSuccess) _logger.LogWarning("[Consumer] ComplaintCreated persist failed {EventId} {Err}", msg.EventId, result.Error);
        try
        {
            foreach (var uid in msg.RecipientUserIds ?? new List<Guid>())
                await _hub.Clients.Group($"user:{uid}").SendAsync("notification", new { id = msg.EventId, eventId = msg.EventId, action = "ComplaintCreated", organizationId = msg.OrganizationId, complaintId = msg.ComplaintId });
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[Consumer] SignalR publish failed for complaintCreated"); }
    }
}
