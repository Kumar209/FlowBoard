using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

public record GetBoardsQuery(Guid ProjectId) : IRequest<List<BoardInfoDto>>;

public class GetBoardsHandler : IRequestHandler<GetBoardsQuery, List<BoardInfoDto>>
{
    private readonly IApplicationDbContext _db;
    public GetBoardsHandler(IApplicationDbContext db) => _db = db;
    public async Task<List<BoardInfoDto>> Handle(GetBoardsQuery req, CancellationToken ct)
        => await _db.Boards.Where(b => b.ProjectId == req.ProjectId).OrderBy(b => b.Position)
            .Select(b => new BoardInfoDto(b.Id, b.ProjectId, b.Name, b.Type, b.Description, b.Position, b.CreatedAt, b.FilterJson))
            .ToListAsync(ct);
}

public record GetSprintsQuery(Guid ProjectId, Guid? BoardId = null) : IRequest<List<SprintDto>>;

public class GetSprintsHandler : IRequestHandler<GetSprintsQuery, List<SprintDto>>
{
    private readonly IApplicationDbContext _db;
    public GetSprintsHandler(IApplicationDbContext db) => _db = db;
    public async Task<List<SprintDto>> Handle(GetSprintsQuery req, CancellationToken ct)
    {
        var q = _db.Sprints.Where(s => s.ProjectId == req.ProjectId);
        if (req.BoardId != null && req.BoardId != Guid.Empty) q = q.Where(s => s.BoardId == req.BoardId);
        return await q.OrderBy(s => s.StartDate)
            .Select(s => new SprintDto(s.Id, s.ProjectId, s.BoardId, s.Name, s.StartDate, s.EndDate, s.Status, s.CreatedAt))
            .ToListAsync(ct);
    }
}
