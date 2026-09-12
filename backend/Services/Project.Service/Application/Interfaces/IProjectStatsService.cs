namespace Project.Service.Application.Interfaces;

public record ProjectStatsDto(int TotalIssues, int CompletedIssues, int InProgressIssues, int TotalStoryPoints, int ActiveSprints, string? ActiveSprintName);
public record ProjectChartBucketDto(string Label, int Value);
public record ProjectVelocityDto(string SprintName, int StoryPoints, int CompletedPoints);
public record AssigneeWorkloadDto(string AssigneeName, int IssueCount);
public record ProjectChartDataDto(
    List<ProjectChartBucketDto> IssuesByStatus,
    List<ProjectChartBucketDto> IssuesByType,
    List<ProjectChartBucketDto> IssuesByPriority,
    List<AssigneeWorkloadDto> AssigneeWorkload,
    List<ProjectVelocityDto> SprintVelocity
);
public record BurndownPointDto(string Date, int Total, int Remaining, int Ideal);
public record BurndownDto(Guid SprintId, string SprintName, string StartDate, string EndDate, List<BurndownPointDto> Points);

public interface IProjectStatsService
{
    Task<ProjectStatsDto> GetProjectStatsAsync(Guid projectId, Guid callerId, CancellationToken ct = default);
    Task<ProjectChartDataDto> GetProjectChartDataAsync(Guid projectId, Guid callerId, CancellationToken ct = default);
    Task<BurndownDto> GetBurndownAsync(Guid projectId, Guid? sprintId, Guid callerId, CancellationToken ct = default);
}
