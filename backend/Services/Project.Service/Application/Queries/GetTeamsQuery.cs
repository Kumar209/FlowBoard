using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

public record GetTeamsQuery(Guid ProjectId) : IRequest<List<TeamDto>>;
public record GetTeamMembersQuery(Guid TeamId) : IRequest<List<TeamMemberDto>>;

public class GetTeamsHandler : IRequestHandler<GetTeamsQuery, List<TeamDto>>
{
    private readonly ITeamService _service;
    public GetTeamsHandler(ITeamService service) => _service = service;
    public Task<List<TeamDto>> Handle(GetTeamsQuery req, CancellationToken ct)
        => _service.GetTeamsAsync(req.ProjectId, ct);
}

public class GetTeamMembersHandler : IRequestHandler<GetTeamMembersQuery, List<TeamMemberDto>>
{
    private readonly ITeamService _service;
    public GetTeamMembersHandler(ITeamService service) => _service = service;
    public Task<List<TeamMemberDto>> Handle(GetTeamMembersQuery req, CancellationToken ct)
        => _service.GetMembersAsync(req.TeamId, ct);
}
