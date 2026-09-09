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

        if (await db.Users.AnyAsync(u => u.Email == superEmail.ToLowerInvariant())) return;

        var hash = BCrypt.Net.BCrypt.HashPassword(superPassword);
        var user = new User(superEmail, hash, superName);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var org = new Organization("FlowBoard System", "flowboard-system-" + Guid.NewGuid().ToString()[..6], user.Id, "System organization for SuperAdmin");
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var ws = new Workspace(org.Id, "System", "system-" + Guid.NewGuid().ToString()[..4]);
        db.Workspaces.Add(ws);
        await db.SaveChangesAsync();

        var member = new WorkspaceMember(ws.Id, user.Id, WorkspaceRole.SuperAdmin);
        db.WorkspaceMembers.Add(member);
        await db.SaveChangesAsync();
    }
}
