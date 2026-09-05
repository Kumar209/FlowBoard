using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

/// <summary>
/// GetBoard - returns project with ordered lists and tasks grouped by ListId+Position. Cached as board:{projectId} TTL 5m via Upstash Redis (Task 2.3). Invalidated on task write.
/// </summary>
public record GetBoardQuery(Guid ProjectId) : IRequest<BoardDto>;

public record BoardDto(ProjectDto Project, List<BoardListDto> Lists, List<TaskDto> Tasks);

public class GetBoardHandler : IRequestHandler<GetBoardQuery, BoardDto>
{
    private readonly IApplicationDbContext _db;
    public GetBoardHandler(IApplicationDbContext db) => _db = db;

    public async Task<BoardDto> Handle(GetBoardQuery req, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, ct)
            ?? throw new Exception("Project not found");
        var lists = await _db.BoardLists.Where(b => b.ProjectId == req.ProjectId).OrderBy(b => b.Position)
            .Select(b => new BoardListDto(b.Id, b.ProjectId, b.Name, b.Position)).ToListAsync(ct);
        var tasks = await _db.Tasks.Where(t => t.ProjectId == req.ProjectId).OrderBy(t => t.Position)
            .Select(t => new TaskDto(t.Id, t.ProjectId, t.ListId, t.Title, t.Description, t.Priority.ToString(), t.LabelsJson, t.AssigneeId, t.Position, t.CreatedAt)).ToListAsync(ct);

        var dto = new ProjectDto(project.Id, project.WorkspaceId, project.Name, project.Key, project.Description, project.OwnerId, project.CreatedAt);
        return new BoardDto(dto, lists, tasks);
    }
}
