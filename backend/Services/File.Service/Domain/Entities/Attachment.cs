using SharedKernel;

namespace File.Service.Domain.Entities;

/// <summary>
/// Attachment - file uploaded to Cloudinary linked to a Task. Stored in [file] schema.
/// Folder: flowboard/{workspaceId}/{projectId}/{taskId} with eager w_300 thumb.
/// </summary>
public class Attachment : BaseEntity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid UploaderId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty; // secure_url
    public string PublicId { get; private set; } = string.Empty; // for delete
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid OrganizationId { get; private set; }

    private Attachment() { }

    public Attachment(Guid projectId, Guid taskId, Guid uploaderId, string fileName, string url, string publicId, string contentType, long sizeBytes, Guid workspaceId, Guid organizationId)
    {
        ProjectId = projectId;
        TaskId = taskId;
        UploaderId = uploaderId;
        FileName = fileName;
        Url = url;
        PublicId = publicId;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        WorkspaceId = workspaceId;
        OrganizationId = organizationId;
    }
}
