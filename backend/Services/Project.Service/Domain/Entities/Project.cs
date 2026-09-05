using SharedKernel;

namespace Project.Service.Domain.Entities;

/// <summary>
/// Project aggregate root - board container inside a Workspace (e.g., "FlowBoard Demo" Key FB-3). Owned by a user (OwnerId) in that workspace. Groups BoardLists (columns) and TaskItems (cards). Key is short uppercase prefix (FB) + increment, unique per WorkspaceId for filtering/search (e.g., FB-3 search). Used by ProjectService to enforce workspace isolation and PM/OrgAdmin create permission (Member/Client cannot create). Navigation: Lists + Tasks (cascade delete).
/// </summary>
public class Project : BaseEntity, IAggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty; // e.g., FB-1, short prefix from Name
    public string? Description { get; private set; }
    public Guid OwnerId { get; private set; }

    // Navigation
    public ICollection<BoardList> Lists { get; private set; } = new List<BoardList>();
    public ICollection<TaskItem> Tasks { get; private set; } = new List<TaskItem>();

    private Project() { } // EF

    public Project(Guid workspaceId, string name, string key, Guid ownerId, string? description = null)
    {
        WorkspaceId = workspaceId;
        Name = name;
        Key = key.ToUpperInvariant();
        OwnerId = ownerId;
        Description = description;
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        Touch();
    }
}
