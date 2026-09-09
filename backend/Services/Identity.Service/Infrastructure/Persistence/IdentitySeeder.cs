using Identity.Service.Domain.Entities;
using Identity.Service.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Identity.Service.Infrastructure.Persistence;

public static class IdentitySeeder
{
    public static async Task SeedSuperAdminAsync(IdentityDbContext db)
    {
        const string superEmail = "superadmin@flowboard.local";
        const string superPassword = "Super666@lmp";
        const string superName = "FlowBoard SuperAdmin";

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == superEmail.ToLowerInvariant());
        if (user == null)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(superPassword);
            user = new User(superEmail, hash, superName);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var org = await db.Organizations.FirstOrDefaultAsync(o => o.OwnerId == user.Id);
        if (org == null)
        {
            org = new Organization("FlowBoard System", "flowboard-system-" + Guid.NewGuid().ToString()[..6], user.Id, "System organization for SuperAdmin");
            db.Organizations.Add(org);
            await db.SaveChangesAsync();
        }

        var ws = await db.Workspaces.FirstOrDefaultAsync(w => w.OrganizationId == org.Id);
        if (ws == null)
        {
            ws = new Workspace(org.Id, "System", "system-" + Guid.NewGuid().ToString()[..4]);
            db.Workspaces.Add(ws);
            await db.SaveChangesAsync();
        }

        if (!await db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == ws.Id && m.UserId == user.Id))
        {
            var member = new WorkspaceMember(ws.Id, user.Id, WorkspaceRole.SuperAdmin);
            db.WorkspaceMembers.Add(member);
            await db.SaveChangesAsync();
        }

        if (!await db.OrganizationMembers.AnyAsync(m => m.OrganizationId == org.Id && m.UserId == user.Id))
        {
            var orgMember = new OrganizationMember(org.Id, user.Id, 2); // OrgAdmin
            db.OrganizationMembers.Add(orgMember);
            await db.SaveChangesAsync();
        }

        await SeedPermissionsAsync(db);
    }

    public static async Task SeedPermissionsAsync(IdentityDbContext db)
    {
        if (await db.Permissions.AnyAsync()) return;
        var perms = new[]
        {
            new Permission("org:view", "View Organization", "Organization", "See org name/members"),
            new Permission("org:update", "Update Organization", "Organization", "Edit org name/desc/logo"),
            new Permission("org:delete", "Delete Organization", "Organization", "Delete org cascade"),
            new Permission("workspace:view", "View Workspace", "Workspace", "See workspaces"),
            new Permission("workspace:create", "Create Workspace", "Workspace", "Create new workspace"),
            new Permission("workspace:update", "Update Workspace", "Workspace", "Edit workspace"),
            new Permission("workspace:delete", "Delete Workspace", "Workspace", "Delete workspace"),
            new Permission("project:view", "View Project", "Project", "See projects"),
            new Permission("project:create", "Create Project", "Project", "Create projects"),
            new Permission("project:update", "Update Project", "Project", "Edit project"),
            new Permission("project:delete", "Delete Project", "Project", "Delete project"),
            new Permission("board:view", "View Board", "Board", "See boards"),
            new Permission("board:create", "Create Board", "Board", "Create boards"),
            new Permission("board:update", "Update Board", "Board", "Edit board"),
            new Permission("board:delete", "Delete Board", "Board", "Delete board"),
            new Permission("task:view", "View Task", "Task", "See tasks"),
            new Permission("task:create", "Create Task", "Task", "Create tasks"),
            new Permission("task:update", "Update Task", "Task", "Update tasks"),
            new Permission("task:delete", "Delete Task", "Task", "Delete tasks"),
            new Permission("task:move", "Move Task", "Task", "Drag between columns"),
            new Permission("task:assign", "Assign Task", "Task", "Assign assignee"),
            new Permission("comment:view", "View Comment", "Task", "See comments"),
            new Permission("comment:create", "Create Comment", "Task", "Add comments"),
            new Permission("activity:view:org", "View Org Activity", "Activity", "See organization audit"),
            new Permission("activity:view:project", "View Project Activity", "Activity", "See project audit"),
            new Permission("role:view", "View Roles", "Role", "See custom roles"),
            new Permission("role:manage", "Manage Roles", "Role", "Create/update/delete roles and permissions"),
        };
        db.Permissions.AddRange(perms);
        await db.SaveChangesAsync();
    }
}
