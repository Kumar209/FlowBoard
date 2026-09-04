using Microsoft.EntityFrameworkCore;
using Identity.Service.Domain.Entities;

namespace Identity.Service.Application.Interfaces;

// Enterprise Clean Architecture - Application defines abstraction, Infrastructure implements
// Handlers depend on this interface, not concrete IdentityDbContext (DIP)
// This allows mocking in unit tests without SQL Server
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Organization> Organizations { get; }
    DbSet<Workspace> Workspaces { get; }
    DbSet<WorkspaceMember> WorkspaceMembers { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
