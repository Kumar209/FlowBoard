using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Caching;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// CreateBoardList - column in Project board (To Do/In Progress/Done). Position = max+1. Allowed for Member+ (any workspace member) - Viewer read-only check at controller if needed.
/// </summary>
public record CreateBoardListCommand(Guid ProjectId, string Name, Guid CallerId, List<string> CallerRoles) : IRequest<Result<BoardListDto>>;

public class CreateBoardListValidator : AbstractValidator<CreateBoardListCommand>
{
    public CreateBoardListValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateBoardListHandler : IRequestHandler<CreateBoardListCommand, Result<BoardListDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IRedisCacheService _cache;
    public CreateBoardListHandler(IApplicationDbContext db, IRedisCacheService cache) { _db = db; _cache = cache; }

    public async Task<Result<BoardListDto>> Handle(CreateBoardListCommand req, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, ct);
        if (project == null) return Result<BoardListDto>.Failure("Project not found");

        var maxPos = await _db.BoardLists.Where(b => b.ProjectId == req.ProjectId).MaxAsync(b => (int?)b.Position, ct) ?? -1;
        var list = new Domain.Entities.BoardList(req.ProjectId, req.Name, maxPos + 1);
        _db.BoardLists.Add(list);
        await _db.SaveChangesAsync(ct);

        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(req.ProjectId, null, req.CallerId, "ListCreated", $"{{\"name\":\"{req.Name}\"}}"));
        await _db.SaveChangesAsync(ct);
        await _cache.RemoveAsync(CacheKeys.Board(req.ProjectId));
        return Result<BoardListDto>.Success(new BoardListDto(list.Id, list.ProjectId, list.Name, list.Position));
    }
}
