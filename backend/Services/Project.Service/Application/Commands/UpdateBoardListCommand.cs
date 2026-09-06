using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// UpdateBoardList - Rename list (Jira Kanban). Viewer/Client 403.
/// </summary>
public record UpdateBoardListCommand(Guid ProjectId, Guid ListId, string Name, int? Position, Guid CallerId, List<string> CallerRoles) : IRequest<Result<BoardListDto>>;

public class UpdateBoardListValidator : AbstractValidator<UpdateBoardListCommand>
{
    public UpdateBoardListValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ListId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateBoardListHandler : IRequestHandler<UpdateBoardListCommand, Result<BoardListDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public UpdateBoardListHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }

    public async Task<Result<BoardListDto>> Handle(UpdateBoardListCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Viewer") || req.CallerRoles.Contains("Client"))
            return Result<BoardListDto>.Failure("Forbidden - Viewer/Client cannot rename lists");

        var list = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == req.ListId && b.ProjectId == req.ProjectId, ct);
        if (list == null) return Result<BoardListDto>.Failure("List not found");

        if (req.Position.HasValue && req.Position.Value != list.Position)
        {
            var conflict = await _db.BoardLists.AnyAsync(b => b.ProjectId == req.ProjectId && b.BoardId == list.BoardId && b.Position == req.Position.Value && b.Id != list.Id, ct);
            if (conflict) return Result<BoardListDto>.Failure($"Position {req.Position.Value} already used by another column in this board — choose another");
            list.Move(req.Position.Value);
        }
        list.Rename(req.Name);
        await _db.SaveChangesAsync(ct);
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(req.ProjectId, null, req.CallerId, "ListRenamed", $"{{\"name\":\"{req.Name}\"}}"));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CacheKeys.Board(req.ProjectId));
        return Result<BoardListDto>.Success(new BoardListDto(list.Id, list.ProjectId, list.Name, list.Position));
    }
}

public record DeleteBoardListCommand(Guid ProjectId, Guid ListId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<bool>>;

public class DeleteBoardListHandler : IRequestHandler<DeleteBoardListCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public DeleteBoardListHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }

    public async Task<Result<bool>> Handle(DeleteBoardListCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Viewer") || req.CallerRoles.Contains("Client"))
            return Result<bool>.Failure("Forbidden - Viewer/Client cannot delete lists");

        var list = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == req.ListId && b.ProjectId == req.ProjectId, ct);
        if (list == null) return Result<bool>.Failure("List not found");

        var hasTasks = await _db.Tasks.AnyAsync(t => t.ListId == req.ListId, ct);
        if (hasTasks) return Result<bool>.Failure("Cannot delete list with tasks - move or delete tasks first");

        _db.BoardLists.Remove(list);
        await _db.SaveChangesAsync(ct);
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(req.ProjectId, null, req.CallerId, "ListDeleted", $"{{\"name\":\"{list.Name}\"}}"));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CacheKeys.Board(req.ProjectId));
        return Result<bool>.Success(true);
    }
}
