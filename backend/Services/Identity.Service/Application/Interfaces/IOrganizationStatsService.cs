namespace Identity.Service.Application.Interfaces;

public record OrgStatsDto(int TotalWorkspaces, int TotalProjects, int TotalMembers, int TotalIssues, int ActiveSprints, int CompletedIssues);
public record ChartBucketDto(string Label, int Value);
public record DailyCountDto(string Date, int Count);
public record AiUsageDailyDto(string Date, long Tokens, decimal Cost, int Requests);
public record OrgChartDataDto(
    List<ChartBucketDto> IssuesByStatus,
    List<ChartBucketDto> IssuesByPriority,
    List<ChartBucketDto> IssuesByWorkspace,
    List<DailyCountDto> ActivityTrend,
    List<AiUsageDailyDto> AiUsage
);

public interface IOrganizationStatsService
{
    Task<OrgStatsDto> GetOrgStatsAsync(Guid organizationId, Guid callerId, CancellationToken ct = default);
    Task<OrgChartDataDto> GetOrgChartDataAsync(Guid organizationId, Guid callerId, CancellationToken ct = default);
}
