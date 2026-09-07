using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;
using Project.Service.Domain.Entities;
using SharedKernel;

namespace Project.Service.Infrastructure.Services;

public class BoardService : IBoardService
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public BoardService(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }

    public async Task<Result<BoardInfoDto>> CreateBoardAsync(Guid projectId, string name, string type, string? description, string? filterJson, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Viewer") || callerRoles.Contains("Client")) return Result<BoardInfoDto>.Failure("Forbidden - Viewer/Client cannot create boards");
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project == null) return Result<BoardInfoDto>.Failure("Project not found");
        var maxPos = await _db.Boards.Where(b => b.ProjectId == projectId).MaxAsync(b => (int?)b.Position, ct) ?? -1;
        var board = new Board(projectId, name, type ?? "Kanban", description, maxPos + 1, filterJson);
        _db.Boards.Add(board);
        await _db.SaveChangesAsync(ct);
        _db.ActivityLogs.Add(new ActivityLog(projectId, null, callerId, "BoardCreated", $"{{\"name\":\"{name}\"}}"));
        await _db.SaveChangesAsync(ct);
        return Result<BoardInfoDto>.Success(new BoardInfoDto(board.Id, board.ProjectId, board.Name, board.Type, board.Description, board.Position, board.CreatedAt, board.FilterJson));
    }

    public async Task<Result<BoardInfoDto>> UpdateBoardAsync(Guid boardId, string name, string type, string? filterJson, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Viewer") || callerRoles.Contains("Client")) return Result<BoardInfoDto>.Failure("Forbidden - Viewer/Client cannot update boards");
        var board = await _db.Boards.FindAsync(new object[] { boardId }, ct);
        if (board == null) return Result<BoardInfoDto>.Failure("Board not found");
        board.Rename(name);
        if (!string.IsNullOrEmpty(type)) board.UpdateType(type);
        board.SetFilter(filterJson);
        await _db.SaveChangesAsync(ct);
        return Result<BoardInfoDto>.Success(new BoardInfoDto(board.Id, board.ProjectId, board.Name, board.Type, board.Description, board.Position, board.CreatedAt, board.FilterJson));
    }

    public async Task<Result<bool>> DeleteBoardAsync(Guid boardId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Viewer") || callerRoles.Contains("Client")) return Result<bool>.Failure("Forbidden - Viewer/Client cannot delete boards");
        var board = await _db.Boards.FindAsync(new object[] { boardId }, ct);
        if (board == null) return Result<bool>.Failure("Board not found");
        var hasSprints = await _db.Sprints.AnyAsync(s => s.BoardId == boardId, ct);
        if (hasSprints) return Result<bool>.Failure("Cannot delete board with sprints - delete sprints first");
        _db.Boards.Remove(board);
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<List<BoardInfoDto>> GetBoardsAsync(Guid projectId, CancellationToken ct = default)
        => await _db.Boards.Where(b => b.ProjectId == projectId).OrderBy(b => b.Position)
            .Select(b => new BoardInfoDto(b.Id, b.ProjectId, b.Name, b.Type, b.Description, b.Position, b.CreatedAt, b.FilterJson))
            .ToListAsync(ct);

    public async Task<Result<BoardListDto>> CreateBoardListAsync(Guid projectId, string name, Guid callerId, List<string> callerRoles, Guid? boardId, int? position, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project == null) return Result<BoardListDto>.Failure("Project not found");
        Guid? targetBoardId = boardId;
        if (targetBoardId == null)
        {
            var firstBoard = await _db.Boards.Where(b => b.ProjectId == projectId).OrderBy(b => b.Position).FirstOrDefaultAsync(ct);
            targetBoardId = firstBoard?.Id;
        }
        int pos;
        if (position.HasValue)
        {
            var conflict = await _db.BoardLists.AnyAsync(b => b.ProjectId == projectId && b.BoardId == targetBoardId && b.Position == position.Value, ct);
            if (conflict) return Result<BoardListDto>.Failure($"Position {position.Value} already used — choose another");
            pos = position.Value;
        }
        else
        {
            var maxPos = await _db.BoardLists.Where(b => b.ProjectId == projectId && b.BoardId == targetBoardId).MaxAsync(b => (int?)b.Position, ct) ?? -1;
            pos = maxPos + 1;
        }
        var list = new BoardList(projectId, name, pos, targetBoardId);
        _db.BoardLists.Add(list);
        await _db.SaveChangesAsync(ct);
        _db.ActivityLogs.Add(new ActivityLog(projectId, null, callerId, "ListCreated", $"{{\"name\":\"{name}\"}}"));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"board:{projectId}");
        await _cache.RemoveByPrefixAsync($"board:{projectId}:");
        return Result<BoardListDto>.Success(new BoardListDto(list.Id, list.ProjectId, list.Name, list.Position));
    }

    public async Task<Result<BoardListDto>> UpdateBoardListAsync(Guid projectId, Guid listId, string name, int? position, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Viewer") || callerRoles.Contains("Client")) return Result<BoardListDto>.Failure("Forbidden - Viewer/Client cannot rename lists");
        var list = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == listId && b.ProjectId == projectId, ct);
        if (list == null) return Result<BoardListDto>.Failure("List not found");
        if (position.HasValue && position.Value != list.Position)
        {
            var conflict = await _db.BoardLists.AnyAsync(b => b.ProjectId == projectId && b.BoardId == list.BoardId && b.Position == position.Value && b.Id != list.Id, ct);
            if (conflict) return Result<BoardListDto>.Failure($"Position {position.Value} already used by another column in this board — choose another");
            list.Move(position.Value);
        }
        list.Rename(name);
        await _db.SaveChangesAsync(ct);
        _db.ActivityLogs.Add(new ActivityLog(projectId, null, callerId, "ListRenamed", $"{{\"name\":\"{name}\"}}"));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"board:{projectId}");
        await _cache.RemoveByPrefixAsync($"board:{projectId}:");
        return Result<BoardListDto>.Success(new BoardListDto(list.Id, list.ProjectId, list.Name, list.Position));
    }

    public async Task<Result<bool>> DeleteBoardListAsync(Guid projectId, Guid listId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Viewer") || callerRoles.Contains("Client")) return Result<bool>.Failure("Forbidden - Viewer/Client cannot delete lists");
        var list = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == listId && b.ProjectId == projectId, ct);
        if (list == null) return Result<bool>.Failure("List not found");
        var hasTasks = await _db.Tasks.AnyAsync(t => t.ListId == listId, ct);
        if (hasTasks) return Result<bool>.Failure("Cannot delete list with tasks - move or delete tasks first");
        _db.BoardLists.Remove(list);
        await _db.SaveChangesAsync(ct);
        _db.ActivityLogs.Add(new ActivityLog(projectId, null, callerId, "ListDeleted", $"{{\"name\":\"{list.Name}\"}}"));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"board:{projectId}");
        await _cache.RemoveByPrefixAsync($"board:{projectId}:");
        return Result<bool>.Success(true);
    }
}
