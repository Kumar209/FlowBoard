/**
 * Shared permission keys - Single source of truth for frontend.
 * 35 keys seeded in [identity].[Permissions] via IdentitySeeder.
 * Keep in sync with backend BuildingBlocks/SharedKernel/PermissionKeys.cs
 * Use these constants everywhere instead of hardcoding "board:create" strings.
 */

export const PermissionKeys = {
  // Org (3)
  OrgView: 'org:view',
  OrgUpdate: 'org:update',
  OrgDelete: 'org:delete',
  // Workspace (4)
  WorkspaceView: 'workspace:view',
  WorkspaceCreate: 'workspace:create',
  WorkspaceUpdate: 'workspace:update',
  WorkspaceDelete: 'workspace:delete',
  // Project (4)
  ProjectView: 'project:view',
  ProjectCreate: 'project:create',
  ProjectUpdate: 'project:update',
  ProjectDelete: 'project:delete',
  // Board (4)
  BoardView: 'board:view',
  BoardCreate: 'board:create',
  BoardUpdate: 'board:update',
  BoardDelete: 'board:delete',
  // Status (4)
  StatusView: 'status:view',
  StatusCreate: 'status:create',
  StatusUpdate: 'status:update',
  StatusDelete: 'status:delete',
  // Task (6)
  TaskView: 'task:view',
  TaskCreate: 'task:create',
  TaskUpdate: 'task:update',
  TaskDelete: 'task:delete',
  TaskMove: 'task:move',
  TaskAssign: 'task:assign',
  // Comment (2)
  CommentView: 'comment:view',
  CommentCreate: 'comment:create',
  // Attachment (4)
  AttachmentView: 'attachment:view',
  AttachmentCreate: 'attachment:create',
  AttachmentUpdate: 'attachment:update',
  AttachmentDelete: 'attachment:delete',
  // Activity (2)
  ActivityViewOrg: 'activity:view:org',
  ActivityViewProject: 'activity:view:project',
  // Role (2)
  RoleView: 'role:view',
  RoleManage: 'role:manage',
} as const;

export type PermissionKey = (typeof PermissionKeys)[keyof typeof PermissionKeys];

export const AllPermissions: PermissionKey[] = Object.values(PermissionKeys);
