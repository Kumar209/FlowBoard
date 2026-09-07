using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;
using SharedKernel;

namespace Project.Service.Infrastructure.Services;

public class SprintService : ISprintService
{
    private readonly IApplicationDbContext _db;
    public SprintService(IApplicationDbContext db) => _db = db;

    public async Task<Result<SprintDto>> CreateSprintAsync(Guid projectId, Guid? boardId, string name, DateTime startDate, DateTime endDate, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Viewer") || callerRoles.Contains("Client")) return Result<SprintDto>.Failure("Forbidden - Viewer/Client cannot create sprints");
        if (boardId != null && boardId != Guid.Empty)
        {
            var board = await _db.Boards.FindAsync(new object[] { boardId }, ct);
            if (board == null) return Result<SprintDto>.Failure("Board not found");
            if (board.ProjectId != projectId) return Result<SprintDto>.Failure("Board does not belong to project");
        }
        var sprint = new Domain.Entities.Sprint(projectId, boardId, name, startDate, endDate, "Planned");
        _db.Sprints.Add(sprint);
        await _db.SaveChangesAsync(ct);
        _db.ActivityLogs.Add(new Domain.Entities.ActivityLog(projectId, null, callerId, "SprintCreated", $"{{\"name\":\"{name}\"}}"));
        await _db.SaveChangesAsync(ct);
        return Result<SprintDto>.Success(new SprintDto(sprint.Id, sprint.ProjectId, sprint.BoardId, sprint.Name, sprint.StartDate, sprint.EndDate, sprint.Status, sprint.CreatedAt));
    }

    public async Task<Result<SprintDto>> UpdateSprintAsync(Guid sprintId, string name, DateTime startDate, DateTime endDate, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Viewer") || callerRoles.Contains("Client")) return Result<SprintDto>.Failure("Forbidden - Viewer/Client cannot update sprints");
        var sprint = await _db.Sprints.FindAsync(new object[] { sprintId }, ct);
        if (sprint == null) return Result<SprintDto>.Failure("Sprint not found");
        sprint.Update(name, startDate, endDate);
        await _db.SaveChangesAsync(ct);
        return Result<SprintDto>.Success(new SprintDto(sprint.Id, sprint.ProjectId, sprint.BoardId, sprint.Name, sprint.StartDate, sprint.EndDate, sprint.Status, sprint.CreatedAt));
    }

    public async Task<Result<bool>> DeleteSprintAsync(Guid sprintId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Viewer") || callerRoles.Contains("Client")) return Result<bool>.Failure("Forbidden - Viewer/Client cannot delete sprints");
        var sprint = await _db.Sprints.FindAsync(new object[] { sprintId }, ct);
        if (sprint == null) return Result<bool>.Failure("Sprint not found");
        _db.Sprints.Remove(sprint);
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<List<SprintDto>> GetSprintsAsync(Guid projectId, Guid? boardId, CancellationToken ct = default)
    {
        var q = _db.Sprints.Where(s => s.ProjectId == projectId);
        if (boardId != null && boardId != Guid.Empty) q = q.Where(s => s.BoardId == boardId);
        return await q.OrderBy(s => s.StartDate).Select(s => new SprintDto(s.Id, s.ProjectId, s.BoardId, s.Name, s.StartDate, s.EndDate, s.Status, s.CreatedAt)).ToListAsync(ct);
    }
}

public class TeamService : ITeamService
{
    private readonly IApplicationDbContext _db;
    public TeamService(IApplicationDbContext db) => _db = db;

    public async Task<Result<TeamDto>> CreateTeamAsync(Guid projectId, string name, string? description, Guid callerId, CancellationToken ct = default)
    {
        var proj = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (proj == null) return Result<TeamDto>.Failure("Project not found");
        var exists = await _db.Teams.AnyAsync(t => t.ProjectId == projectId && t.Name == name, ct);
        if (exists) return Result<TeamDto>.Failure("Team name already exists in this project");
        var team = new Domain.Entities.Team(projectId, name, description);
        _db.Teams.Add(team);
        await _db.SaveChangesAsync(ct);
        return Result<TeamDto>.Success(new TeamDto(team.Id, team.ProjectId, team.Name, team.Description, team.CreatedAt, 0));
    }

