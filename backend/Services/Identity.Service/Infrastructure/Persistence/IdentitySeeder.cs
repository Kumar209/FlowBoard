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
    }
}
