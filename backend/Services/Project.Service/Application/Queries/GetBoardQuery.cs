using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

/// <summary>
/// Returns project with ordered lists and tasks. Cached as board:{projectId} 5m via Redis, invalidated on task write.
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
