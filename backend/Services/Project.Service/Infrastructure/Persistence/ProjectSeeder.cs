using Microsoft.EntityFrameworkCore;
using Project.Service.Domain.Entities;
using ProjectEntity = Project.Service.Domain.Entities.Project;

namespace Project.Service.Infrastructure.Persistence;

public static class ProjectSeeder
{
    // Project starts fully empty: no boards, no columns, no sprints, no tasks.
    // User creates Teams -> Boards -> Columns manually per workflow.
    // Seed only ensures at least one demo project exists for local dev, without auto boards.
    public static async Task SeedAsync(ProjectDbContext db)
    {
        if (await db.Projects.AnyAsync())
        {
            // Do not auto-backfill boards/columns/sprints - keep existing data as-is, but don't create missing ones
            return;
        }

        var workspaceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var ownerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var project = new ProjectEntity(workspaceId, "FlowBoard Demo", "FB-3", ownerId, "Seeded demo project - empty per workflow");
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        // Intentionally NOT creating boards/columns/sprints/tasks - project is empty
    }
}
