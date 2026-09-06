using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// Board CRUD - Enterprise: Project has multiple Boards (Engineering/QA/Support) as views. Boards own Columns.
/// </summary>
public record CreateBoardCommand(Guid ProjectId, string Name, string Type, string? Description, Guid CallerId, List<string> CallerRoles, string? FilterJson = null) : IRequest<Result<BoardInfoDto>>;
public record UpdateBoardCommand(Guid BoardId, string Name, string Type, Guid CallerId, List<string> CallerRoles, string? FilterJson = null) : IRequest<Result<BoardInfoDto>>;
public record DeleteBoardCommand(Guid BoardId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<bool>>;

public class CreateBoardValidator : AbstractValidator<CreateBoardCommand>
{
    public CreateBoardValidator() { RuleFor(x => x.ProjectId).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(100); RuleFor(x => x.Type).Must(t => new[]{"Scrum","Kanban"}.Contains(t)).When(x=>!string.IsNullOrEmpty(x.Type)); }
}
public class UpdateBoardValidator : AbstractValidator<UpdateBoardCommand>
{
    public UpdateBoardValidator() { RuleFor(x => x.BoardId).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(100); }
}

public class CreateBoardHandler : IRequestHandler<CreateBoardCommand, Result<BoardInfoDto>>
{
    private readonly IApplicationDbContext _db;
    public CreateBoardHandler(IApplicationDbContext db) => _db = db;
    public async Task<Result<BoardInfoDto>> Handle(CreateBoardCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Viewer") || req.CallerRoles.Contains("Client"))
            return Result<BoardInfoDto>.Failure("Forbidden - Viewer/Client cannot create boards");
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, ct);
        if (project == null) return Result<BoardInfoDto>.Failure("Project not found");
        var maxPos = await _db.Boards.Where(b => b.ProjectId == req.ProjectId).MaxAsync(b => (int?)b.Position, ct) ?? -1;
        var board = new Domain.Entities.Board(req.ProjectId, req.Name, req.Type ?? "Kanban", req.Description, maxPos + 1, req.FilterJson);
        _db.Boards.Add(board);
        await _db.SaveChangesAsync(ct);
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(req.ProjectId, null, req.CallerId, "BoardCreated", $"{{\"name\":\"{req.Name}\"}}"));
        await _db.SaveChangesAsync(ct);
        return Result<BoardInfoDto>.Success(new BoardInfoDto(board.Id, board.ProjectId, board.Name, board.Type, board.Description, board.Position, board.CreatedAt, board.FilterJson));
    }
}
public class UpdateBoardHandler : IRequestHandler<UpdateBoardCommand, Result<BoardInfoDto>>
{
    private readonly IApplicationDbContext _db;
    public UpdateBoardHandler(IApplicationDbContext db) => _db = db;
    public async Task<Result<BoardInfoDto>> Handle(UpdateBoardCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Viewer") || req.CallerRoles.Contains("Client"))
            return Result<BoardInfoDto>.Failure("Forbidden - Viewer/Client cannot update boards");
        var board = await _db.Boards.FindAsync(new object[]{ req.BoardId }, ct);
        if (board == null) return Result<BoardInfoDto>.Failure("Board not found");
        board.Rename(req.Name);
        if (!string.IsNullOrEmpty(req.Type)) board.UpdateType(req.Type);
        board.SetFilter(req.FilterJson);
        await _db.SaveChangesAsync(ct);
        return Result<BoardInfoDto>.Success(new BoardInfoDto(board.Id, board.ProjectId, board.Name, board.Type, board.Description, board.Position, board.CreatedAt, board.FilterJson));
    }
}
public class DeleteBoardHandler : IRequestHandler<DeleteBoardCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    public DeleteBoardHandler(IApplicationDbContext db) => _db = db;
    public async Task<Result<bool>> Handle(DeleteBoardCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Viewer") || req.CallerRoles.Contains("Client"))
            return Result<bool>.Failure("Forbidden - Viewer/Client cannot delete boards");
        var board = await _db.Boards.FindAsync(new object[]{ req.BoardId }, ct);
        if (board == null) return Result<bool>.Failure("Board not found");
        var hasSprints = await _db.Sprints.AnyAsync(s => s.BoardId == req.BoardId, ct);
        if (hasSprints) return Result<bool>.Failure("Cannot delete board with sprints - delete sprints first");
        _db.Boards.Remove(board);
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
