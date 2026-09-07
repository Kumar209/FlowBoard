using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

/// <summary>
/// GetBoard - returns project with ordered lists and tasks grouped by ListId+Position. Cached as board:{projectId} TTL 5m via Upstash Redis (Task 2.3). Invalidated on task write.
/// </summary>
public record GetBoardQuery(Guid ProjectId, Guid? BoardId = null) : ICacheableRequest<BoardDto>
{
    public string CacheKey => BoardId.HasValue ? $"{CacheKeys.Board(ProjectId)}:{BoardId}" : CacheKeys.Board(ProjectId);
    public TimeSpan Expiration => TimeSpan.FromMinutes(CacheKeys.BoardTtlMinutes);
}

public record BoardDto(ProjectDto Project, List<BoardListDto> Lists, List<TaskDto> Tasks, List<BoardListDto>? Boards = null);

public class GetBoardHandler : IRequestHandler<GetBoardQuery, BoardDto>
{
    private readonly IProjectService _service;
    public GetBoardHandler(IProjectService service) => _service = service;
    public Task<BoardDto> Handle(GetBoardQuery req, CancellationToken ct)
        => _service.GetBoardAsync(req.ProjectId, req.BoardId, ct);
}
