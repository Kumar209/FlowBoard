using SharedKernel;

namespace Project.Service.Domain.Entities;

/// <summary>
/// ProjectEnvironment - Deployment target for a Project (Development, QA, Staging, Production).
/// Separate component with CRUD: Name, URL, Description, Status.
/// </summary>
public class ProjectEnvironment : BaseEntity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Status { get; private set; } = "Active"; // Active, Inactive, Maintenance
    public Project? Project { get; private set; }

    private ProjectEnvironment() { }

    public ProjectEnvironment(Guid projectId, string name, string url, string? description = null, string status = "Active")
    {
        ProjectId = projectId;
        Name = name;
        Url = url;
        Description = description;
        Status = status;
    }

    public void Update(string name, string url, string? description, string status)
    {
        Name = name;
        Url = url;
        Description = description;
        Status = status;
        Touch();
    }
}
