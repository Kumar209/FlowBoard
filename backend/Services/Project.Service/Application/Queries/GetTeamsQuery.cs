using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

public record GetTeamsQuery(Guid ProjectId) : IRequest<List<TeamDto>>;
public record GetTeamMembersQuery(Guid TeamId) : IRequest<List<TeamMemberDto>>;

public class GetTeamsHandler : IRequestHandler<GetTeamsQuery, List<TeamDto>>
{
    private readonly IApplicationDbContext _db;
    public GetTeamsHandler(IApplicationDbContext db) => _db = db;
    public async Task<List<TeamDto>> Handle(GetTeamsQuery req, CancellationToken ct)
    {
        var teams = await _db.Teams.Where(t => t.ProjectId == req.ProjectId).OrderBy(t => t.CreatedAt).ToListAsync(ct);
        var result = new List<TeamDto>();
        foreach (var t in teams)
        {
            var count = await _db.TeamMembers.CountAsync(m => m.TeamId == t.Id, ct);
            result.Add(new TeamDto(t.Id, t.ProjectId, t.Name, t.Description, t.CreatedAt, count));
        }
        return result;
    }
}

public class GetTeamMembersHandler : IRequestHandler<GetTeamMembersQuery, List<TeamMemberDto>>
{
    private readonly IApplicationDbContext _db;
    public GetTeamMembersHandler(IApplicationDbContext db) => _db = db;
    public async Task<List<TeamMemberDto>> Handle(GetTeamMembersQuery req, CancellationToken ct)
    {
        return await _db.TeamMembers.Where(m => m.TeamId == req.TeamId).OrderBy(m => m.JoinedAt)
            .Select(m => new TeamMemberDto(m.Id, m.TeamId, m.UserId, m.JoinedAt)).ToListAsync(ct);
    }
}
