using FluentValidation;
using MediatR;
using File.Service.Application.DTOs;
using File.Service.Application.Interfaces;
using SharedKernel;

namespace File.Service.Application.Commands;

public record UploadAttachmentCommand(
    Guid TaskId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream FileStream,
    Guid CallerId,
    List<string> CallerRoles) : IRequest<Result<AttachmentDto>>;

public class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    public UploadAttachmentCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SizeBytes).GreaterThan(0).LessThanOrEqualTo(10 * 1024 * 1024);
        RuleFor(x => x.CallerId).NotEmpty();
    }
}

public class UploadAttachmentCommandHandler : IRequestHandler<UploadAttachmentCommand, Result<AttachmentDto>>
{
    private readonly IFileService _fileService;
    public UploadAttachmentCommandHandler(IFileService fileService) => _fileService = fileService;

    public async Task<Result<AttachmentDto>> Handle(UploadAttachmentCommand request, CancellationToken ct)
    {
        return await _fileService.UploadAsync(request.TaskId, request.FileName, request.ContentType, request.SizeBytes, request.FileStream, request.CallerId, request.CallerRoles, ct);
    }
}
