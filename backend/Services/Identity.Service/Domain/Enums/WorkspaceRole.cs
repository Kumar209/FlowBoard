namespace Identity.Service.Domain.Enums;

/// <summary>
/// WorkspaceRole - 6 RBAC roles for FlowBoard. Member 0 (view/comment/move own), ProjectManager 1 (create projects/lists/tasks - multiple per workspace), OrgAdmin 2 (manage workspace/members), Client 3 (external view assigned + comment/attach no create), Viewer 4 (read-only export), SuperAdmin 5 (all orgs billing). PM can create projects is the core enterprise multi-manager pattern.
/// </summary>
public enum WorkspaceRole
{
    Member = 0,          // View, Comment, Move own Tasks, Upload
    ProjectManager = 1,  // Create Projects + Lists/Tasks, Assign, Manage Sprints, AI Generate
    OrgAdmin = 2,        // Manage Workspace, Members, Projects (all in workspace)
    Client = 3,          // View assigned Projects/Tasks + Comment + Attach (No create/move/delete) - External
    Viewer = 4,          // View boards, Export (Internal read-only)
    SuperAdmin = 5       // Manage all Orgs, Billing, Feature Flags
}
