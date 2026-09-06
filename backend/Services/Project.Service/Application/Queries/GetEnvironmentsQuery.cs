using MediatR;
using Microsoft.EntityFrameworkCore;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

public record GetEnvironmentsQuery(Guid ProjectId) : IRequest<List<ProjectEnvironmentDto>>;

public class GetEnvironmentsHandler : IRequestHandler<GetEnvironmentsQuery, List<ProjectEnvironmentDto>>
{
    private readonly IApplicationDbContext _db;
    public GetEnvironmentsHandler(IApplicationDbContext db) => _db = db;
    public async Task<List<ProjectEnvironmentDto>> Handle(GetEnvironmentsQuery req, CancellationToken ct)
        => await _db.Environments.Where(e => e.ProjectId == req.ProjectId).OrderBy(e => e.Name)
            .Select(e => new ProjectEnvironmentDto(e.Id, e.ProjectId, e.Name, e.Url, e.Description, e.Status, e.CreatedAt))
            .ToListAsync(ct);
}
