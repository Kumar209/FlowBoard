namespace File.Service.Application.DTOs;

public record AttachmentDto(
    Guid Id,
    Guid ProjectId,
    Guid TaskId,
    Guid UploaderId,
    string FileName,
    string Url,
    string PublicId,
    string ContentType,
    long SizeBytes,
    DateTime CreatedAt);

public record PaginatedResult<T>(List<T> Items, int Total, int Page, int PageSize);
