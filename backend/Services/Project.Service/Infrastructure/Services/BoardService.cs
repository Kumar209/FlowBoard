using System.Text.RegularExpressions;
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
        if (Roles.CanUpload(callerRoles) == false) return Result<BoardInfoDto>.Failure("Forbidden - Client cannot create boards");
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project == null) return Result<BoardInfoDto>.Failure("Project not found");
        var maxPos = await _db.Boards.Where(b => b.ProjectId == projectId).MaxAsync(b => (int?)b.Position, ct) ?? -1;
        var board = new Board(projectId, name, type ?? "Kanban", description, maxPos + 1, filterJson);
        _db.Boards.Add(board);
        await _db.SaveChangesAsync(ct);
        var wsBoard = project.WorkspaceId;
        _db.ActivityLogs.Add(new ActivityLog(projectId, null, callerId, "BoardCreated", $"{{\"name\":\"{name}\"}}", wsBoard));
        await _db.SaveChangesAsync(ct);
        return Result<BoardInfoDto>.Success(new BoardInfoDto(board.Id, board.ProjectId, board.Name, board.Type, board.Description, board.Position, board.CreatedAt, board.FilterJson));
    }

    public async Task<Result<BoardInfoDto>> UpdateBoardAsync(Guid boardId, string name, string type, string? filterJson, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (Roles.CanUpload(callerRoles) == false) return Result<BoardInfoDto>.Failure("Forbidden - Client cannot update boards");
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
        if (Roles.CanUpload(callerRoles) == false) return Result<bool>.Failure("Forbidden - Client cannot delete boards");
        var board = await _db.Boards.FindAsync(new object[] { boardId }, ct);
        if (board == null) return Result<bool>.Failure("Board not found");
        var hasSprints = await _db.Sprints.AnyAsync(s => s.BoardId == boardId, ct);
        if (hasSprints) return Result<bool>.Failure("Cannot delete board with sprints - delete sprints first");
        _db.Boards.Remove(board);
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<List<BoardInfoDto>> GetBoardsAsync(Guid projectId, CancellationToken ct = default)
    {
        var boards = await _db.Boards.Where(b => b.ProjectId == projectId).OrderBy(b => b.Position).ToListAsync(ct);
        var result = new List<BoardInfoDto>();
        foreach (var b in boards)
        {
            int count = 0;
            try
            {
                if (string.IsNullOrWhiteSpace(b.FilterJson))
                {
                    count = await _db.Tasks.CountAsync(t => t.ProjectId == projectId, ct);
                }
                else
                {
                    var filter = System.Text.Json.JsonSerializer.Deserialize<BoardFilterDto>(b.FilterJson);
                    if (filter?.TeamIds != null && filter.TeamIds.Any())
                        count = await _db.Tasks.CountAsync(t => t.ProjectId == projectId && t.TeamId != null && filter.TeamIds.Contains(t.TeamId.Value), ct);
                    else
                        count = await _db.Tasks.CountAsync(t => t.ProjectId == projectId, ct);
                }
            }
            catch { count = await _db.Tasks.CountAsync(t => t.ProjectId == projectId, ct); }
            result.Add(new BoardInfoDto(b.Id, b.ProjectId, b.Name, b.Type, b.Description, b.Position, b.CreatedAt, b.FilterJson, count));
        }
        return result;
    }

    public async Task<Result<BoardListDto>> CreateBoardListAsync(Guid projectId, string name, Guid callerId, List<string> callerRoles, Guid? boardId, int? position, List<Guid>? statusIds = null, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project == null) return Result<BoardListDto>.Failure("Project not found");
        Guid? targetBoardId = boardId;
        if (targetBoardId == null)
        {
            var firstBoard = await _db.Boards.Where(b => b.ProjectId == projectId).OrderBy(b => b.Position).FirstOrDefaultAsync(ct);
            targetBoardId = firstBoard?.Id;
        }
        if (targetBoardId == null) return Result<BoardListDto>.Failure("Create a board first — no board to add column to");
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
        var normalized = Regex.Replace(name.Trim(), @"[-_]+", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        var display = string.Join(" ", normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => char.ToUpper(p[0]) + p.Substring(1).ToLower()));
        // Strict: status must already exist — do not auto-create. User must create status in Project → Statuses first.
        if (statusIds == null || !statusIds.Any())
            return Result<BoardListDto>.Failure("Select at least one existing Status to map this column to — create statuses in Project → Statuses first. No auto-create.");
        var existingStatuses = await _db.Statuses.Where(s => s.ProjectId == projectId && statusIds.Contains(s.Id)).ToListAsync(ct);
        if (existingStatuses.Count != statusIds.Count)
            return Result<BoardListDto>.Failure("One or more selected statuses not found in this project");
        var list = new BoardList(projectId, display, pos, targetBoardId);
        _db.BoardLists.Add(list);
        await _db.SaveChangesAsync(ct);
        // A1 — Move with ≥1 guard: one status = one column per board, ≥1 required, move only if source cnt>1
        var distinctIds = statusIds.Distinct().ToList();
        var alreadyMapped = await _db.BoardColumnStatuses
            .Where(bcs => distinctIds.Contains(bcs.StatusId))
            .Join(_db.BoardLists.Where(bl => bl.BoardId == targetBoardId), bcs => bcs.ColumnId, bl => bl.Id, (bcs, bl) => new { bcs, bl })
            .ToListAsync(ct);
        if (alreadyMapped.Any())
        {
            // Check edge 1→0: any source column with cnt==1 cannot be moved
            foreach (var g in alreadyMapped.GroupBy(x => x.bl.Id))
            {
                var cnt = await _db.BoardColumnStatuses.CountAsync(bcs => bcs.ColumnId == g.Key, ct);
                if (cnt == 1 && g.Any(x => distinctIds.Contains(x.bcs.StatusId)))
                {
                    var colName = g.First().bl.Name;
                    var statusId = g.First().bcs.StatusId;
                    var statusName = existingStatuses.First(s => s.Id == statusId).Name;
                    return Result<BoardListDto>.Failure($"Cannot move status '{statusName}' — column '{colName}' would be left without status. Add another status to '{colName}' first.");
                }
            }
            _db.BoardColumnStatuses.RemoveRange(alreadyMapped.Select(x => x.bcs));
            await _db.SaveChangesAsync(ct);
        }
        foreach (var sid in distinctIds)
        {
            _db.BoardColumnStatuses.Add(new BoardColumnStatus(list.Id, sid, targetBoardId.Value));
        }
        await _db.SaveChangesAsync(ct);
        var wsList = await _db.Projects.Where(p => p.Id == projectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync(ct);
        _db.ActivityLogs.Add(new ActivityLog(projectId, null, callerId, "ListCreated", $"{{\"name\":\"{name}\"}}", wsList));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"board:{projectId}");
        await _cache.RemoveByPrefixAsync($"board:{projectId}:");
        return Result<BoardListDto>.Success(new BoardListDto(list.Id, list.ProjectId, list.Name, list.Position));
    }

    public async Task<Result<BoardListDto>> UpdateBoardListAsync(Guid projectId, Guid listId, string name, int? position, Guid callerId, List<string> callerRoles, CancellationToken ct = default, List<Guid>? statusIds = null)
    {
        if (Roles.CanUpload(callerRoles) == false) return Result<BoardListDto>.Failure("Forbidden - Client cannot rename lists");
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
        // Update status mapping if provided — move statuses from other columns (C)
        if (statusIds != null)
        {
            if (!statusIds.Any()) return Result<BoardListDto>.Failure("Select at least one status to map");
            var distinctIds = statusIds.Distinct().ToList();
            var validStatuses = await _db.Statuses.Where(s => s.ProjectId == projectId && distinctIds.Contains(s.Id)).ToListAsync(ct);
            if (validStatuses.Count != distinctIds.Count) return Result<BoardListDto>.Failure("One or more statuses not found in this project");
            // Remove existing mappings for this column (target will get new set, so old target's 0 not an issue)
            var existing = await _db.BoardColumnStatuses.Where(bcs => bcs.ColumnId == listId).ToListAsync(ct);
            _db.BoardColumnStatuses.RemoveRange(existing);
            await _db.SaveChangesAsync(ct);
            // Move statuses already mapped to other columns of same board — check 1→0 edge
            var alreadyMappedOther = await _db.BoardColumnStatuses
                .Where(bcs => distinctIds.Contains(bcs.StatusId))
                .Join(_db.BoardLists.Where(bl => bl.BoardId == list.BoardId), bcs => bcs.ColumnId, bl => bl.Id, (bcs, bl) => new { bcs, bl })
                .ToListAsync(ct);
            if (alreadyMappedOther.Any())
            {
                foreach (var g in alreadyMappedOther.GroupBy(x => x.bl.Id))
                {
                    var cnt = await _db.BoardColumnStatuses.CountAsync(bcs => bcs.ColumnId == g.Key, ct);
                    if (cnt == 1 && g.Any(x => distinctIds.Contains(x.bcs.StatusId)))
                    {
                        var colName = g.First().bl.Name;
                        var statusName = validStatuses.First(s => g.Any(x => x.bcs.StatusId == s.Id)).Name;
                        // Rollback already removed existing for target: re-add before return
                        foreach (var old in existing) _db.BoardColumnStatuses.Add(new BoardColumnStatus(listId, old.StatusId, list.BoardId ?? Guid.Empty));
                        await _db.SaveChangesAsync(ct);
                        return Result<BoardListDto>.Failure($"Cannot move status '{statusName}' — column '{colName}' would be left without status. Add another status to '{colName}' first.");
                    }
                }
                _db.BoardColumnStatuses.RemoveRange(alreadyMappedOther.Select(x => x.bcs));
                await _db.SaveChangesAsync(ct);
            }
            foreach (var sid in distinctIds)
                _db.BoardColumnStatuses.Add(new BoardColumnStatus(listId, sid, list.BoardId ?? Guid.Empty));
            await _db.SaveChangesAsync(ct);
        }
        var wsRen = await _db.Projects.Where(p => p.Id == projectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync(ct);
        _db.ActivityLogs.Add(new ActivityLog(projectId, null, callerId, "ListRenamed", $"{{\"name\":\"{name}\"}}", wsRen));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"board:{projectId}");
        await _cache.RemoveByPrefixAsync($"board:{projectId}:");
        var updatedStatusIds = statusIds ?? (await _db.BoardColumnStatuses.Where(bcs => bcs.ColumnId == listId).Select(bcs => bcs.StatusId).ToListAsync(ct));
        return Result<BoardListDto>.Success(new BoardListDto(list.Id, list.ProjectId, list.Name, list.Position, updatedStatusIds));
    }

    public async Task<Result<bool>> DeleteBoardListAsync(Guid projectId, Guid listId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (Roles.CanUpload(callerRoles) == false) return Result<bool>.Failure("Forbidden - Client cannot delete lists");
        var list = await _db.BoardLists.FirstOrDefaultAsync(b => b.Id == listId && b.ProjectId == projectId, ct);
        if (list == null) return Result<bool>.Failure("List not found");
        var hasTasks = await _db.Tasks.AnyAsync(t => t.ListId == listId, ct);
        if (hasTasks) return Result<bool>.Failure("Cannot delete list with tasks - move or delete tasks first");
        _db.BoardLists.Remove(list);
        await _db.SaveChangesAsync(ct);
        var wsDel = await _db.Projects.Where(p => p.Id == projectId).Select(p => p.WorkspaceId).FirstOrDefaultAsync(ct);
        _db.ActivityLogs.Add(new ActivityLog(projectId, null, callerId, "ListDeleted", $"{{\"name\":\"{list.Name}\"}}", wsDel));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"board:{projectId}");
        await _cache.RemoveByPrefixAsync($"board:{projectId}:");
        return Result<bool>.Success(true);
    }
}
