using File.Service.Application.DTOs;
using SharedKernel;

namespace File.Service.Application.Interfaces;

public interface IFileService
{
    Task<Result<AttachmentDto>> UploadAsync(Guid taskId, string fileName, string contentType, long sizeBytes, Stream fileStream, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid attachmentId, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<List<AttachmentDto>> GetByTaskAsync(Guid taskId, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
    Task<bool> IsTaskAccessibleAsync(Guid taskId, Guid callerId, List<string> callerRoles, CancellationToken ct = default);
}

public interface ICloudinaryService
{
    Task<(string Url, string PublicId)> UploadAsync(string fileName, Stream fileStream, string contentType, string folder, CancellationToken ct = default);
    Task DeleteAsync(string publicId, CancellationToken ct = default);
}
