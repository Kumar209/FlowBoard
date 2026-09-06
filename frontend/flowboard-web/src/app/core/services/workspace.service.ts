import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

/**
 * WorkspaceService - MNC-grade: inject(HttpClient) + signals.
 * Wraps Identity Service workspaces via Gateway :5000 -> :5001.
 * Used by Workspaces list w (all) and Workspace detail w/:wid.
 */
export interface WorkspaceDto { id: string; name: string; slug: string; organizationId: string; role: number; roleName?: string; }
export interface OrganizationDto { id: string; name: string; slug: string; ownerId: string; description?: string; createdAt?: string; }

@Injectable({ providedIn: 'root' })
export class WorkspaceService {
  private http = inject(HttpClient);

  // Signals - selected workspace for sidebar/projects global filter
  selectedWorkspaceId = signal<string | null>(null);

  getMyWorkspaces() {
    return this.http.get<WorkspaceDto[]>(`${environment.apiUrl}/api/workspaces`, { withCredentials: true });
  }
  getMyWorkspacesPaginated(page=1, pageSize=12, search?: string) {
    let params: any = { page, pageSize };
    if (search) params.search = search;
    return this.http.get<{items: WorkspaceDto[]; total:number; page:number; pageSize:number}>(`${environment.apiUrl}/api/workspaces`, { params, withCredentials: true });
  }

  getMyOrganizations() {
    return this.http.get<OrganizationDto[]>(`${environment.apiUrl}/api/organizations`, { withCredentials: true });
  }
  updateOrganization(id: string, name: string, description?: string) {
    return this.http.put<OrganizationDto>(`${environment.apiUrl}/api/organizations/${id}`, { Name: name, Description: description }, { withCredentials: true });
  }
  getOrganizationMembers(organizationId: string) {
    return this.http.get<any[]>(`${environment.apiUrl}/api/organizations/${organizationId}/members`, { withCredentials: true });
  }
  createOrganizationMember(organizationId: string, fullName: string, email: string, password: string, role: string, workspaceId?: string) {
    return this.http.post(`${environment.apiUrl}/api/organizations/${organizationId}/employees`, { FullName: fullName, Email: email, Password: password, Role: role, WorkspaceId: workspaceId }, { withCredentials: true });
  }
  updateOrganizationMember(organizationId: string, userId: string, fullName?: string, email?: string, role?: string, workspaceId?: string) {
    return this.http.put(`${environment.apiUrl}/api/organizations/${organizationId}/employees/${userId}`, { FullName: fullName, Email: email, Role: role, WorkspaceId: workspaceId }, { withCredentials: true });
  }
  deleteOrganizationMember(organizationId: string, userId: string) {
    return this.http.delete(`${environment.apiUrl}/api/organizations/${organizationId}/employees/${userId}`, { withCredentials: true });
  }
  getWorkspaceMembers(workspaceId: string) {
    // alias for consistency
    return this.http.get<any[]>(`${environment.apiUrl}/api/workspaces/${workspaceId}/members`, { withCredentials: true });
  }

  createWorkspace(organizationId: string, name: string) {
    return this.http.post<WorkspaceDto>(`${environment.apiUrl}/api/workspaces`, { organizationId, name }, { withCredentials: true });
  }
  updateWorkspace(id: string, name: string, slug?: string) {
    return this.http.put(`${environment.apiUrl}/api/workspaces/${id}`, { name, slug }, { withCredentials: true });
  }
  deleteWorkspace(id: string) {
    return this.http.delete(`${environment.apiUrl}/api/workspaces/${id}`, { withCredentials: true });
  }
}
