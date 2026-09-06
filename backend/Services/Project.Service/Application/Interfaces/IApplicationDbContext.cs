using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Project.Service.Domain.Entities;
using ProjectEntity = Project.Service.Domain.Entities.Project;

namespace Project.Service.Application.Interfaces;

// Clean Architecture DIP - Application defines, Infrastructure implements (same as Identity Task 1.2.1)
public interface IApplicationDbContext
{
    DbSet<ProjectEntity> Projects { get; }
    DbSet<Board> Boards { get; }
    DbSet<BoardList> BoardLists { get; } // Column
    DbSet<Sprint> Sprints { get; }
    DbSet<Team> Teams { get; }
    DbSet<TeamMember> TeamMembers { get; }
    DbSet<TaskItem> Tasks { get; } // Issue
    DbSet<SubTask> SubTasks { get; }
    DbSet<Comment> Comments { get; }
    DbSet<ActivityLog> ActivityLogs { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<ProjectEnvironment> Environments { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
