using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Commands;

/// <summary>
/// DeleteProject - only OrgAdmin/SuperAdmin can delete. Cascades lists/tasks via FK.
/// </summary>
public record DeleteProjectCommand(Guid ProjectId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<bool>>;

public class DeleteProjectHandler : IRequestHandler<DeleteProjectCommand, Result<bool>>
{
    private readonly IProjectService _service;
    public DeleteProjectHandler(IProjectService service) => _service = service;
    public Task<Result<bool>> Handle(DeleteProjectCommand req, CancellationToken ct)
        => _service.DeleteProjectAsync(req.ProjectId, req.CallerId, req.CallerRoles, ct);
}
