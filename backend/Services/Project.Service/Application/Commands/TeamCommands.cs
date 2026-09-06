using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

public record CreateTeamCommand(Guid ProjectId, string Name, string? Description, Guid CallerId) : IRequest<Result<TeamDto>>;
public record UpdateTeamCommand(Guid TeamId, string Name, string? Description, Guid CallerId) : IRequest<Result<TeamDto>>;
public record DeleteTeamCommand(Guid TeamId, Guid CallerId) : IRequest<Result<object>>;
public record AddTeamMemberCommand(Guid TeamId, Guid UserId, Guid CallerId) : IRequest<Result<TeamMemberDto>>;
public record RemoveTeamMemberCommand(Guid TeamId, Guid UserId, Guid CallerId) : IRequest<Result<object>>;

public class CreateTeamValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(100); RuleFor(x => x.ProjectId).NotEmpty(); }
}
public class UpdateTeamValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(100); }
}

public class CreateTeamHandler : IRequestHandler<CreateTeamCommand, Result<TeamDto>>
{
    private readonly IApplicationDbContext _db;
    public CreateTeamHandler(IApplicationDbContext db) => _db = db;
    public async Task<Result<TeamDto>> Handle(CreateTeamCommand req, CancellationToken ct)
    {
        var proj = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, ct);
        if (proj == null) return Result<TeamDto>.Failure("Project not found");
        var exists = await _db.Teams.AnyAsync(t => t.ProjectId == req.ProjectId && t.Name == req.Name, ct);
        if (exists) return Result<TeamDto>.Failure("Team name already exists in this project");
        var team = new Domain.Entities.Team(req.ProjectId, req.Name, req.Description);
        _db.Teams.Add(team);
        await _db.SaveChangesAsync(ct);
        return Result<TeamDto>.Success(new TeamDto(team.Id, team.ProjectId, team.Name, team.Description, team.CreatedAt, 0));
    }
}

public class UpdateTeamHandler : IRequestHandler<UpdateTeamCommand, Result<TeamDto>>
{
    private readonly IApplicationDbContext _db;
    public UpdateTeamHandler(IApplicationDbContext db) => _db = db;
    public async Task<Result<TeamDto>> Handle(UpdateTeamCommand req, CancellationToken ct)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == req.TeamId, ct);
        if (team == null) return Result<TeamDto>.Failure("Team not found");
        team.Update(req.Name, req.Description);
        await _db.SaveChangesAsync(ct);
        var count = await _db.TeamMembers.CountAsync(m => m.TeamId == team.Id, ct);
        return Result<TeamDto>.Success(new TeamDto(team.Id, team.ProjectId, team.Name, team.Description, team.CreatedAt, count));
    }
}

public class DeleteTeamHandler : IRequestHandler<DeleteTeamCommand, Result<object>>
{
    private readonly IApplicationDbContext _db;
    public DeleteTeamHandler(IApplicationDbContext db) => _db = db;
    public async Task<Result<object>> Handle(DeleteTeamCommand req, CancellationToken ct)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == req.TeamId, ct);
        if (team == null) return Result<object>.Failure("Team not found");
        _db.Teams.Remove(team);
        await _db.SaveChangesAsync(ct);
        return Result<object>.Success(new { message = "Deleted" });
    }
}

public class AddTeamMemberHandler : IRequestHandler<AddTeamMemberCommand, Result<TeamMemberDto>>
{
    private readonly IApplicationDbContext _db;
    public AddTeamMemberHandler(IApplicationDbContext db) => _db = db;
    public async Task<Result<TeamMemberDto>> Handle(AddTeamMemberCommand req, CancellationToken ct)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == req.TeamId, ct);
        if (team == null) return Result<TeamMemberDto>.Failure("Team not found");
        var exists = await _db.TeamMembers.AnyAsync(m => m.TeamId == req.TeamId && m.UserId == req.UserId, ct);
        if (exists) return Result<TeamMemberDto>.Failure("Already member");
        var member = new Domain.Entities.TeamMember(req.TeamId, req.UserId);
        _db.TeamMembers.Add(member);
        await _db.SaveChangesAsync(ct);
        return Result<TeamMemberDto>.Success(new TeamMemberDto(member.Id, member.TeamId, member.UserId, member.JoinedAt));
    }
}

public class RemoveTeamMemberHandler : IRequestHandler<RemoveTeamMemberCommand, Result<object>>
{
    private readonly IApplicationDbContext _db;
    public RemoveTeamMemberHandler(IApplicationDbContext db) => _db = db;
    public async Task<Result<object>> Handle(RemoveTeamMemberCommand req, CancellationToken ct)
    {
        var member = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == req.TeamId && m.UserId == req.UserId, ct);
        if (member == null) return Result<object>.Failure("Member not found");
        _db.TeamMembers.Remove(member);
        await _db.SaveChangesAsync(ct);
        return Result<object>.Success(new { message = "Removed" });
    }
}
