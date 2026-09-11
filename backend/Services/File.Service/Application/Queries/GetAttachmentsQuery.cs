using MediatR;
using File.Service.Application.DTOs;
using File.Service.Application.Interfaces;

namespace File.Service.Application.Queries;

public record GetAttachmentsQuery(Guid TaskId, Guid CallerId, List<string> CallerRoles) : IRequest<List<AttachmentDto>>;

public class GetAttachmentsQueryHandler : IRequestHandler<GetAttachmentsQuery, List<AttachmentDto>>
{
    private readonly IFileService _fileService;
    public GetAttachmentsQueryHandler(IFileService fileService) => _fileService = fileService;

    public async Task<List<AttachmentDto>> Handle(GetAttachmentsQuery request, CancellationToken ct)
    {
        return await _fileService.GetByTaskAsync(request.TaskId, request.CallerId, request.CallerRoles, ct);
    }
}
