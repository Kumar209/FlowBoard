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
    private readonly IEnvironmentService _service;
    public CreateEnvironmentHandler(IEnvironmentService service) => _service = service;
    public Task<Result<ProjectEnvironmentDto>> Handle(CreateEnvironmentCommand req, CancellationToken ct)
        => _service.CreateEnvironmentAsync(req.ProjectId, req.Name, req.Url, req.Description, req.Status, req.CallerId, req.CallerRoles, ct);
}
public class UpdateEnvironmentHandler : IRequestHandler<UpdateEnvironmentCommand, Result<ProjectEnvironmentDto>>
{
    private readonly IEnvironmentService _service;
    public UpdateEnvironmentHandler(IEnvironmentService service) => _service = service;
    public Task<Result<ProjectEnvironmentDto>> Handle(UpdateEnvironmentCommand req, CancellationToken ct)
        => _service.UpdateEnvironmentAsync(req.EnvironmentId, req.Name, req.Url, req.Description, req.Status, req.CallerId, req.CallerRoles, ct);
}
public class DeleteEnvironmentHandler : IRequestHandler<DeleteEnvironmentCommand, Result<bool>>
{
    private readonly IEnvironmentService _service;
    public DeleteEnvironmentHandler(IEnvironmentService service) => _service = service;
    public Task<Result<bool>> Handle(DeleteEnvironmentCommand req, CancellationToken ct)
        => _service.DeleteEnvironmentAsync(req.EnvironmentId, req.CallerId, req.CallerRoles, ct);
}
