import { Injectable, signal, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';

/**
 * ProjectService - MNC-grade: inject(HttpClient) + signals + typed HttpClient + firstValueFrom in components (not toPromise).
 * Why not simple? inject() is tree-shakable for standalone, signals give fine-grained state (selectedProject/filters) without NgRx.
 * All methods return Observable<T> (HttpClient) - caller does firstValueFrom() in TanStack queryFn (MNC uses firstValueFrom, not deprecated toPromise().then(r=>r!)).
 */

export interface Project { id: string; workspaceId: string; name: string; key: string; description?: string; ownerId: string; createdAt: string; }
export interface BoardList { id: string; projectId: string; name: string; position: number; }
export interface TaskItem { id: string; projectId: string; listId: string; title: string; description?: string; priority: string; labelsJson?: string; assigneeId?: string; position: number; createdAt: string; }
export interface BoardDto { project: Project; lists: BoardList[]; tasks: TaskItem[]; }
export interface ActivityDto { id: string; projectId: string; taskId?: string; actorId: string; action: string; payloadJson?: string; occurredAt: string; }

@Injectable({ providedIn: 'root' })
export class ProjectService {
  // Signals - client state (no NgRx, MNC-grade) - fine-grained, OnPush components read via signal()
  selectedProject = signal<Project | null>(null);
  selectedWorkspaceId = signal<string | null>(null);
  filters = signal<{ search?: string; assigneeId?: string; priority?: string; label?: string }>({});

  private http = inject(HttpClient);

  // Projects
  getProjects(workspaceId: string, page = 1, pageSize = 20) {
    return this.http.get<{ items: Project[]; total: number; page: number; pageSize: number }>(
      `${environment.apiUrl}/api/workspaces/${workspaceId}/projects`, { params: { page, pageSize } as any, withCredentials: true }
    );
  }

  createProject(workspaceId: string, name: string, description?: string) {
    return this.http.post<Project>(`${environment.apiUrl}/api/workspaces/${workspaceId}/projects`, { name, description }, { withCredentials: true });
  }
  updateProject(projectId: string, name: string, description?: string, slug?: string) {
    return this.http.put<Project>(`${environment.apiUrl}/api/projects/${projectId}`, { name, description, slug }, { withCredentials: true });
  }
  deleteProject(projectId: string) {
    return this.http.delete(`${environment.apiUrl}/api/projects/${projectId}`, { withCredentials: true });
  }
  createList(projectId: string, name: string) {
    return this.http.post<BoardList>(`${environment.apiUrl}/api/projects/${projectId}/lists`, { name }, { withCredentials: true });
  }

  // Board
  getBoard(projectId: string) {
    return this.http.get<BoardDto>(`${environment.apiUrl}/api/projects/${projectId}/board`, { withCredentials: true });
  }

  // Tasks with filtering
  getTasks(projectId: string, opts: { search?: string; assigneeId?: string; priority?: string; label?: string; page?: number; pageSize?: number } = {}) {
    let params = new HttpParams().set('projectId', projectId);
    if (opts.search) params = params.set('search', opts.search);
    if (opts.assigneeId) params = params.set('assigneeId', opts.assigneeId);
    if (opts.priority) params = params.set('priority', opts.priority);
    if (opts.label) params = params.set('label', opts.label);
    if (opts.page) params = params.set('page', opts.page);
    if (opts.pageSize) params = params.set('pageSize', opts.pageSize);
    return this.http.get<{ items: TaskItem[]; total: number }>(`${environment.apiUrl}/api/tasks`, { params, withCredentials: true });
  }

  createTask(projectId: string, listId: string, title: string, description?: string, priority = 'Medium') {
    return this.http.post<TaskItem>(`${environment.apiUrl}/api/tasks`, { projectId, listId, title, description, priority }, { withCredentials: true });
  }

  updateTask(taskId: string, title: string, description?: string, priority = 'Medium', listId?: string) {
    return this.http.put(`${environment.apiUrl}/api/tasks/${taskId}`, { title, description, priority, listId }, { withCredentials: true });
  }

  moveTask(taskId: string, toListId: string, newPosition: number) {
    return this.http.put(`${environment.apiUrl}/api/tasks/${taskId}/move`, { toListId, newPosition }, { withCredentials: true });
  }

  getActivities(projectId: string, page=1, pageSize=20) {
    return this.http.get<{items: ActivityDto[]; total:number; page:number; pageSize:number}>(`${environment.apiUrl}/api/projects/${projectId}/activities`, { params: { page, pageSize } as any, withCredentials: true });
  }
}
