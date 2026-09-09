import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface OrgWorkspaceRoleDto { id: string; organizationId: string; name: string; description?: string; createdBy: string; createdAt: string; membersCount: number; permissionsCount: number; }
export interface PermissionDto { id: string; key: string; name: string; group: string; description?: string; }

@Injectable({ providedIn: 'root' })
export class OrganizationRoleService {
  private http = inject(HttpClient);

  getRoles(organizationId: string) {
    return this.http.get<OrgWorkspaceRoleDto[]>(`${environment.apiUrl}/api/organizations/${organizationId}/workspace-roles`, { withCredentials: true });
  }
  createRole(organizationId: string, name: string, description?: string) {
    return this.http.post<OrgWorkspaceRoleDto>(`${environment.apiUrl}/api/organizations/${organizationId}/workspace-roles`, { name, description }, { withCredentials: true });
  }
  updateRole(organizationId: string, roleId: string, name: string, description?: string) {
    return this.http.put<OrgWorkspaceRoleDto>(`${environment.apiUrl}/api/organizations/${organizationId}/workspace-roles/${roleId}`, { name, description }, { withCredentials: true });
  }
  deleteRole(organizationId: string, roleId: string) {
    return this.http.delete(`${environment.apiUrl}/api/organizations/${organizationId}/workspace-roles/${roleId}`, { withCredentials: true });
  }
  getRolePermissions(organizationId: string, roleId: string) {
    return this.http.get<any>(`${environment.apiUrl}/api/organizations/${organizationId}/workspace-roles/${roleId}/permissions`, { withCredentials: true });
  }
  updateRolePermissions(organizationId: string, roleId: string, permissionIds: string[]) {
    return this.http.put<any>(`${environment.apiUrl}/api/organizations/${organizationId}/workspace-roles/${roleId}/permissions`, { permissionIds }, { withCredentials: true });
  }
  getPermissions() {
    return this.http.get<PermissionDto[]>(`${environment.apiUrl}/api/permissions`, { withCredentials: true });
  }
}
