using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

/// <summary>
/// GetTaskDetail - Jira-style detail: task + subtasks + comments.
/// </summary>
public record GetTaskDetailQuery(Guid TaskId) : IRequest<TaskDetailDto>;

public class GetTaskDetailHandler : IRequestHandler<GetTaskDetailQuery, TaskDetailDto>
{
    private readonly ITaskService _service;
    public GetTaskDetailHandler(ITaskService service) => _service = service;
    public Task<TaskDetailDto> Handle(GetTaskDetailQuery req, CancellationToken ct)
        => _service.GetTaskDetailAsync(req.TaskId, ct);
}

public record GetSubTasksQuery(Guid TaskId) : IRequest<List<SubTaskDto>>;
public class GetSubTasksHandler : IRequestHandler<GetSubTasksQuery, List<SubTaskDto>>
{
    private readonly IApplicationDbContext _db;
    public GetSubTasksHandler(IApplicationDbContext db) => _db = db;
    public async Task<List<SubTaskDto>> Handle(GetSubTasksQuery req, CancellationToken ct)
        => await _db.SubTasks.Where(s => s.TaskId == req.TaskId).OrderBy(s => s.CreatedAt)
            .Select(s => new SubTaskDto(s.Id, s.TaskId, s.Title, s.IsCompleted, s.CreatedAt)).ToListAsync(ct);
}

public record GetCommentsQuery(Guid TaskId) : IRequest<List<CommentDto>>;
public class GetCommentsHandler : IRequestHandler<GetCommentsQuery, List<CommentDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCommentsHandler(IApplicationDbContext db) => _db = db;
    public async Task<List<CommentDto>> Handle(GetCommentsQuery req, CancellationToken ct)
        => await _db.Comments.Where(c => c.TaskId == req.TaskId).OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(c.Id, c.TaskId, c.AuthorId, c.Content, c.CreatedAt)).ToListAsync(ct);
}
