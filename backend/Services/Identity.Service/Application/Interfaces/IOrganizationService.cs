namespace Identity.Service.Application.Interfaces;

public interface IOrganizationService
{
    Task<List<OrganizationDto>> GetMyOrganizationsAsync(Guid userId, CancellationToken ct = default);
    Task<OrganizationDto> CreateOrganizationAsync(string name, string? description, Guid userId, CancellationToken ct = default);
    Task<OrganizationDto> UpdateOrganizationAsync(Guid organizationId, string name, string? description, Guid callerId, CancellationToken ct = default);
    Task<List<OrgMemberDto>> GetOrgMembersAsync(Guid organizationId, Guid callerId, CancellationToken ct = default);
    Task<OrgMemberDto> CreateEmployeeAsync(Guid organizationId, string fullName, string email, string password, string role, List<Guid>? workspaceIds, Guid callerId, CancellationToken ct = default);
    Task<OrgMemberDto> UpdateEmployeeAsync(Guid organizationId, Guid userId, string? fullName, string? email, string? role, List<Guid>? workspaceIds, Guid callerId, CancellationToken ct = default);
    Task DeleteEmployeeAsync(Guid organizationId, Guid userId, Guid callerId, CancellationToken ct = default);
}

public record OrganizationDto(Guid Id, string Name, string Slug, Guid OwnerId, string? Description, DateTime CreatedAt);
public record OrgMemberDto(Guid UserId, string FullName, string Email, string? AvatarUrl, string Role, int RoleInt, Guid WorkspaceId, DateTime JoinedAt, List<string>? WorkspaceNames = null, List<Guid>? WorkspaceIds = null);
