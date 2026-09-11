using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using File.Service.Application.DTOs;
using File.Service.Application.Interfaces;
using File.Service.Domain.Entities;
using SharedKernel;

namespace File.Service.Infrastructure.Services;

public class FileService : IFileService
{
    private readonly IApplicationDbContext _db;
    private readonly ICloudinaryService _cloudinary;

    // Allowed mime types - MNC strict whitelist (image + pdf + text + zip)
    private static readonly HashSet<string> AllowedMimePrefixes = new() { "image/", "video/", "application/pdf", "text/", "application/zip", "application/msword", "application/vnd." };
    private static readonly HashSet<string> AllowedExtensions = new() { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".pdf", ".txt", ".csv", ".zip", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".mp4", ".mov" };
    private const long MaxSizeBytes = 10 * 1024 * 1024; // 10MB

    public FileService(IApplicationDbContext db, ICloudinaryService cloudinary)
    {
        _db = db;
        _cloudinary = cloudinary;
    }

    public async Task<Result<AttachmentDto>> UploadAsync(Guid taskId, string fileName, string contentType, long sizeBytes, Stream fileStream, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        // 1. File validation
        if (string.IsNullOrWhiteSpace(fileName)) return Result<AttachmentDto>.Failure("FileName required");
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext) && !AllowedMimePrefixes.Any(p => contentType.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return Result<AttachmentDto>.Failure($"File type not allowed: {contentType} ({ext})");
        if (sizeBytes > MaxSizeBytes) return Result<AttachmentDto>.Failure($"File too large: {sizeBytes} bytes > 10MB");
        if (fileStream.Length == 0) return Result<AttachmentDto>.Failure("Empty file");

        // 2. Resolve task -> project -> workspace -> org with permission checks
        var resolve = await ResolveAndAuthorizeAsync(taskId, callerId, callerRoles, "attachment:create", ct);
        if (!resolve.IsSuccess) return Result<AttachmentDto>.Failure(resolve.Error!);
        var (projectId, workspaceId, orgId) = resolve.Value;

        // 3. Client cannot upload (strict) - Viewer is now custom role, blocked via RolePermissions if no permission
        if (Roles.CanUpload(callerRoles) == false)
            return Result<AttachmentDto>.Failure("Forbidden - Client cannot upload attachments");

        // 4. Cloudinary upload — short ids to keep public_id <255 (MNC) + sanitized truncated filename (fixes VS break on long public_id)
        var folder = $"flowboard/{orgId.ToString()[..8]}/{workspaceId.ToString()[..8]}/{projectId.ToString()[..8]}/{taskId.ToString()[..8]}";
        var safeFileName = Path.GetFileNameWithoutExtension(fileName);
        safeFileName = System.Text.RegularExpressions.Regex.Replace(safeFileName, @"[^a-zA-Z0-9_\-]", "_");
        if (safeFileName.Length > 40) safeFileName = safeFileName[..40];
        safeFileName += Path.GetExtension(fileName);
        string url, publicId;
        try
        {
            (url, publicId) = await _cloudinary.UploadAsync(safeFileName, fileStream, contentType, folder, ct);
        }
        catch (Exception ex)
        {
            return Result<AttachmentDto>.Failure($"Cloudinary upload failed: {ex.Message}");
        }

        // 5. Persist attachment + outbox in same txn
        var attachment = new Attachment(projectId, taskId, callerId, fileName, url, publicId, contentType, sizeBytes, workspaceId, orgId);
        _db.Attachments.Add(attachment);
        // Activity for History tab — issue-level
        try {
            var projWs = await _db.Database.SqlQueryRaw<GuidRow>("SELECT WorkspaceId as Value FROM [project].[Projects] WHERE Id = {0}", projectId).FirstOrDefaultAsync(ct);
            var wsIdForLog = projWs?.Value ?? workspaceId;
            _db.Database.ExecuteSqlRaw("INSERT INTO [project].[ActivityLogs] (Id, ProjectId, TaskId, ActorId, Action, PayloadJson, OccurredAt, CreatedAt, UpdatedAt, WorkspaceId) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {6}, {6}, {7})",
                Guid.NewGuid(), projectId, taskId, callerId, "AttachmentUploaded", JsonSerializer.Serialize(new { fileName, url, publicId, contentType }), DateTime.UtcNow, wsIdForLog);
        } catch { /* best-effort, no FK for TaskId */ }
        var evt = new
        {
            AttachmentId = attachment.Id,
            TaskId = taskId,
            ProjectId = projectId,
            WorkspaceId = workspaceId,
            OrganizationId = orgId,
            UploaderId = callerId,
            FileName = fileName,
            Url = url,
            PublicId = publicId,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            OccurredOnUtc = DateTime.UtcNow,
            EventId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid().ToString()
        };
        _db.OutboxMessages.Add(new OutboxMessage("FileUploaded", JsonSerializer.Serialize(evt)));
        await _db.SaveChangesAsync(ct);

        var dto = new AttachmentDto(attachment.Id, projectId, taskId, callerId, fileName, url, publicId, contentType, sizeBytes, attachment.CreatedAt);
        return Result<AttachmentDto>.Success(dto);
    }

