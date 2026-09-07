namespace SharedKernel;

/// <summary>
/// Single source of truth for workspace roles.
/// Keep in sync with frontend src/app/shared/constants/roles.ts and Identity.Domain.Enums.WorkspaceRole
/// </summary>
public static class WorkspaceRoles
{
    public const string Member = "Member";
    public const string ProjectManager = "ProjectManager";
    public const string OrgAdmin = "OrgAdmin";
    public const string Client = "Client";
    public const string Viewer = "Viewer";
    public const string SuperAdmin = "SuperAdmin";

    public static readonly string[] OrgLevel5 = new[] { Member, ProjectManager, OrgAdmin, Client, Viewer };
    public static readonly string[] All6 = new[] { Member, ProjectManager, OrgAdmin, Client, Viewer, SuperAdmin };

    public static readonly Dictionary<string, int> Map = new()
    {
        [Member] = 0,
        [ProjectManager] = 1,
        [OrgAdmin] = 2,
        [Client] = 3,
        [Viewer] = 4,
        [SuperAdmin] = 5,
    };

    public static readonly Dictionary<int, string> ReverseMap = new()
    {
        [0] = Member,
        [1] = ProjectManager,
        [2] = OrgAdmin,
        [3] = Client,
        [4] = Viewer,
        [5] = SuperAdmin,
    };

    public static string GetLabel(int value) => ReverseMap.TryGetValue(value, out var label) ? label : value.ToString();
    public static int GetValue(string label) => Map.TryGetValue(label, out var value) ? value : -1;
    public static bool IsOrgLevel(string role) => OrgLevel5.Contains(role);
    public static bool IsValid(string role) => All6.Contains(role);
}
