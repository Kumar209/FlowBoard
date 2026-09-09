namespace Identity.Service.Application.Interfaces;

public record OrganizationActivityDto(Guid Id, Guid OrganizationId, Guid ActorUserId, string Action, string? PayloadJson, DateTime OccurredOn, string? ActorName = null);

public interface IOrganizationActivityService
{
    Task<(List<OrganizationActivityDto> Items, int Total)> GetActivitiesAsync(Guid organizationId, int page, int pageSize, Guid callerId, CancellationToken ct = default);
    Task LogAsync(Guid organizationId, Guid actorUserId, string action, string? payloadJson, CancellationToken ct = default);
}
