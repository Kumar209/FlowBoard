using MediatR;
using SharedKernel;
using Identity.Service.Application.Interfaces;

namespace Identity.Service.Application.Queries;

public record GetOrgStatsQuery(Guid OrganizationId, Guid CallerId) : IRequest<Result<OrgStatsDto>>;
public record GetOrgChartDataQuery(Guid OrganizationId, Guid CallerId) : IRequest<Result<OrgChartDataDto>>;

public class GetOrgStatsHandler : IRequestHandler<GetOrgStatsQuery, Result<OrgStatsDto>>
{
    private readonly IOrganizationStatsService _svc;
    public GetOrgStatsHandler(IOrganizationStatsService svc) => _svc = svc;
    public async Task<Result<OrgStatsDto>> Handle(GetOrgStatsQuery req, CancellationToken ct)
    {
        try { var dto = await _svc.GetOrgStatsAsync(req.OrganizationId, req.CallerId, ct); return Result<OrgStatsDto>.Success(dto); }
        catch (Exception ex) { return Result<OrgStatsDto>.Failure(ex.Message); }
    }
}

public class GetOrgChartDataHandler : IRequestHandler<GetOrgChartDataQuery, Result<OrgChartDataDto>>
{
    private readonly IOrganizationStatsService _svc;
    public GetOrgChartDataHandler(IOrganizationStatsService svc) => _svc = svc;
    public async Task<Result<OrgChartDataDto>> Handle(GetOrgChartDataQuery req, CancellationToken ct)
    {
        try { var dto = await _svc.GetOrgChartDataAsync(req.OrganizationId, req.CallerId, ct); return Result<OrgChartDataDto>.Success(dto); }
        catch (Exception ex) { return Result<OrgChartDataDto>.Failure(ex.Message); }
    }
}
