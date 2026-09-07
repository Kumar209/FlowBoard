/**
 * Shared roles - Single source of truth for frontend.
 * Org level: 5 roles (Member, ProjectManager, OrgAdmin, Client, Viewer)
 * System level: 6 roles (includes SuperAdmin)
 * Keep in sync with backend Domain/Enums/WorkspaceRole.cs
 */

export enum WorkspaceRole {
  Member = 0,
  ProjectManager = 1,
  OrgAdmin = 2,
  Client = 3,
  Viewer = 4,
  SuperAdmin = 5,
}

export const WORKSPACE_ROLES_5 = [
  { value: WorkspaceRole.Member, label: 'Member' },
  { value: WorkspaceRole.ProjectManager, label: 'ProjectManager' },
  { value: WorkspaceRole.OrgAdmin, label: 'OrgAdmin' },
  { value: WorkspaceRole.Client, label: 'Client' },
  { value: WorkspaceRole.Viewer, label: 'Viewer' },
] as const;

export const WORKSPACE_ROLES_6 = [
  ...WORKSPACE_ROLES_5,
  { value: WorkspaceRole.SuperAdmin, label: 'SuperAdmin' },
] as const;

export const ROLE_LABEL_MAP: Record<string, string> = {
  '0': 'Member',
  '1': 'ProjectManager',
  '2': 'OrgAdmin',
  '3': 'Client',
  '4': 'Viewer',
  '5': 'SuperAdmin',
};

export const ROLE_VALUE_MAP: Record<string, WorkspaceRole> = {
  'Member': WorkspaceRole.Member,
  'ProjectManager': WorkspaceRole.ProjectManager,
  'OrgAdmin': WorkspaceRole.OrgAdmin,
  'Client': WorkspaceRole.Client,
  'Viewer': WorkspaceRole.Viewer,
  'SuperAdmin': WorkspaceRole.SuperAdmin,
};

export function getRoleLabel(value: string | number): string {
  return ROLE_LABEL_MAP[String(value)] ?? String(value);
}

export function getRoleValue(label: string): WorkspaceRole | undefined {
  return ROLE_VALUE_MAP[label];
}
