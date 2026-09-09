using Project.Service.Application.DTOs;
using Project.Service.Application.Queries;
using SharedKernel;

namespace Project.Service.Application.Interfaces;

public interface IProjectService
{
    Task<Result<ProjectDto>> CreateProjectAsync(Guid workspaceId, string name, string? description, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<ProjectDto>> UpdateProjectAsync(Guid projectId, string name, string? description, string? slug, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<bool>> DeleteProjectAsync(Guid projectId, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<PaginatedResult<ProjectDto>> GetProjectsAsync(Guid workspaceId, int page, int pageSize, CancellationToken ct = default);
    Task<BoardDto> GetBoardAsync(Guid projectId, Guid? boardId, CancellationToken ct = default);
}

public interface IBoardService
{
    Task<Result<BoardInfoDto>> CreateBoardAsync(Guid projectId, string name, string type, string? description, string? filterJson, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<BoardInfoDto>> UpdateBoardAsync(Guid boardId, string name, string type, string? filterJson, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<bool>> DeleteBoardAsync(Guid boardId, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<List<BoardInfoDto>> GetBoardsAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<BoardListDto>> CreateBoardListAsync(Guid projectId, string name, Guid callerId, List<string> callerRoles, Guid? boardId, int? position, CancellationToken ct = default);
    Task<Result<BoardListDto>> UpdateBoardListAsync(Guid projectId, Guid listId, string name, int? position, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<bool>> DeleteBoardListAsync(Guid projectId, Guid listId, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
}

public interface ITaskService
{
    Task<Result<TaskDto>> CreateTaskAsync(Guid projectId, Guid? listId, string title, string? description, string priority, string? labelsJson, Guid? assigneeId, DateTime? dueDate, string? issueType, string? epic, int? storyPoints, DateTime? startDate, string? environment, Guid? parentIssueId, Guid? sprintId, Guid? teamId, Guid callerId, List<string> callerRoles, CancellationToken ct = default, Guid? statusId = null);
    Task<Result<TaskDto>> UpdateTaskAsync(Guid taskId, string title, string? description, string priority, string? labelsJson, Guid? assigneeId, DateTime? dueDate, string? issueType, string? epic, int? storyPoints, DateTime? startDate, string? environment, Guid? parentIssueId, Guid? sprintId, string? watchersJson, string? linkedIssuesJson, int? timeEstimated, int? timeSpent, int? timeRemaining, Guid? teamId, Guid? listId, string? status, Guid callerId, List<string> callerRoles, CancellationToken ct = default, Guid? statusId = null);
    Task<Result> MoveTaskAsync(Guid taskId, Guid toListId, int newPosition, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result> DeleteTaskAsync(Guid taskId, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<PaginatedResult<TaskDto>> GetTasksAsync(Guid projectId, string? search, Guid? assigneeId, string? priority, string? label, DateTime? dueFrom, DateTime? dueTo, string? sortBy, bool sortDesc, int page, int pageSize, CancellationToken ct = default);
    Task<TaskDetailDto> GetTaskDetailAsync(Guid taskId, CancellationToken ct = default);
}

public interface ISprintService
{
    Task<Result<SprintDto>> CreateSprintAsync(Guid projectId, Guid? boardId, string name, DateTime startDate, DateTime endDate, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<SprintDto>> UpdateSprintAsync(Guid sprintId, string name, DateTime startDate, DateTime endDate, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<bool>> DeleteSprintAsync(Guid sprintId, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<List<SprintDto>> GetSprintsAsync(Guid projectId, Guid? boardId, CancellationToken ct = default);
}

public interface ITeamService
{
    Task<Result<TeamDto>> CreateTeamAsync(Guid projectId, string name, string? description, Guid callerId, CancellationToken ct = default);
    Task<Result<TeamDto>> UpdateTeamAsync(Guid teamId, string name, string? description, Guid callerId, CancellationToken ct = default);
    Task<Result> DeleteTeamAsync(Guid teamId, Guid callerId, CancellationToken ct = default);
    Task<List<TeamDto>> GetTeamsAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<TeamMemberDto>> AddMemberAsync(Guid teamId, Guid userId, Guid callerId, CancellationToken ct = default);
    Task<Result> RemoveMemberAsync(Guid teamId, Guid userId, Guid callerId, CancellationToken ct = default);
    Task<List<TeamMemberDto>> GetMembersAsync(Guid teamId, CancellationToken ct = default);
}

public interface IEnvironmentService
{
    Task<Result<ProjectEnvironmentDto>> CreateEnvironmentAsync(Guid projectId, string name, string url, string? description, string status, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<ProjectEnvironmentDto>> UpdateEnvironmentAsync(Guid environmentId, string name, string url, string? description, string status, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<bool>> DeleteEnvironmentAsync(Guid environmentId, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<List<ProjectEnvironmentDto>> GetEnvironmentsAsync(Guid projectId, CancellationToken ct = default);
}
