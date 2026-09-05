using Microsoft.EntityFrameworkCore;
using Project.Service.Domain.Entities;
using ProjectEntity = Project.Service.Domain.Entities.Project;

namespace Project.Service.Infrastructure.Persistence;

public static class ProjectSeeder
{
    // Seed 1 Project 'FlowBoard Demo' with 3 Lists (ToDo, InProgress, Done) + 12 Tasks (4 per list)
    public static async Task SeedAsync(ProjectDbContext db)
    {
        if (await db.Projects.AnyAsync()) return;

        var workspaceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var ownerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Try to use first workspace from Identity if exists? For stub, use fixed IDs above.
        var project = new ProjectEntity(workspaceId, "FlowBoard Demo", "FB-3", ownerId, "Seeded demo project for Task 2.1 - verify board");
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var todo = new BoardList(project.Id, "To Do", 0);
        var inProgress = new BoardList(project.Id, "In Progress", 1);
        var done = new BoardList(project.Id, "Done", 2);
        db.BoardLists.AddRange(todo, inProgress, done);
        await db.SaveChangesAsync();

        var tasks = new[]
        {
            // To Do (4)
            new TaskItem(project.Id, todo.Id, "Setup CI/CD pipeline", ownerId, 0, Domain.Enums.TaskPriority.High),
            new TaskItem(project.Id, todo.Id, "Design database schema", ownerId, 1, Domain.Enums.TaskPriority.Urgent),
            new TaskItem(project.Id, todo.Id, "Create wireframes", ownerId, 2, Domain.Enums.TaskPriority.Medium),
            new TaskItem(project.Id, todo.Id, "Write API docs", ownerId, 3, Domain.Enums.TaskPriority.Low),
            // In Progress (4)
            new TaskItem(project.Id, inProgress.Id, "Implement auth flow", ownerId, 0, Domain.Enums.TaskPriority.High, assigneeId: ownerId),
            new TaskItem(project.Id, inProgress.Id, "Build kanban board", ownerId, 1, Domain.Enums.TaskPriority.High),
            new TaskItem(project.Id, inProgress.Id, "Add drag-drop", ownerId, 2, Domain.Enums.TaskPriority.Medium),
            new TaskItem(project.Id, inProgress.Id, "Integrate Redis cache", ownerId, 3, Domain.Enums.TaskPriority.Medium),
            // Done (4)
            new TaskItem(project.Id, done.Id, "Init Git repo", ownerId, 0, Domain.Enums.TaskPriority.Low),
            new TaskItem(project.Id, done.Id, "Scaffold backend", ownerId, 1, Domain.Enums.TaskPriority.Medium),
            new TaskItem(project.Id, done.Id, "Scaffold frontend", ownerId, 2, Domain.Enums.TaskPriority.Medium),
            new TaskItem(project.Id, done.Id, "Configure environments", ownerId, 3, Domain.Enums.TaskPriority.Low),
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
