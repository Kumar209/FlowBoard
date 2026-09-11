/**
 * Shared roles - Single source of truth for frontend.
 * Org roles: 3 fixed (Member 1, OrgAdmin 2, Client 3) — stored in [identity].OrganizationMembers
 * Workspace roles: DYNAMIC per-org via [identity].OrganizationWorkspaceRoles (e.g., Developer, QA) — NOT hardcoded.
 * System: SuperAdmin 0 (global, not in OrganizationMembers, Users.IsSuperAdmin)
 * Keep in sync with backend BuildingBlocks/SharedKernel/Roles.cs (single shared file)
 * Compact 0-3 (2026-09-10) — SuperAdmin 0, Member 1, OrgAdmin 2, Client 3 (legacy 1,4 gaps removed via DB drop)
 * Backward compat: WorkspaceRole enum kept for existing guards/services (maps ProjectManager/Viewer to Member)
 */

export enum WorkspaceRole {
  SuperAdmin = 0,
  Member = 1,
  ProjectManager = 1, // legacy custom — now maps to custom OrganizationWorkspaceRoles, kept for compat
  OrgAdmin = 2,
  Client = 3,
  Viewer = 1, // legacy custom — maps to custom (Member fallback)
}

export type OrgRole = 'Member' | 'OrgAdmin' | 'Client';
export type SystemRole = OrgRole | 'SuperAdmin';

export const OrgRoleValues = {
  SuperAdmin: 0,
  Member: 1,
  OrgAdmin: 2,
  Client: 3,
} as const;

export const FIXED_ORG_ROLES: { value: number; label: OrgRole }[] = [
  { value: OrgRoleValues.Member, label: 'Member' },
  { value: OrgRoleValues.OrgAdmin, label: 'OrgAdmin' },
  { value: OrgRoleValues.Client, label: 'Client' },
];

export const ALL_FIXED_ROLES = [...FIXED_ORG_ROLES, { value: OrgRoleValues.SuperAdmin, label: 'SuperAdmin' as const }] as const;

export const ROLE_LABEL_MAP: Record<string, string> = {
  '0': 'SuperAdmin',
  '1': 'Member',
  '2': 'OrgAdmin',
  '3': 'Client',
};

export const ROLE_VALUE_MAP: Record<string, number> = {
  'SuperAdmin': 0,
  'Member': 1,
  'OrgAdmin': 2,
  'Client': 3,
  // legacy aliases
  'ProjectManager': 1,
  'Viewer': 1,
};

export function getRoleLabel(value: string | number): string {
  return ROLE_LABEL_MAP[String(value)] ?? String(value);
}

export function getRoleValue(label: string): number | undefined {
  return ROLE_VALUE_MAP[label];
}

export function isFixedOrgRole(role: string): boolean {
  return ['Member', 'OrgAdmin', 'Client'].includes(role);
}
