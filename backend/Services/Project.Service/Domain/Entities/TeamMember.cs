namespace Project.Service.Domain.Entities;

/// <summary>
/// TeamMember - user in a team. Composite? Use Id as PK for simplicity. Unique Project+Team+User.
/// </summary>
public class TeamMember
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;

    public Team? Team { get; private set; }

    private TeamMember() { }

    public TeamMember(Guid teamId, Guid userId)
    {
        TeamId = teamId;
        UserId = userId;
    }
}
