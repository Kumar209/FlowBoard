using FluentValidation;
using MediatR;
using File.Service.Application.Interfaces;
using SharedKernel;

namespace File.Service.Application.Commands;

public record DeleteAttachmentCommand(Guid AttachmentId, Guid CallerId, List<string> CallerRoles) : IRequest<Result<bool>>;

public class DeleteAttachmentCommandValidator : AbstractValidator<DeleteAttachmentCommand>
{
    public DeleteAttachmentCommandValidator()
    {
        RuleFor(x => x.AttachmentId).NotEmpty();
        RuleFor(x => x.CallerId).NotEmpty();
    }
}

public class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand, Result<bool>>
{
    private readonly IFileService _fileService;
    public DeleteAttachmentCommandHandler(IFileService fileService) => _fileService = fileService;

    public async Task<Result<bool>> Handle(DeleteAttachmentCommand request, CancellationToken ct)
    {
        return await _fileService.DeleteAsync(request.AttachmentId, request.CallerId, request.CallerRoles, ct);
    }
}
