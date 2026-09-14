namespace SharedKernel;

/// <summary>
/// Single source for role names and values — compact 0-3 (SuperAdmin 0, Member 1, OrgAdmin 2, Client 3).
/// Keep in sync with frontend src/app/shared/constants/roles.ts
/// </summary>
public static class Roles
{
    // Fixed org-level roles + system SuperAdmin
    public const string SuperAdmin = "SuperAdmin"; // 0 — system global, manages all orgs/billing, not in OrganizationMembers
    public const string Member = "Member";       // 1 — org Member, project/workspace member, can view/comment/move own, upload
    public const string OrgAdmin = "OrgAdmin";   // 2 — org admin, manages workspaces/members/roles, can delete own org, manage roles/permissions
    public const string Client = "Client";       // 3 — external, view assigned + comment/attach no create/move/delete

    // Int values as stored in OrganizationMembers.Role + WorkspaceMembers.Role (when CustomRoleId is null)
    public const int SuperAdminValue = 0;
    public const int MemberValue = 1;
    public const int OrgAdminValue = 2;
    public const int ClientValue = 3;

    public static readonly string[] FixedOrgRoles = new[] { Member, OrgAdmin, Client };
    public static readonly string[] AllFixed = new[] { SuperAdmin, Member, OrgAdmin, Client };

    public static readonly Dictionary<string, int> Map = new()
    {
        [SuperAdmin] = SuperAdminValue,
        [Member] = MemberValue,
        [OrgAdmin] = OrgAdminValue,
        [Client] = ClientValue,
    };

    public static readonly Dictionary<int, string> ReverseMap = new()
    {
        [SuperAdminValue] = SuperAdmin,
        [MemberValue] = Member,
        [OrgAdminValue] = OrgAdmin,
        [ClientValue] = Client,
    };

    public static string GetLabel(int value) => ReverseMap.TryGetValue(value, out var label) ? label : value.ToString();
    public static int GetValue(string label) => Map.TryGetValue(label, out var value) ? value : -1;
    public static bool IsFixedOrgRole(string role) => FixedOrgRoles.Contains(role);
    public static bool IsValidOrgRole(string role) => AllFixed.Contains(role);

    // Common role checks
    private static string NormalizeRole(string r) {
        if (int.TryParse(r, out var iv)) return GetLabel(iv);
        return r;
    }
    private static IEnumerable<string> Normalized(IEnumerable<string> roles) => roles.Select(NormalizeRole);
    public static bool IsSuperAdmin(IEnumerable<string> roles) => Normalized(roles).Contains(SuperAdmin);
    public static bool IsOrgAdmin(IEnumerable<string> roles) => Normalized(roles).Contains(OrgAdmin);
    public static bool IsPrivilegedForManage(IEnumerable<string> roles) => Normalized(roles).Any(r => r == OrgAdmin || r == SuperAdmin);
    public static bool CanManageRoles(IEnumerable<string> roles) => IsPrivilegedForManage(roles);
    public static bool CanManageProjects(IEnumerable<string> roles) => IsPrivilegedForManage(roles);
    public static bool CanUpload(IEnumerable<string> roles) => !Normalized(roles).Contains(Client);
}
