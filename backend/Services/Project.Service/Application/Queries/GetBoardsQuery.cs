using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

public record GetBoardsQuery(Guid ProjectId) : IRequest<List<BoardInfoDto>>;

public class GetBoardsHandler : IRequestHandler<GetBoardsQuery, List<BoardInfoDto>>
{
    private readonly IBoardService _service;
    public GetBoardsHandler(IBoardService service) => _service = service;
    public Task<List<BoardInfoDto>> Handle(GetBoardsQuery req, CancellationToken ct)
        => _service.GetBoardsAsync(req.ProjectId, ct);
}

public record GetSprintsQuery(Guid ProjectId, Guid? BoardId = null) : IRequest<List<SprintDto>>;

public class GetSprintsHandler : IRequestHandler<GetSprintsQuery, List<SprintDto>>
{
    private readonly ISprintService _service;
    public GetSprintsHandler(ISprintService service) => _service = service;
    public Task<List<SprintDto>> Handle(GetSprintsQuery req, CancellationToken ct)
        => _service.GetSprintsAsync(req.ProjectId, req.BoardId, ct);
}
