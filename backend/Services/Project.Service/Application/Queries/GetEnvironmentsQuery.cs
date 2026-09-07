using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

public record GetEnvironmentsQuery(Guid ProjectId) : IRequest<List<ProjectEnvironmentDto>>;

public class GetEnvironmentsHandler : IRequestHandler<GetEnvironmentsQuery, List<ProjectEnvironmentDto>>
{
    private readonly IEnvironmentService _service;
    public GetEnvironmentsHandler(IEnvironmentService service) => _service = service;
    public Task<List<ProjectEnvironmentDto>> Handle(GetEnvironmentsQuery req, CancellationToken ct)
        => _service.GetEnvironmentsAsync(req.ProjectId, ct);
}
