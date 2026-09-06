using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

public record CreateEnvironmentCommand(Guid ProjectId, string Name, string Url, string? Description, string Status, Guid CallerId, List<string> CallerRoles) : IRequest<Result<ProjectEnvironmentDto>>;
public record UpdateEnvironmentCommand(Guid EnvironmentId, string Name, string Url, string? Description, string Status, Guid CallerId, List<string> CallerRoles) : IRequest<Result<ProjectEnvironmentDto>>;
public record DeleteEnvironmentCommand(Guid EnvironmentId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<bool>>;

public class CreateEnvironmentValidator : AbstractValidator<CreateEnvironmentCommand>
{
    public CreateEnvironmentValidator() { RuleFor(x => x.ProjectId).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(100); RuleFor(x => x.Url).MaximumLength(500).When(x=>!string.IsNullOrEmpty(x.Url)); RuleFor(x => x.Status).Must(s=> new[]{"Active","Inactive","Maintenance"}.Contains(s)).When(x=>!string.IsNullOrEmpty(x.Status)); }
}
public class UpdateEnvironmentValidator : AbstractValidator<UpdateEnvironmentCommand>
{
    public UpdateEnvironmentValidator() { RuleFor(x => x.EnvironmentId).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(100); RuleFor(x => x.Url).MaximumLength(500).When(x=>!string.IsNullOrEmpty(x.Url)); }
}

public class CreateEnvironmentHandler : IRequestHandler<CreateEnvironmentCommand, Result<ProjectEnvironmentDto>>
{
    private readonly IApplicationDbContext _db;
    public CreateEnvironmentHandler(IApplicationDbContext db) => _db = db;
    public async Task<Result<ProjectEnvironmentDto>> Handle(CreateEnvironmentCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Viewer") || req.CallerRoles.Contains("Client"))
            return Result<ProjectEnvironmentDto>.Failure("Forbidden - Viewer/Client cannot create environments");
        var exists = await _db.Environments.AnyAsync(e => e.ProjectId == req.ProjectId && e.Name == req.Name, ct);
        if (exists) return Result<ProjectEnvironmentDto>.Failure("Environment with same name already exists");
        var env = new Domain.Entities.ProjectEnvironment(req.ProjectId, req.Name, req.Url, req.Description, req.Status ?? "Active");
        _db.Environments.Add(env);
        await _db.SaveChangesAsync(ct);
        return Result<ProjectEnvironmentDto>.Success(new ProjectEnvironmentDto(env.Id, env.ProjectId, env.Name, env.Url, env.Description, env.Status, env.CreatedAt));
    }
}
public class UpdateEnvironmentHandler : IRequestHandler<UpdateEnvironmentCommand, Result<ProjectEnvironmentDto>>
{
    private readonly IApplicationDbContext _db;
    public UpdateEnvironmentHandler(IApplicationDbContext db) => _db = db;
    public async Task<Result<ProjectEnvironmentDto>> Handle(UpdateEnvironmentCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Viewer") || req.CallerRoles.Contains("Client"))
            return Result<ProjectEnvironmentDto>.Failure("Forbidden - Viewer/Client cannot update environments");
        var env = await _db.Environments.FindAsync(new object[]{ req.EnvironmentId }, ct);
        if (env == null) return Result<ProjectEnvironmentDto>.Failure("Environment not found");
        env.Update(req.Name, req.Url, req.Description, req.Status ?? "Active");
        await _db.SaveChangesAsync(ct);
        return Result<ProjectEnvironmentDto>.Success(new ProjectEnvironmentDto(env.Id, env.ProjectId, env.Name, env.Url, env.Description, env.Status, env.CreatedAt));
    }
}
public class DeleteEnvironmentHandler : IRequestHandler<DeleteEnvironmentCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    public DeleteEnvironmentHandler(IApplicationDbContext db) => _db = db;
    public async Task<Result<bool>> Handle(DeleteEnvironmentCommand req, CancellationToken ct)
    {
        if (req.CallerRoles.Contains("Viewer") || req.CallerRoles.Contains("Client"))
            return Result<bool>.Failure("Forbidden - Viewer/Client cannot delete environments");
        var env = await _db.Environments.FindAsync(new object[]{ req.EnvironmentId }, ct);
        if (env == null) return Result<bool>.Failure("Environment not found");
        _db.Environments.Remove(env);
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
