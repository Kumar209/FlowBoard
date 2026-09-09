namespace Identity.Service.Application.Interfaces;

public record OrganizationRoleDto(Guid Id, Guid OrganizationId, string Name, string? Description, Guid CreatedBy, DateTime CreatedAt, int MembersCount, int PermissionsCount);
public record PermissionDto(Guid Id, string Key, string Name, string Group, string? Description);
public record RoleWithPermissionsDto(OrganizationRoleDto Role, List<Guid> PermissionIds, List<PermissionDto> Permissions);

public interface IOrganizationRoleService
{
    Task<List<OrganizationRoleDto>> GetRolesAsync(Guid organizationId, Guid callerId, CancellationToken ct = default);
    Task<OrganizationRoleDto> CreateRoleAsync(Guid organizationId, string name, string? description, Guid callerId, CancellationToken ct = default);
    Task<OrganizationRoleDto> UpdateRoleAsync(Guid organizationId, Guid roleId, string name, string? description, Guid callerId, CancellationToken ct = default);
    Task DeleteRoleAsync(Guid organizationId, Guid roleId, Guid callerId, CancellationToken ct = default);
    Task<RoleWithPermissionsDto> GetRoleWithPermissionsAsync(Guid organizationId, Guid roleId, Guid callerId, CancellationToken ct = default);
    Task<RoleWithPermissionsDto> UpdateRolePermissionsAsync(Guid organizationId, Guid roleId, List<Guid> permissionIds, Guid callerId, CancellationToken ct = default);
    Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken ct = default);
}