    public async Task<Result<bool>> DeleteAsync(Guid attachmentId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        var attachment = await _db.Attachments.FirstOrDefaultAsync(a => a.Id == attachmentId, ct);
        if (attachment == null) return Result<bool>.Failure("Attachment not found");

        // Resolve task auth for this attachment's task
        var resolve = await ResolveAndAuthorizeAsync(attachment.TaskId, callerId, callerRoles, "attachment:delete", ct);
        if (!resolve.IsSuccess) return Result<bool>.Failure(resolve.Error!);

        // Only uploader or OrgAdmin/SuperAdmin can delete (ProjectManager is now custom workspace role)
        bool isUploader = attachment.UploaderId == callerId;
        bool isPrivileged = Roles.IsPrivilegedForManage(callerRoles);
        // Also check SuperAdmin via identity
        if (!isUploader && !isPrivileged)
        {
            try
            {
                var isSuper = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[WorkspaceMembers] WHERE UserId = {0} AND Role = {1}", callerId, Roles.SuperAdminValue).FirstOrDefaultAsync(ct) > 0;
                if (isSuper) isPrivileged = true;
            } catch { }
        }
        if (!isUploader && !isPrivileged)
            return Result<bool>.Failure("Forbidden - Only uploader or OrgAdmin/SuperAdmin can delete");

        try { await _cloudinary.DeleteAsync(attachment.PublicId, ct); } catch { /* best-effort, still delete DB */ }

        _db.Attachments.Remove(attachment);
        try {
            var projWsDel = await _db.Database.SqlQueryRaw<GuidRow>("SELECT WorkspaceId as Value FROM [project].[Projects] WHERE Id = {0}", attachment.ProjectId).FirstOrDefaultAsync(ct);
            var wsIdDel = projWsDel?.Value ?? attachment.WorkspaceId;
            _db.Database.ExecuteSqlRaw("INSERT INTO [project].[ActivityLogs] (Id, ProjectId, TaskId, ActorId, Action, PayloadJson, OccurredAt, CreatedAt, UpdatedAt, WorkspaceId) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {6}, {6}, {7})",
                Guid.NewGuid(), attachment.ProjectId, attachment.TaskId, callerId, "AttachmentDeleted", JsonSerializer.Serialize(new { fileName = attachment.FileName, publicId = attachment.PublicId }), DateTime.UtcNow, wsIdDel);
        } catch { }
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<List<AttachmentDto>> GetByTaskAsync(Guid taskId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        var resolve = await ResolveAndAuthorizeAsync(taskId, callerId, callerRoles, "attachment:view", ct);
        if (!resolve.IsSuccess) throw new UnauthorizedAccessException(resolve.Error!);

        var list = await _db.Attachments.Where(a => a.TaskId == taskId).OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
        return list.Select(a => new AttachmentDto(a.Id, a.ProjectId, a.TaskId, a.UploaderId, a.FileName, a.Url, a.PublicId, a.ContentType, a.SizeBytes, a.CreatedAt)).ToList();
    }

    public async Task<bool> IsTaskAccessibleAsync(Guid taskId, Guid callerId, List<string> callerRoles, CancellationToken ct = default)
    {
        var r = await ResolveAndAuthorizeAsync(taskId, callerId, callerRoles, "attachment:view", ct);
        return r.IsSuccess;
    }

    // Central authorization: verifies org/workspace/project membership + permission + callerRoles
    private async Task<Result<(Guid ProjectId, Guid WorkspaceId, Guid OrgId)>> ResolveAndAuthorizeAsync(Guid taskId, Guid callerId, List<string> callerRoles, string permKey, CancellationToken ct)
    {
        // 1. Resolve task -> project -> workspace -> org (same DB, cross-schema via raw SQL)
        Guid projectId = Guid.Empty, workspaceId = Guid.Empty, orgId = Guid.Empty;
        try
        {
            var taskRow = await _db.Database.SqlQueryRaw<TaskRow>("SELECT Id, ProjectId, ListId FROM [project].[Tasks] WHERE Id = {0}", taskId).FirstOrDefaultAsync(ct);
            if (taskRow == null) return Result<(Guid, Guid, Guid)>.Failure("Task not found");
            projectId = taskRow.ProjectId;
            var projRow = await _db.Database.SqlQueryRaw<ProjectRow>("SELECT Id, WorkspaceId FROM [project].[Projects] WHERE Id = {0}", projectId).FirstOrDefaultAsync(ct);
            if (projRow == null) return Result<(Guid, Guid, Guid)>.Failure("Project not found for task");
            workspaceId = projRow.WorkspaceId;
            var wsRow = await _db.Database.SqlQueryRaw<GuidRow>("SELECT OrganizationId as Value FROM [identity].[Workspaces] WHERE Id = {0}", workspaceId).FirstOrDefaultAsync(ct);
            if (wsRow == null || wsRow.Value == Guid.Empty) return Result<(Guid, Guid, Guid)>.Failure("Workspace/Organization not found");
            orgId = wsRow.Value;
        }
        catch (Exception ex)
        {
            return Result<(Guid, Guid, Guid)>.Failure($"Resolve failed: {ex.Message}");
        }

        // 2. SuperAdmin bypass (Users.IsSuperAdmin or WorkspaceMembers Role=5)
        try
        {
            var isSuperClaim = callerRoles.Contains(Roles.SuperAdmin);
            if (isSuperClaim) return Result<(Guid, Guid, Guid)>.Success((projectId, workspaceId, orgId));
            var isSuperDb = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[WorkspaceMembers] WHERE UserId = {0} AND Role = {1}", callerId, Roles.SuperAdminValue).FirstOrDefaultAsync(ct) > 0;
            if (isSuperDb) return Result<(Guid, Guid, Guid)>.Success((projectId, workspaceId, orgId));
        } catch { }

        // 3. Org membership check (OrganizationMembers 0/2/3 + OwnerId)
        bool isOrgMember = false;
        try
        {
            var orgMemberCount = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[OrganizationMembers] WHERE OrganizationId = {0} AND UserId = {1}", orgId, callerId).FirstOrDefaultAsync(ct);
            if (orgMemberCount > 0) isOrgMember = true;
            else
            {
                var isOwner = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[Organizations] WHERE Id = {0} AND OwnerId = {1}", orgId, callerId).FirstOrDefaultAsync(ct) > 0;
                if (isOwner) isOrgMember = true;
            }
        } catch { }
        if (!isOrgMember) return Result<(Guid, Guid, Guid)>.Failure("Forbidden - Not an organization member");

        // 4. Workspace membership check
        bool isWsMember = false;
        try { isWsMember = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0} AND UserId = {1}", workspaceId, callerId).FirstOrDefaultAsync(ct) > 0; } catch { }
        if (!isWsMember) return Result<(Guid, Guid, Guid)>.Failure("Forbidden - Not a workspace member");

        // 5. Project membership check (ProjectMembers) — OrgAdmin/SuperAdmin have full authority even if not explicit project member
        bool isProjMember = false;
        try { isProjMember = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [project].[ProjectMembers] WHERE ProjectId = {0} AND UserId = {1}", projectId, callerId).FirstOrDefaultAsync(ct) > 0; } catch { }
        bool isPrivilegedForProject = Roles.IsPrivilegedForManage(callerRoles);
        if (!isProjMember && !isPrivilegedForProject)
        {
            try {
                var isOrgAdminDb = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[OrganizationMembers] WHERE OrganizationId = {0} AND UserId = {1} AND Role = {2}", orgId, callerId, Roles.OrgAdminValue).FirstOrDefaultAsync(ct) > 0;
                if (isOrgAdminDb) isPrivilegedForProject = true;
                var isOwnerDb = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[Organizations] WHERE Id = {0} AND OwnerId = {1}", orgId, callerId).FirstOrDefaultAsync(ct) > 0;
                if (isOwnerDb) isPrivilegedForProject = true;
            } catch { }
        }
        if (!isProjMember && !isPrivilegedForProject)
        {
            return Result<(Guid, Guid, Guid)>.Failure("Forbidden - You are not a member of this project. Only project team members and OrgAdmin can manage attachments.");
        }

        // 6. Permission check via RolePermissions (if custom role exists)
        // For attachment:view we allow all project members; for create/delete check RolePermissions if custom role assigned
        if (permKey != "attachment:view")
        {
            try
            {
                var customRoleId = await _db.Database.SqlQueryRaw<Guid?>("SELECT CustomRoleId as Value FROM [identity].[WorkspaceMembers] WHERE WorkspaceId = {0} AND UserId = {1}", workspaceId, callerId).FirstOrDefaultAsync(ct);
                // If customRoleId is null -> fixed role, allow Member/OrgAdmin/SuperAdmin, deny Client (Viewer is custom)
                if (customRoleId == null || customRoleId == Guid.Empty)
                {
                    if (Roles.CanUpload(callerRoles) == false)
                        return Result<(Guid, Guid, Guid)>.Failure($"Forbidden - Missing permission {permKey}");
                }
                else
                {
                    var permId = await _db.Database.SqlQueryRaw<Guid>("SELECT Id as Value FROM [identity].[Permissions] WHERE [Key] = {0}", permKey).FirstOrDefaultAsync(ct);
                    if (permId != Guid.Empty)
                    {
                        var hasPerm = await _db.Database.SqlQueryRaw<int>("SELECT COUNT(1) as Value FROM [identity].[RolePermissions] WHERE RoleId = {0} AND PermissionId = {1}", customRoleId, permId).FirstOrDefaultAsync(ct) > 0;
                        if (!hasPerm) return Result<(Guid, Guid, Guid)>.Failure($"Forbidden - Missing permission {permKey}");
                    }
                }
            } catch { /* best-effort, if tables missing allow */ }
        }

        return Result<(Guid, Guid, Guid)>.Success((projectId, workspaceId, orgId));
    }

    private class TaskRow { public Guid Id { get; set; } public Guid ProjectId { get; set; } public Guid? ListId { get; set; } }
    private class ProjectRow { public Guid Id { get; set; } public Guid WorkspaceId { get; set; } }
    private class GuidRow { public Guid Value { get; set; } }
}
