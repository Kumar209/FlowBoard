namespace SharedKernel;

/// <summary>
/// Single source for 47 permission keys — seeded in [identity].[Permissions] via IdentitySeeder.
/// Use these constants everywhere (services, controllers, helpers, frontend) instead of hardcoding "board:create" strings.
/// </summary>
public static class PermissionKeys
{
    // Org (3)
    public const string OrgView = "org:view";
    public const string OrgUpdate = "org:update";
    public const string OrgDelete = "org:delete";

    // Workspace (4)
    public const string WorkspaceView = "workspace:view";
    public const string WorkspaceCreate = "workspace:create";
    public const string WorkspaceUpdate = "workspace:update";
    public const string WorkspaceDelete = "workspace:delete";

    // Project (4)
    public const string ProjectView = "project:view";
    public const string ProjectCreate = "project:create";
    public const string ProjectUpdate = "project:update";
    public const string ProjectDelete = "project:delete";

    // Board (4)
    public const string BoardView = "board:view";
    public const string BoardCreate = "board:create";
    public const string BoardUpdate = "board:update";
    public const string BoardDelete = "board:delete";

    // Status (4)
    public const string StatusView = "status:view";
    public const string StatusCreate = "status:create";
    public const string StatusUpdate = "status:update";
    public const string StatusDelete = "status:delete";

    // Task (6)
    public const string TaskView = "task:view";
    public const string TaskCreate = "task:create";
    public const string TaskUpdate = "task:update";
    public const string TaskDelete = "task:delete";
    public const string TaskMove = "task:move";
    public const string TaskAssign = "task:assign";

    // Comment (2)
    public const string CommentView = "comment:view";
    public const string CommentCreate = "comment:create";

    // Attachment (4)
    public const string AttachmentView = "attachment:view";
    public const string AttachmentCreate = "attachment:create";
    public const string AttachmentUpdate = "attachment:update";
    public const string AttachmentDelete = "attachment:delete";

    // Activity (2)
    public const string ActivityViewOrg = "activity:view:org";
    public const string ActivityViewProject = "activity:view:project";

    // Role (2)
    public const string RoleView = "role:view";
    public const string RoleManage = "role:manage";

    // Sprint (4)
    public const string SprintView = "sprint:view";
    public const string SprintCreate = "sprint:create";
    public const string SprintUpdate = "sprint:update";
    public const string SprintDelete = "sprint:delete";

    // Team (4)
    public const string TeamView = "team:view";
    public const string TeamCreate = "team:create";
    public const string TeamUpdate = "team:update";
    public const string TeamDelete = "team:delete";

    // Environment (4)
    public const string EnvironmentView = "environment:view";
    public const string EnvironmentCreate = "environment:create";
    public const string EnvironmentUpdate = "environment:update";
    public const string EnvironmentDelete = "environment:delete";

    public static readonly string[] All = new[]
    {
        OrgView, OrgUpdate, OrgDelete,
        WorkspaceView, WorkspaceCreate, WorkspaceUpdate, WorkspaceDelete,
        ProjectView, ProjectCreate, ProjectUpdate, ProjectDelete,
        BoardView, BoardCreate, BoardUpdate, BoardDelete,
        StatusView, StatusCreate, StatusUpdate, StatusDelete,
        TaskView, TaskCreate, TaskUpdate, TaskDelete, TaskMove, TaskAssign,
        CommentView, CommentCreate,
        AttachmentView, AttachmentCreate, AttachmentUpdate, AttachmentDelete,
        ActivityViewOrg, ActivityViewProject,
        RoleView, RoleManage,
        SprintView, SprintCreate, SprintUpdate, SprintDelete,
        TeamView, TeamCreate, TeamUpdate, TeamDelete,
        EnvironmentView, EnvironmentCreate, EnvironmentUpdate, EnvironmentDelete
    };
}

// trigger deploy 2026-09-20 orgadmin workspace fallback
