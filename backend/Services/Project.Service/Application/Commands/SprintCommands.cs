using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// Sprint CRUD - Board → Sprint → Columns. Sprint groups issues for time-box.
/// </summary>
public record CreateSprintCommand(Guid ProjectId, Guid? BoardId, string Name, DateTime StartDate, DateTime EndDate, Guid CallerId, List<string> CallerRoles) : IRequest<Result<SprintDto>>;
public record UpdateSprintCommand(Guid SprintId, string Name, DateTime StartDate, DateTime EndDate, Guid CallerId, List<string> CallerRoles) : IRequest<Result<SprintDto>>;
public record DeleteSprintCommand(Guid SprintId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<bool>>;

public class CreateSprintValidator : AbstractValidator<CreateSprintCommand>
{
    public CreateSprintValidator() { RuleFor(x => x.ProjectId).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(100); RuleFor(x => x.StartDate).NotEmpty(); RuleFor(x => x.EndDate).NotEmpty().GreaterThan(x => x.StartDate); }
}
public class UpdateSprintValidator : AbstractValidator<UpdateSprintCommand>
{
    public UpdateSprintValidator() { RuleFor(x => x.SprintId).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(100); }
}

public class CreateSprintHandler : IRequestHandler<CreateSprintCommand, Result<SprintDto>>
{
    private readonly IApplicationDbContext _db;
    public CreateSprintHandler(IApplicationDbContext db) => _db = db;
    public async Task<Result<SprintDto>> Handle(CreateSprintCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Viewer") || req.CallerRoles.Contains("Client"))
            return Result<SprintDto>.Failure("Forbidden - Viewer/Client cannot create sprints");
        if (req.BoardId != null && req.BoardId != Guid.Empty) {
            var board = await _db.Boards.FindAsync(new object[]{ req.BoardId }, ct);
            if (board == null) return Result<SprintDto>.Failure("Board not found");
            if (board.ProjectId != req.ProjectId) return Result<SprintDto>.Failure("Board does not belong to project");
        }
        var sprint = new Domain.Entities.Sprint(req.ProjectId, req.BoardId, req.Name, req.StartDate, req.EndDate, "Planned");
        _db.Sprints.Add(sprint);
        await _db.SaveChangesAsync(ct);
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(req.ProjectId, null, req.CallerId, "SprintCreated", $"{{\"name\":\"{req.Name}\"}}"));
        await _db.SaveChangesAsync(ct);
        return Result<SprintDto>.Success(new SprintDto(sprint.Id, sprint.ProjectId, sprint.BoardId, sprint.Name, sprint.StartDate, sprint.EndDate, sprint.Status, sprint.CreatedAt));
    }
}
public class UpdateSprintHandler : IRequestHandler<UpdateSprintCommand, Result<SprintDto>>
{
    private readonly IApplicationDbContext _db;
    public UpdateSprintHandler(IApplicationDbContext db) => _db = db;
    public async Task<Result<SprintDto>> Handle(UpdateSprintCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Viewer") || req.CallerRoles.Contains("Client"))
            return Result<SprintDto>.Failure("Forbidden - Viewer/Client cannot update sprints");
        var sprint = await _db.Sprints.FindAsync(new object[]{ req.SprintId }, ct);
        if (sprint == null) return Result<SprintDto>.Failure("Sprint not found");
        sprint.Update(req.Name, req.StartDate, req.EndDate);
        await _db.SaveChangesAsync(ct);
        return Result<SprintDto>.Success(new SprintDto(sprint.Id, sprint.ProjectId, sprint.BoardId, sprint.Name, sprint.StartDate, sprint.EndDate, sprint.Status, sprint.CreatedAt));
    }
}
public class DeleteSprintHandler : IRequestHandler<DeleteSprintCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    public DeleteSprintHandler(IApplicationDbContext db) => _db = db;
    public async Task<Result<bool>> Handle(DeleteSprintCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Viewer") || req.CallerRoles.Contains("Client"))
            return Result<bool>.Failure("Forbidden - Viewer/Client cannot delete sprints");
        var sprint = await _db.Sprints.FindAsync(new object[]{ req.SprintId }, ct);
        if (sprint == null) return Result<bool>.Failure("Sprint not found");
        _db.Sprints.Remove(sprint);
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
