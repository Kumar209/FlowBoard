using MediatR;
using Notification.Service.Application.DTOs;
using Notification.Service.Application.Interfaces;

namespace Notification.Service.Application.Queries;

public record GetNotificationsQuery(Guid RecipientUserId, int Page = 1, int PageSize = 20, bool? UnreadOnly = null) : IRequest<PaginatedNotificationsResult>;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, PaginatedNotificationsResult>
{
    private readonly INotificationService _service;
    public GetNotificationsQueryHandler(INotificationService service) => _service = service;
    public Task<PaginatedNotificationsResult> Handle(GetNotificationsQuery req, CancellationToken ct)
        => _service.GetNotificationsAsync(req.RecipientUserId, req.Page, req.PageSize, req.UnreadOnly, ct);
}
