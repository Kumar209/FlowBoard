using Identity.Service.Domain.Entities;
using SharedKernel;
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
            var member = new WorkspaceMember(ws.Id, user.Id, Roles.SuperAdminValue);
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
        var allKeys = new[]
        {
            ("org:view", "View Organization", "Organization", "See org name/members"),
            ("org:update", "Update Organization", "Organization", "Edit org name/desc/logo"),
            ("org:delete", "Delete Organization", "Organization", "Delete org cascade"),
            ("workspace:view", "View Workspace", "Workspace", "See workspaces"),
            ("workspace:create", "Create Workspace", "Workspace", "Create new workspace"),
            ("workspace:update", "Update Workspace", "Workspace", "Edit workspace"),
            ("workspace:delete", "Delete Workspace", "Workspace", "Delete workspace"),
            ("project:view", "View Project", "Project", "See projects"),
            ("project:create", "Create Project", "Project", "Create projects"),
            ("project:update", "Update Project", "Project", "Edit project"),
            ("project:delete", "Delete Project", "Project", "Delete project"),
            ("board:view", "View Board", "Board", "See boards"),
            ("board:create", "Create Board", "Board", "Create boards"),
            ("board:update", "Update Board", "Board", "Edit board"),
            ("board:delete", "Delete Board", "Board", "Delete board"),
            ("status:view", "View Status", "Status", "See project statuses"),
            ("status:create", "Create Status", "Status", "Create statuses"),
            ("status:update", "Update Status", "Status", "Edit status name"),
            ("status:delete", "Delete Status", "Status", "Delete status if unused"),
            ("task:view", "View Task", "Task", "See tasks"),
            ("task:create", "Create Task", "Task", "Create tasks"),
            ("task:update", "Update Task", "Task", "Update tasks"),
            ("task:delete", "Delete Task", "Task", "Delete tasks"),
            ("task:move", "Move Task", "Task", "Drag between columns"),
            ("task:assign", "Assign Task", "Task", "Assign assignee"),
            ("comment:view", "View Comment", "Task", "See comments"),
            ("comment:create", "Create Comment", "Task", "Add comments"),
            ("activity:view:org", "View Org Activity", "Activity", "See organization audit"),
            ("activity:view:project", "View Project Activity", "Activity", "See project audit"),
            ("role:view", "View Roles", "Role", "See custom roles"),
            ("role:manage", "Manage Roles", "Role", "Create/update/delete roles and permissions"),
            ("attachment:view", "View Attachment", "Attachment", "See attachments"),
            ("attachment:create", "Create Attachment", "Attachment", "Upload attachments via Cloudinary"),
            ("attachment:update", "Update Attachment", "Attachment", "Update attachment metadata"),
            ("attachment:delete", "Delete Attachment", "Attachment", "Delete attachments from Cloudinary + DB"),
        };
        var existingKeys = await db.Permissions.Select(p => p.Key).ToListAsync();
        var toAdd = allKeys.Where(k => !existingKeys.Contains(k.Item1)).Select(k => new Permission(k.Item1, k.Item2, k.Item3, k.Item4)).ToList();
        if (toAdd.Any())
        {
            db.Permissions.AddRange(toAdd);
            await db.SaveChangesAsync();
        }
    }
}
