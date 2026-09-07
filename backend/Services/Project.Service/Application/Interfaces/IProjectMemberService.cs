using Project.Service.Application.DTOs;
using SharedKernel;

namespace Project.Service.Application.Interfaces;

public interface IProjectMemberService
{
    Task<PaginatedResult<ProjectMemberDto>> GetMembersAsync(Guid projectId, int page, int pageSize, string? search, CancellationToken ct = default);
    Task<Result<ProjectMemberDto>> AddMemberAsync(Guid projectId, Guid userId, string role, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<bool>> RemoveMemberAsync(Guid projectId, Guid userId, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<List<ProjectMemberDto>> GetMembersListAsync(Guid projectId, CancellationToken ct = default);
}