    public async Task<Result<TeamDto>> UpdateTeamAsync(Guid teamId, string name, string? description, Guid callerId, CancellationToken ct = default)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, ct);
        if (team == null) return Result<TeamDto>.Failure("Team not found");
        team.Update(name, description);
        await _db.SaveChangesAsync(ct);
        var count = await _db.TeamMembers.CountAsync(m => m.TeamId == team.Id, ct);
        return Result<TeamDto>.Success(new TeamDto(team.Id, team.ProjectId, team.Name, team.Description, team.CreatedAt, count));
    }

    public async Task<Result> DeleteTeamAsync(Guid teamId, Guid callerId, CancellationToken ct = default)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, ct);
        if (team == null) return Result.Failure("Team not found");
        _db.Teams.Remove(team);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<List<TeamDto>> GetTeamsAsync(Guid projectId, CancellationToken ct = default)
    {
        var teams = await _db.Teams.Where(t => t.ProjectId == projectId).OrderBy(t => t.CreatedAt).ToListAsync(ct);
        var result = new List<TeamDto>();
        foreach (var t in teams)
        {
            var count = await _db.TeamMembers.CountAsync(m => m.TeamId == t.Id, ct);
            result.Add(new TeamDto(t.Id, t.ProjectId, t.Name, t.Description, t.CreatedAt, count));
        }
        return result;
    }

    public async Task<Result<TeamMemberDto>> AddMemberAsync(Guid teamId, Guid userId, Guid callerId, CancellationToken ct = default)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, ct);
        if (team == null) return Result<TeamMemberDto>.Failure("Team not found");
        var exists = await _db.TeamMembers.AnyAsync(m => m.TeamId == teamId && m.UserId == userId, ct);
        if (exists) return Result<TeamMemberDto>.Failure("Already member");
        var member = new Domain.Entities.TeamMember(teamId, userId);
        _db.TeamMembers.Add(member);
        await _db.SaveChangesAsync(ct);
        return Result<TeamMemberDto>.Success(new TeamMemberDto(member.Id, member.TeamId, member.UserId, member.JoinedAt));
    }

    public async Task<Result> RemoveMemberAsync(Guid teamId, Guid userId, Guid callerId, CancellationToken ct = default)
    {
        var member = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId, ct);
        if (member == null) return Result.Failure("Member not found");
        _db.TeamMembers.Remove(member);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<List<TeamMemberDto>> GetMembersAsync(Guid teamId, CancellationToken ct = default)
        => await _db.TeamMembers.Where(m => m.TeamId == teamId).OrderBy(m => m.JoinedAt).Select(m => new TeamMemberDto(m.Id, m.TeamId, m.UserId, m.JoinedAt)).ToListAsync(ct);
}

public class EnvironmentService : IEnvironmentService
{
    private readonly IApplicationDbContext _db;
    public EnvironmentService(IApplicationDbContext db) => _db = db;

    public async Task<Result<ProjectEnvironmentDto>> CreateEnvironmentAsync(Guid projectId, string name, string url, string? description, string status, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Viewer") || callerRoles.Contains("Client")) return Result<ProjectEnvironmentDto>.Failure("Forbidden - Viewer/Client cannot create environments");
        var exists = await _db.Environments.AnyAsync(e => e.ProjectId == projectId && e.Name == name, ct);
        if (exists) return Result<ProjectEnvironmentDto>.Failure("Environment with same name already exists");
        var env = new Domain.Entities.ProjectEnvironment(projectId, name, url, description, status ?? "Active");
        _db.Environments.Add(env);
        await _db.SaveChangesAsync(ct);
        return Result<ProjectEnvironmentDto>.Success(new ProjectEnvironmentDto(env.Id, env.ProjectId, env.Name, env.Url, env.Description, env.Status, env.CreatedAt));
    }

    public async Task<Result<ProjectEnvironmentDto>> UpdateEnvironmentAsync(Guid environmentId, string name, string url, string? description, string status, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Viewer") || callerRoles.Contains("Client")) return Result<ProjectEnvironmentDto>.Failure("Forbidden - Viewer/Client cannot update environments");
        var env = await _db.Environments.FindAsync(new object[] { environmentId }, ct);
        if (env == null) return Result<ProjectEnvironmentDto>.Failure("Environment not found");
        env.Update(name, url, description, status ?? "Active");
        await _db.SaveChangesAsync(ct);
        return Result<ProjectEnvironmentDto>.Success(new ProjectEnvironmentDto(env.Id, env.ProjectId, env.Name, env.Url, env.Description, env.Status, env.CreatedAt));
    }

    public async Task<Result<bool>> DeleteEnvironmentAsync(Guid environmentId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        if (callerRoles.Contains("Viewer") || callerRoles.Contains("Client")) return Result<bool>.Failure("Forbidden - Viewer/Client cannot delete environments");
        var env = await _db.Environments.FindAsync(new object[] { environmentId }, ct);
        if (env == null) return Result<bool>.Failure("Environment not found");
        _db.Environments.Remove(env);
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<List<ProjectEnvironmentDto>> GetEnvironmentsAsync(Guid projectId, CancellationToken ct = default)
        => await _db.Environments.Where(e => e.ProjectId == projectId).OrderBy(e => e.CreatedAt).Select(e => new ProjectEnvironmentDto(e.Id, e.ProjectId, e.Name, e.Url, e.Description, e.Status, e.CreatedAt)).ToListAsync(ct);
}
