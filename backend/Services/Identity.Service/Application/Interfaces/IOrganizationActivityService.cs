namespace Identity.Service.Application.Interfaces;

public record OrganizationActivityDto(Guid Id, Guid OrganizationId, Guid ActorUserId, string Action, string? PayloadJson, DateTime OccurredOn, string? ActorName = null, Guid? ProjectId = null, Guid? WorkspaceId = null, string? ProjectName = null, string? WorkspaceName = null, string? CallerEmail = null, string? CustomRoleName = null);

public interface IOrganizationActivityService
{
    Task<(List<OrganizationActivityDto> Items, int Total)> GetActivitiesAsync(Guid organizationId, int page, int pageSize, Guid callerId, bool includeProjects = false, CancellationToken ct = default);
    Task LogAsync(Guid organizationId, Guid actorUserId, string action, string? payloadJson, CancellationToken ct = default);
}
