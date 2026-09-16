using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Notification.Service.Application.Interfaces;
using Notification.Service.Hubs;
using Shared.Contracts.Events;

namespace Notification.Service.Consumers;

public class ProjectMemberAddedConsumer : IConsumer<ProjectMemberAddedEvent>
{
    private readonly INotificationService _service;
    private readonly IHubContext<BoardHub> _hub;
    private readonly ILogger<ProjectMemberAddedConsumer> _logger;
    public ProjectMemberAddedConsumer(INotificationService service, IHubContext<BoardHub> hub, ILogger<ProjectMemberAddedConsumer> logger) { _service = service; _hub = hub; _logger = logger; }
    public async Task Consume(ConsumeContext<ProjectMemberAddedEvent> ctx)
    {
        var msg = ctx.Message;
        var result = await _service.PersistProjectMemberAddedAsync(msg.EventId, msg.ProjectId, msg.WorkspaceId, msg.UserId, msg.ActorId, msg.RecipientUserIds ?? new List<Guid>(), msg.OccurredOnUtc);
        if (!result.IsSuccess) _logger.LogWarning("[Consumer] ProjectMemberAdded persist failed {EventId} {Err}", msg.EventId, result.Error);
        try
        {
            foreach (var uid in msg.RecipientUserIds ?? new List<Guid>())
                await _hub.Clients.Group($"user:{uid}").SendAsync("notification", new { id = msg.EventId, eventId = msg.EventId, action = "ProjectMemberAdded", projectId = msg.ProjectId });
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[Consumer] SignalR publish failed for projectMemberAdded"); }
    }
}
