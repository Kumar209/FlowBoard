using Identity.Service.Domain.Entities;

namespace Identity.Service.Application.Interfaces;

public interface IWorkspaceService
{
    Task<(List<WorkspaceDto> Items, int Total)> GetMyWorkspacesAsync(Guid userId, int page, int pageSize, string? search, CancellationToken ct = default);
    Task<WorkspaceDto> CreateWorkspaceAsync(Guid organizationId, string name, Guid userId, CancellationToken ct = default);
    Task<WorkspaceMemberDto> InviteAsync(Guid workspaceId, string email, string role, Guid callerId, CancellationToken ct = default);
    Task<WorkspaceDto> UpdateWorkspaceAsync(Guid workspaceId, string name, string? slug, Guid callerId, CancellationToken ct = default);
    Task DeleteWorkspaceAsync(Guid workspaceId, Guid callerId, CancellationToken ct = default);
    Task<List<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId, Guid callerId, CancellationToken ct = default);
    Task<WorkspaceMemberDto> ChangeRoleAsync(Guid workspaceId, Guid userId, string role, Guid callerId, CancellationToken ct = default);
}

public record WorkspaceDto(Guid Id, string Name, string Slug, Guid OrganizationId, string Role);
public record WorkspaceMemberDto(Guid WorkspaceId, Guid UserId, string Email, string FullName, string? AvatarUrl, string Role, int RoleInt, DateTime JoinedAt);
