using Project.Service.Application.DTOs;
using SharedKernel;

namespace Project.Service.Application.Interfaces;

public interface IStatusService
{
    Task<List<StatusDto>> GetStatusesAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<StatusDto>> CreateStatusAsync(Guid projectId, string name, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<StatusDto>> UpdateStatusAsync(Guid statusId, string name, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<bool>> DeleteStatusAsync(Guid statusId, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
}
