/**
 * Shared roles - Single source of truth for frontend. Fixed organization roles only.
 * Org roles: 3 fixed (Member 1, OrgAdmin 2, Client 3) — stored in [identity].OrganizationMembers.Role + OwnerId
 * Workspace membership: WorkspaceMembers.WorkspaceId+UserId with Role (0-3) + CustomRoleId FK → OrganizationWorkspaceRoles (dynamic per-org, e.g., Developer, QA)
 * System: SuperAdmin 0 (global, Users.IsSuperAdmin, not in OrganizationMembers)
 * Keep in sync with backend BuildingBlocks/SharedKernel/Roles.cs
 * Compact 0-3 — SuperAdmin 0, Member 1, OrgAdmin 2, Client 3
 * Custom workspace roles are NOT in this enum — fetched via GET /api/organizations/{orgId}/workspace-roles and checked via RolePermissions (permission keys like project:create, task:create, comment:create)
 */

export enum WorkspaceRole {
  SuperAdmin = 0,
  Member = 1,
  OrgAdmin = 2,
  Client = 3,
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
