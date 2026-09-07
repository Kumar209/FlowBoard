using FluentValidation;
using MediatR;
using Notification.Service.Application.Interfaces;
using SharedKernel;

namespace Notification.Service.Application.Commands;

public record MarkNotificationReadCommand(Guid NotificationId, Guid RecipientUserId) : IRequest<Result>;

public class MarkNotificationReadValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadValidator()
    {
        RuleFor(x => x.NotificationId).NotEmpty();
        RuleFor(x => x.RecipientUserId).NotEmpty();
    }
}

public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly INotificationService _service;
    public MarkNotificationReadHandler(INotificationService service) => _service = service;
    public Task<Result> Handle(MarkNotificationReadCommand req, CancellationToken ct)
        => _service.MarkAsReadAsync(req.NotificationId, req.RecipientUserId, ct);
}

public record MarkAllNotificationsReadCommand(Guid RecipientUserId) : IRequest<Result<int>>;

public class MarkAllNotificationsReadValidator : AbstractValidator<MarkAllNotificationsReadCommand>
{
    public MarkAllNotificationsReadValidator() => RuleFor(x => x.RecipientUserId).NotEmpty();
}

public class MarkAllNotificationsReadHandler : IRequestHandler<MarkAllNotificationsReadCommand, Result<int>>
{
    private readonly INotificationService _service;
    public MarkAllNotificationsReadHandler(INotificationService service) => _service = service;
    public Task<Result<int>> Handle(MarkAllNotificationsReadCommand req, CancellationToken ct)
        => _service.MarkAllAsReadAsync(req.RecipientUserId, ct);
}
