using MediatR;
using SharedKernel;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

public record GetProjectStatsQuery(Guid ProjectId, Guid CallerId) : IRequest<Result<ProjectStatsDto>>;
public record GetProjectChartDataQuery(Guid ProjectId, Guid CallerId) : IRequest<Result<ProjectChartDataDto>>;
public record GetBurndownQuery(Guid ProjectId, Guid? SprintId, Guid CallerId) : IRequest<Result<BurndownDto>>;

public class GetProjectStatsHandler : IRequestHandler<GetProjectStatsQuery, Result<ProjectStatsDto>>
{
    private readonly IProjectStatsService _svc;
    public GetProjectStatsHandler(IProjectStatsService svc) => _svc = svc;
    public async Task<Result<ProjectStatsDto>> Handle(GetProjectStatsQuery req, CancellationToken ct)
    {
        try { var dto = await _svc.GetProjectStatsAsync(req.ProjectId, req.CallerId, ct); return Result<ProjectStatsDto>.Success(dto); }
        catch (Exception ex) { return Result<ProjectStatsDto>.Failure(ex.Message); }
    }
}
public class GetProjectChartDataHandler : IRequestHandler<GetProjectChartDataQuery, Result<ProjectChartDataDto>>
{
    private readonly IProjectStatsService _svc;
    public GetProjectChartDataHandler(IProjectStatsService svc) => _svc = svc;
    public async Task<Result<ProjectChartDataDto>> Handle(GetProjectChartDataQuery req, CancellationToken ct)
    {
        try { var dto = await _svc.GetProjectChartDataAsync(req.ProjectId, req.CallerId, ct); return Result<ProjectChartDataDto>.Success(dto); }
        catch (Exception ex) { return Result<ProjectChartDataDto>.Failure(ex.Message); }
    }
}
public class GetBurndownHandler : IRequestHandler<GetBurndownQuery, Result<BurndownDto>>
{
    private readonly IProjectStatsService _svc;
    public GetBurndownHandler(IProjectStatsService svc) => _svc = svc;
    public async Task<Result<BurndownDto>> Handle(GetBurndownQuery req, CancellationToken ct)
    {
        try { var dto = await _svc.GetBurndownAsync(req.ProjectId, req.SprintId, req.CallerId, ct); return Result<BurndownDto>.Success(dto); }
        catch (Exception ex) { return Result<BurndownDto>.Failure(ex.Message); }
    }
}
