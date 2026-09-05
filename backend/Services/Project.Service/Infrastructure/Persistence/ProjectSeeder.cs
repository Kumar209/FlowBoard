using Microsoft.EntityFrameworkCore;
using Project.Service.Domain.Entities;
using ProjectEntity = Project.Service.Domain.Entities.Project;

namespace Project.Service.Infrastructure.Persistence;

public static class ProjectSeeder
{
    // Seed 1 Project 'FlowBoard Demo' with 3 Lists (ToDo, InProgress, Done) + 12 Tasks (4 per list)
    public static async Task SeedAsync(ProjectDbContext db)
    {
        if (await db.Projects.AnyAsync())
        {
            // Ensure existing demo project has 4 lists (add In Review if missing)
            var existing = await db.Projects.FirstOrDefaultAsync();
            if (existing != null)
            {
                var hasInReview = await db.BoardLists.AnyAsync(l => l.ProjectId == existing.Id && l.Name == "In Review");
                if (!hasInReview)
                {
                    var existingDone = await db.BoardLists.FirstOrDefaultAsync(l => l.ProjectId == existing.Id && l.Name == "Done");
                    if (existingDone != null) existingDone.Move(3);
                    db.BoardLists.Add(new BoardList(existing.Id, "In Review", 2));
                    await db.SaveChangesAsync();
                }
            }
            return;
        }

        var workspaceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var ownerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Try to use first workspace from Identity if exists? For stub, use fixed IDs above.
        var project = new ProjectEntity(workspaceId, "FlowBoard Demo", "FB-3", ownerId, "Seeded demo project for Task 2.1 - verify board");
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var todo = new BoardList(project.Id, "To Do", 0);
        var inProgress = new BoardList(project.Id, "In Progress", 1);
        var inReview = new BoardList(project.Id, "In Review", 2);
        var done = new BoardList(project.Id, "Done", 3);
        db.BoardLists.AddRange(todo, inProgress, inReview, done);
        await db.SaveChangesAsync();

        var tasks = new[]
        {
            // To Do (3)
            new TaskItem(project.Id, todo.Id, "Setup CI/CD pipeline", ownerId, 0, Domain.Enums.TaskPriority.High),
            new TaskItem(project.Id, todo.Id, "Design database schema", ownerId, 1, Domain.Enums.TaskPriority.Urgent),
            new TaskItem(project.Id, todo.Id, "Create wireframes", ownerId, 2, Domain.Enums.TaskPriority.Medium),
            // In Progress (3)
            new TaskItem(project.Id, inProgress.Id, "Implement auth flow", ownerId, 0, Domain.Enums.TaskPriority.High, assigneeId: ownerId),
            new TaskItem(project.Id, inProgress.Id, "Build kanban board", ownerId, 1, Domain.Enums.TaskPriority.High),
            new TaskItem(project.Id, inProgress.Id, "Add drag-drop", ownerId, 2, Domain.Enums.TaskPriority.Medium),
            // In Review (3) - new default status before Done
            new TaskItem(project.Id, inReview.Id, "Code review - auth", ownerId, 0, Domain.Enums.TaskPriority.High),
            new TaskItem(project.Id, inReview.Id, "QA review - board", ownerId, 1, Domain.Enums.TaskPriority.Medium),
            new TaskItem(project.Id, inReview.Id, "Review docs", ownerId, 2, Domain.Enums.TaskPriority.Low),
            // Done (3)
            new TaskItem(project.Id, done.Id, "Init Git repo", ownerId, 0, Domain.Enums.TaskPriority.Low),
            new TaskItem(project.Id, done.Id, "Scaffold backend", ownerId, 1, Domain.Enums.TaskPriority.Medium),
            new TaskItem(project.Id, done.Id, "Scaffold frontend", ownerId, 2, Domain.Enums.TaskPriority.Medium),
        };
        db.Tasks.AddRange(tasks);
        await db.SaveChangesAsync();

        // Optional: add a comment and activity for demo
        var comment = new Comment(tasks[0].Id, ownerId, "Looks good!");
        db.Comments.Add(comment);

        var activity = new ActivityLog(project.Id, tasks[0].Id, ownerId, "TaskCreated", $"{{\"title\":\"{tasks[0].Title}\"}}");
        db.ActivityLogs.Add(activity);

        await db.SaveChangesAsync();
    }
}
