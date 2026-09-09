using MediatR;
using Project.Service.Application.DTOs;
using Project.Service.Application.Interfaces;

namespace Project.Service.Application.Queries;

public record GetAssigneeCandidatesQuery(Guid ProjectId) : IRequest<List<ProjectMemberDto>>;

public class GetAssigneeCandidatesHandler : IRequestHandler<GetAssigneeCandidatesQuery, List<ProjectMemberDto>>
{
    private readonly IProjectMemberService _service;
    public GetAssigneeCandidatesHandler(IProjectMemberService service) => _service = service;
    public Task<List<ProjectMemberDto>> Handle(GetAssigneeCandidatesQuery req, CancellationToken ct)
        => _service.GetAssigneeCandidatesAsync(req.ProjectId, ct);
}
