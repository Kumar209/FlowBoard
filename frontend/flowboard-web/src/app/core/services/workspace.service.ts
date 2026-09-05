import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

/**
 * WorkspaceService - MNC-grade: inject(HttpClient) + signals.
 * Wraps Identity Service workspaces via Gateway :5000 -> :5001.
 * Used by Workspaces list w (all) and Workspace detail w/:wid.
 */
export interface WorkspaceDto { id: string; name: string; slug: string; organizationId: string; role: number; roleName?: string; }
export interface OrganizationDto { id: string; name: string; slug: string; ownerId: string; }

@Injectable({ providedIn: 'root' })
export class WorkspaceService {
  private http = inject(HttpClient);

  // Signals - selected workspace for sidebar/projects global filter
  selectedWorkspaceId = signal<string | null>(null);

  getMyWorkspaces() {
    return this.http.get<WorkspaceDto[]>(`${environment.apiUrl}/api/workspaces`, { withCredentials: true });
  }

  getMyOrganizations() {
    return this.http.get<OrganizationDto[]>(`${environment.apiUrl}/api/organizations`, { withCredentials: true });
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
