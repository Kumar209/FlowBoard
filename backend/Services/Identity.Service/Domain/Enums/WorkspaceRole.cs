namespace Identity.Service.Domain.Enums;

// 6 roles for FlowBoard - PM can create projects (multiple managers), Client is external view+comment
public enum WorkspaceRole
{
    Member = 0,          // View, Comment, Move own Tasks, Upload
    ProjectManager = 1,  // Create Projects + Lists/Tasks, Assign, Manage Sprints, AI Generate
    OrgAdmin = 2,        // Manage Workspace, Members, Projects (all in workspace)
    Client = 3,          // View assigned Projects/Tasks + Comment + Attach (No create/move/delete) - External
    Viewer = 4,          // View boards, Export (Internal read-only)
    SuperAdmin = 5       // Manage all Orgs, Billing, Feature Flags
}
