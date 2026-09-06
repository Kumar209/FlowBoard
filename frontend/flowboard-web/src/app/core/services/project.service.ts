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
export interface TaskItem { id: string; projectId: string; listId: string; title: string; description?: string; priority: string; labelsJson?: string; assigneeId?: string; position: number; createdAt: string; dueDate?: string; issueType?: string; epic?: string; storyPoints?: number; startDate?: string; environment?: string; parentIssueId?: string; sprintId?: string; watchersJson?: string; linkedIssuesJson?: string; timeEstimated?: number; timeSpent?: number; timeRemaining?: number; teamId?: string; status?: string; }
export interface BoardDtoFull { id: string; projectId: string; name: string; type: string; description?: string; position: number; createdAt: string; filterJson?: string | null; }
export interface SprintDto { id: string; projectId: string; boardId: string; name: string; startDate: string; endDate: string; status: string; createdAt: string; }
export interface ProjectEnvironmentDto { id: string; projectId: string; name: string; url: string; description?: string; status: string; createdAt: string; }
export interface BoardDto { project: Project; lists: BoardList[]; tasks: TaskItem[]; }
export interface ActivityDto { id: string; projectId: string; taskId?: string; actorId: string; action: string; payloadJson?: string; occurredAt: string; }
export interface SubTaskDto { id: string; taskId: string; title: string; isCompleted: boolean; createdAt: string; }
export interface CommentDto { id: string; taskId: string; authorId: string; content: string; createdAt: string; }
export interface TaskDetailDto { task: TaskItem; subTasks: SubTaskDto[]; comments: CommentDto[]; }
export interface WorkspaceMemberDto { userId: string; email: string; fullName: string; avatarUrl?: string; role: string; roleInt: number; joinedAt: string; }

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
  createList(projectId: string, name: string, boardId?: string, position?: number) {
    return this.http.post<BoardList>(`${environment.apiUrl}/api/projects/${projectId}/lists`, { Name: name, BoardId: boardId || null, Position: position ?? null }, { withCredentials: true });
  }
  renameList(projectId: string, listId: string, name: string, position?: number) {
    const body:any = { Name: name };
    if (position !== undefined && position !== null) body.Position = position;
    return this.http.put<BoardList>(`${environment.apiUrl}/api/projects/${projectId}/lists/${listId}`, body, { withCredentials: true });
  }
  getBoard(projectId: string, boardId?: string) {
    let params:any = {};
    if (boardId) params.boardId = boardId;
    return this.http.get<BoardDto>(`${environment.apiUrl}/api/projects/${projectId}/board`, { params, withCredentials: true });
  }
  deleteList(projectId: string, listId: string) {
    return this.http.delete(`${environment.apiUrl}/api/projects/${projectId}/lists/${listId}`, { withCredentials: true });
  }
  getWorkspaceMembers(workspaceId: string) {
    return this.http.get<WorkspaceMemberDto[]>(`${environment.apiUrl}/api/workspaces/${workspaceId}/members`, { withCredentials: true });
  }
  // Boards (Enterprise: Project → Boards)
  getBoards(projectId: string) {
    return this.http.get<BoardDtoFull[]>(`${environment.apiUrl}/api/projects/${projectId}/boards`, { withCredentials: true });
  }
  createBoard(projectId: string, name: string, type: string = 'Kanban', description?: string, filterJson?: string | null) {
    return this.http.post<BoardDtoFull>(`${environment.apiUrl}/api/projects/${projectId}/boards`, { Name: name, Type: type, Description: description, FilterJson: filterJson }, { withCredentials: true });
  }
  updateBoard(boardId: string, name: string, type: string = 'Kanban', filterJson?: string | null) {
    return this.http.put<BoardDtoFull>(`${environment.apiUrl}/api/boards/${boardId}`, { Name: name, Type: type, FilterJson: filterJson }, { withCredentials: true });
  }
  deleteBoard(boardId: string) {
    return this.http.delete(`${environment.apiUrl}/api/boards/${boardId}`, { withCredentials: true });
  }
  // Sprints (Board → Sprint)
  getSprints(projectId: string, boardId?: string) {
    let params:any = {};
    if (boardId) params.boardId = boardId;
    return this.http.get<SprintDto[]>(`${environment.apiUrl}/api/projects/${projectId}/sprints`, { params, withCredentials: true });
  }
  createSprint(projectId: string, boardId: string | null, name: string, startDate: string, endDate: string) {
    return this.http.post<SprintDto>(`${environment.apiUrl}/api/projects/${projectId}/sprints`, { BoardId: boardId, Name: name, StartDate: startDate, EndDate: endDate }, { withCredentials: true });
  }
  updateSprint(sprintId: string, name: string, startDate: string, endDate: string) {
    return this.http.put<SprintDto>(`${environment.apiUrl}/api/sprints/${sprintId}`, { Name: name, StartDate: startDate, EndDate: endDate }, { withCredentials: true });
  }
  deleteSprint(sprintId: string) {
    return this.http.delete(`${environment.apiUrl}/api/sprints/${sprintId}`, { withCredentials: true });
  }
  // Environments (Name/URL/Description/Status)
  getEnvironments(projectId: string) {
    return this.http.get<ProjectEnvironmentDto[]>(`${environment.apiUrl}/api/projects/${projectId}/environments`, { withCredentials: true });
  }
  createEnvironment(projectId: string, name: string, url: string, description?: string, status: string = 'Active') {
    return this.http.post<ProjectEnvironmentDto>(`${environment.apiUrl}/api/projects/${projectId}/environments`, { Name: name, Url: url, Description: description, Status: status }, { withCredentials: true });
  }
  updateEnvironment(environmentId: string, name: string, url: string, description?: string, status: string = 'Active') {
    return this.http.put<ProjectEnvironmentDto>(`${environment.apiUrl}/api/environments/${environmentId}`, { Name: name, Url: url, Description: description, Status: status }, { withCredentials: true });
  }
  deleteEnvironment(environmentId: string) {
    return this.http.delete(`${environment.apiUrl}/api/environments/${environmentId}`, { withCredentials: true });
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

  createTask(projectId: string, listId: string, title: string, description?: string, priority = 'Medium', labelsJson?: string, assigneeId?: string, dueDate?: string, issueType: string = 'Task', epic?: string, storyPoints?: number, startDate?: string, taskEnv?: string, parentIssueId?: string, sprintId?: string, teamId?: string) {
    return this.http.post<TaskItem>(`${environment.apiUrl}/api/tasks`, { projectId, listId, title, description, priority, labelsJson, assigneeId, dueDate, issueType, epic, storyPoints, startDate, environment: taskEnv, parentIssueId, sprintId, teamId }, { withCredentials: true });
  }

  updateTask(taskId: string, title: string, description?: string, priority = 'Medium', listId?: string, labelsJson?: string, assigneeId?: string, dueDate?: string, issueType?: string, epic?: string, storyPoints?: number, startDate?: string, taskEnv?: string, parentIssueId?: string, sprintId?: string, watchersJson?: string, linkedIssuesJson?: string, timeEstimated?: number, timeSpent?: number, timeRemaining?: number, teamId?: string) {
    return this.http.put(`${environment.apiUrl}/api/tasks/${taskId}`, { title, description, priority, listId, labelsJson, assigneeId, dueDate, issueType, epic, storyPoints, startDate, environment: taskEnv, parentIssueId, sprintId, watchersJson, linkedIssuesJson, timeEstimated, timeSpent, timeRemaining, teamId }, { withCredentials: true });
  }
  // Teams
  getTeams(projectId: string) {
    return this.http.get<any[]>(`${environment.apiUrl}/api/projects/${projectId}/teams`, { withCredentials: true });
  }
  createTeam(projectId: string, name: string, description?: string) {
    return this.http.post<any>(`${environment.apiUrl}/api/projects/${projectId}/teams`, { Name: name, Description: description }, { withCredentials: true });
  }
  updateTeam(teamId: string, name: string, description?: string) {
    return this.http.put<any>(`${environment.apiUrl}/api/teams/${teamId}`, { Name: name, Description: description }, { withCredentials: true });
  }
  deleteTeam(teamId: string) {
    return this.http.delete(`${environment.apiUrl}/api/teams/${teamId}`, { withCredentials: true });
  }
  getTeamMembers(teamId: string) {
    return this.http.get<any[]>(`${environment.apiUrl}/api/teams/${teamId}/members`, { withCredentials: true });
  }
  addTeamMember(teamId: string, userId: string) {
    return this.http.post<any>(`${environment.apiUrl}/api/teams/${teamId}/members`, { UserId: userId }, { withCredentials: true });
  }
  removeTeamMember(teamId: string, userId: string) {
    return this.http.delete(`${environment.apiUrl}/api/teams/${teamId}/members/${userId}`, { withCredentials: true });
  }
  getTaskDetail(taskId: string) {
    return this.http.get<TaskDetailDto>(`${environment.apiUrl}/api/tasks/${taskId}/detail`, { withCredentials: true });
  }

  moveTask(taskId: string, toListId: string, newPosition: number) {
    return this.http.put(`${environment.apiUrl}/api/tasks/${taskId}/move`, { toListId, newPosition }, { withCredentials: true });
  }

  getActivities(projectId: string, page=1, pageSize=20, taskId?: string) {
    let params: any = { page, pageSize };
    if (taskId) params.taskId = taskId;
    return this.http.get<{items: ActivityDto[]; total:number; page:number; pageSize:number}>(`${environment.apiUrl}/api/projects/${projectId}/activities`, { params, withCredentials: true });
  }
  // Subtasks
  getSubTasks(taskId: string) {
    return this.http.get<SubTaskDto[]>(`${environment.apiUrl}/api/tasks/${taskId}/subtasks`, { withCredentials: true });
  }
  createSubTask(taskId: string, title: string) {
    return this.http.post<SubTaskDto>(`${environment.apiUrl}/api/tasks/${taskId}/subtasks`, { title }, { withCredentials: true });
  }
  updateSubTask(subTaskId: string, title: string) {
    return this.http.put<SubTaskDto>(`${environment.apiUrl}/api/subtasks/${subTaskId}`, { title }, { withCredentials: true });
  }
  toggleSubTask(subTaskId: string) {
    return this.http.put<SubTaskDto>(`${environment.apiUrl}/api/subtasks/${subTaskId}/toggle`, {}, { withCredentials: true });
  }
  deleteSubTask(subTaskId: string) {
    return this.http.delete(`${environment.apiUrl}/api/subtasks/${subTaskId}`, { withCredentials: true });
  }
  deleteTask(taskId: string) {
    return this.http.delete(`${environment.apiUrl}/api/tasks/${taskId}`, { withCredentials: true });
  }
  // Comments
  getComments(taskId: string) {
    return this.http.get<CommentDto[]>(`${environment.apiUrl}/api/tasks/${taskId}/comments`, { withCredentials: true });
  }
  createComment(taskId: string, content: string) {
    return this.http.post<CommentDto>(`${environment.apiUrl}/api/tasks/${taskId}/comments`, { content }, { withCredentials: true });
  }
  updateComment(commentId: string, content: string) {
    return this.http.put<CommentDto>(`${environment.apiUrl}/api/comments/${commentId}`, { content }, { withCredentials: true });
  }
  deleteComment(commentId: string) {
    return this.http.delete(`${environment.apiUrl}/api/comments/${commentId}`, { withCredentials: true });
  }
}
