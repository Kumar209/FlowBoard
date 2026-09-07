using Project.Service.Application.DTOs;
using SharedKernel;

namespace Project.Service.Application.Interfaces;

public interface ICommentService
{
    Task<Result<CommentDto>> AddCommentAsync(Guid taskId, string content, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<CommentDto>> UpdateCommentAsync(Guid commentId, string content, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<bool>> DeleteCommentAsync(Guid commentId, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<List<CommentDto>> GetCommentsAsync(Guid taskId, CancellationToken ct = default);
}

public interface ISubTaskService
{
    Task<Result<SubTaskDto>> CreateSubTaskAsync(Guid taskId, string title, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<SubTaskDto>> UpdateSubTaskAsync(Guid subTaskId, string title, Guid callerId, CancellationToken ct = default);
    Task<Result<SubTaskDto>> ToggleSubTaskAsync(Guid subTaskId, Guid callerId, CancellationToken ct = default);
    Task<Result<bool>> DeleteSubTaskAsync(Guid subTaskId, Guid callerId, CancellationToken ct = default);
    Task<List<SubTaskDto>> GetSubTasksAsync(Guid taskId, CancellationToken ct = default);
}

public interface IActivityService
{
    Task<(List<ActivityDto> Items, int Total)> GetActivitiesAsync(Guid projectId, int page, int pageSize, Guid? taskId, CancellationToken ct = default);
}
