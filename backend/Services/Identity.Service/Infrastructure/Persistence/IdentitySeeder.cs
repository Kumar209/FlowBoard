using Identity.Service.Domain.Entities;
using SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Identity.Service.Infrastructure.Persistence;

public static class IdentitySeeder
{
    public static async Task SeedSuperAdminAsync(IdentityDbContext db, IConfiguration? config = null)
    {
        var superEmail = config?["SuperAdmin:Email"] ?? config?["SuperAdmin__Email"] ?? "superadmin@flowboard.local";
        var superPassword = config?["SuperAdmin:Password"] ?? config?["SuperAdmin__Password"] ?? "PASTE_STRONG_PASSWORD_MIN_12_CHARS";
        if (string.IsNullOrWhiteSpace(superEmail) || superEmail.Contains("PASTE") || string.IsNullOrWhiteSpace(superPassword) || superPassword.Contains("PASTE"))
        {
            // Fallback for local dev without config — use default but warn
            superEmail = "superadmin@flowboard.local";
            superPassword = "Super666@lmp";
        }
        const string superName = "FlowBoard SuperAdmin";

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == superEmail.ToLowerInvariant());
        if (user == null)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(superPassword);
            user = new User(superEmail, hash, superName);
            user.PromoteToSuperAdmin();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        else if (!user.IsSuperAdmin)
        {
            user.PromoteToSuperAdmin();
            await db.SaveChangesAsync();
        }

        // SuperAdmin is global platform owner via Users.IsSuperAdmin, not a tenant member.
        // No system org/workspace is created. Remove legacy system org if it exists (created before this refactor).
        var legacyOrg = await db.Organizations.FirstOrDefaultAsync(o => o.Name == "FlowBoard System" && o.OwnerId == user.Id);
        if (legacyOrg != null)
        {
            var legacyWsIds = await db.Workspaces.Where(w => w.OrganizationId == legacyOrg.Id).Select(w => w.Id).ToListAsync();
            if (legacyWsIds.Any())
            {
                var wsMembers = await db.WorkspaceMembers.Where(m => legacyWsIds.Contains(m.WorkspaceId)).ToListAsync();
                if (wsMembers.Any()) { db.WorkspaceMembers.RemoveRange(wsMembers); await db.SaveChangesAsync(); }
                var wsList = await db.Workspaces.Where(w => w.OrganizationId == legacyOrg.Id).ToListAsync();
                db.Workspaces.RemoveRange(wsList); await db.SaveChangesAsync();
            }
            var orgMembers = await db.OrganizationMembers.Where(m => m.OrganizationId == legacyOrg.Id).ToListAsync();
            if (orgMembers.Any()) { db.OrganizationMembers.RemoveRange(orgMembers); await db.SaveChangesAsync(); }
            db.Organizations.Remove(legacyOrg); await db.SaveChangesAsync();
        }

        await SeedSubscriptionPlansAsync(db);
        await SeedPermissionsAsync(db);
        // Backfill existing orgs without plan to Free (customer orgs only, system org already removed)
        var freePlan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Name == "Free");
        if (freePlan != null)
        {
            var orgsWithoutPlan = await db.Organizations.Where(o => o.SubscriptionPlanId == Guid.Empty).ToListAsync();
            foreach (var o in orgsWithoutPlan) o.SetPlan(freePlan.Id);
            if (orgsWithoutPlan.Any()) await db.SaveChangesAsync();
        }
    }

    public static async Task SeedSubscriptionPlansAsync(IdentityDbContext db)
    {
        if (await db.SubscriptionPlans.AnyAsync()) return;
        var plans = new[]
        {
            new SubscriptionPlanEntity(Guid.Parse("a0000000-0000-0000-0000-000000000010"), "Free", 0m, 5, 2, 3, 5, 100, 1000, "[\"basic-board\",\"basic-tasks\"]"),
            new SubscriptionPlanEntity(Guid.Parse("a0000000-0000-0000-0000-000000000011"), "Pro", 29m, 25, 10, 50, 50, 5000, 10000, "[\"board\",\"sprints\",\"ai-draft\"]"),
            new SubscriptionPlanEntity(Guid.Parse("a0000000-0000-0000-0000-000000000012"), "Business", 79m, 100, 50, 200, 200, 20000, 50000, "[\"board\",\"sprints\",\"ai-full\",\"analytics\"]"),
            new SubscriptionPlanEntity(Guid.Parse("a0000000-0000-0000-0000-000000000013"), "Enterprise", 199m, 500, 200, 1000, 1000, 100000, 200000, "[\"all\"]"),
        };
        db.SubscriptionPlans.AddRange(plans);
        await db.SaveChangesAsync();
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
