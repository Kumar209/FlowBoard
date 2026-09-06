import { Component, ChangeDetectionStrategy, input, output, signal, computed, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../../core/services/project.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ConfirmDeleteComponent } from '../confirm-delete/confirm-delete.component';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

/**
 * TaskDetailModal - Jira-grade: Subtasks CRUD, Comments CRUD, Assignee picker, Priority/Labels/Due editable, History.
 * MNC-grade: OnPush + input.required + signals + computed + firstValueFrom + injectQuery.
 */
@Component({
  selector: 'app-task-detail-modal',
  standalone: true,
  imports: [CommonModule, ConfirmDeleteComponent],
  templateUrl: './task-detail-modal.component.html',
  styleUrls: ['./task-detail-modal.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskDetailModalComponent {
  open = input<boolean>(false);
  task = input<any>(null);
  lists = input<any[]>([]);
  projectId = input<string>('');
  workspaceId = input<string>('');
  loading = input<boolean>(false);
  closed = output<void>();
  saved = output<{title:string; description:string; priority:string; listId:string; labelsJson?:string; assigneeId?:string; dueDate?:string; issueType?:string; epic?:string; storyPoints?:number; startDate?:string; environment?:string; parentIssueId?:string; sprintId?:string; watchersJson?:string; linkedIssuesJson?:string; timeEstimated?:number; timeSpent?:number; timeRemaining?:number; teamId?:string}>();

  private projectService = inject(ProjectService);
  auth = inject(AuthService);
  private queryClient = inject(QueryClient);

  // Editable fields - Must add per user request: IssueType, Sprint, Epic, StoryPoints, StartDate, Environment, Watchers, LinkedIssues, Time Tracking, ParentIssue
  title = signal('');
  description = signal('');
  priority = signal('Medium');
  listId = signal('');
  labels = signal(''); // comma separated display, will be JSON.stringify on save
  assigneeId = signal('');
  dueDate = signal('');
  issueType = signal('Task');
  epic = signal('');
  storyPoints = signal<number | null>(null);
  startDate = signal('');
  environmentSel = signal('');
  parentIssueId = signal('');
  sprintId = signal('');
  teamId = signal('');
  watchers = signal(''); // comma separated userIds
  linkedIssues = signal(''); // comma separated
  timeEstimated = signal<number | null>(null);
  timeSpent = signal<number | null>(null);
  timeRemaining = signal<number | null>(null);
  watcherSearch = signal('');
  parentSearch = signal('');
  linkedSearch = signal('');
  filteredWatchers = computed(() => {
    const q = this.watcherSearch().toLowerCase().trim();
    const members = this.membersQuery.data() || [];
    if (!q) return members.slice(0,5);
    return members.filter((m:any) => m.email.toLowerCase().includes(q) || m.fullName.toLowerCase().includes(q)).slice(0,5);
  });
  filteredParents = computed(() => {
    const q = this.parentSearch().toLowerCase().trim();
    const tasks = this.boardTasks();
    if (!q) return [];
    return tasks.filter((t:any) => t.title.toLowerCase().includes(q) || t.id.toLowerCase().includes(q)).slice(0,5);
  });
  filteredLinked = computed(() => {
    const q = this.linkedSearch().toLowerCase().trim();
    const tasks = this.boardTasks();
    if (!q) return [];
    return tasks.filter((t:any) => t.title.toLowerCase().includes(q) || t.id.toLowerCase().includes(q)).slice(0,5);
  });
  parentDisplayName = computed(() => {
    const pid = this.parentIssueId();
    if (!pid) return '';
    const t = this.boardTasks().find((x:any) => x.id === pid);
    return t ? `${t.title} (${t.id.slice(0,6)})` : pid.slice(0,6);
  });
  childIssues = computed(() => {
    const tid = this.task()?.id;
    if (!tid) return [];
    return this.boardTasks().filter((t:any) => t.parentIssueId === tid);
  });
  boardTasks = signal<any[]>([]);
  newSubtask = signal('');
  newComment = signal('');
  editingCommentId = signal<string | null>(null);
  editCommentContent = signal('');
  editingSubtaskId = signal<string | null>(null);
  editSubtaskTitle = signal('');
  activeTab = signal<'comments'|'history'>('comments');
  deleteSubtaskConfirmId = signal<string | null>(null);
  deleteCommentConfirmId = signal<string | null>(null);

  // Derived
  labelsJson = computed(() => {
    const raw = this.labels().trim();
    if (!raw) return undefined;
    const arr = raw.split(',').map(s=>s.trim()).filter(Boolean);
    return JSON.stringify(arr);
  });
  watchersJson = computed(() => {
    const raw=this.watchers().trim(); if(!raw) return undefined;
    return JSON.stringify(raw.split(',').map(s=>s.trim()).filter(Boolean));
  });
  linkedIssuesJson = computed(() => {
    const raw=this.linkedIssues().trim(); if(!raw) return undefined;
    return JSON.stringify(raw.split(',').map(s=>s.trim()).filter(Boolean));
  });
  isDirty = computed(() => {
    const t = this.task();
    if(!t) return false;
    const due = t.dueDate ? (t.dueDate as string).slice(0,10) : '';
    const start = t.startDate ? (t.startDate as string).slice(0,10) : '';
    const labelsDisplay = (()=>{ try{ const a=JSON.parse(t.labelsJson||'[]'); return Array.isArray(a)?a.join(', '):'';}catch{return t.labelsJson||''}})();
    const watchersDisplay = (()=>{ try{ const a=JSON.parse(t.watchersJson||'[]'); return Array.isArray(a)?a.join(', '):'';}catch{return ''}})();
    const linkedDisplay = (()=>{ try{ const a=JSON.parse(t.linkedIssuesJson||'[]'); return Array.isArray(a)?a.join(', '):'';}catch{return ''}})();
    return this.title() !== t.title || this.description() !== (t.description||'') || this.priority() !== t.priority || this.listId() !== t.listId || this.labels() !== labelsDisplay || (this.assigneeId()||'') !== (t.assigneeId||'') || this.dueDate() !== due || this.issueType() !== (t.issueType||'Task') || this.epic() !== (t.epic||'') || (this.storyPoints() ?? null) !== (t.storyPoints ?? null) || this.startDate() !== start || this.environmentSel() !== (t.environment||'') || this.parentIssueId() !== (t.parentIssueId||'') || this.sprintId() !== (t.sprintId||'') || this.teamId() !== (t.teamId||'') || this.watchers() !== watchersDisplay || this.linkedIssues() !== linkedDisplay || (this.timeEstimated() ?? null) !== (t.timeEstimated ?? null) || (this.timeSpent() ?? null) !== (t.timeSpent ?? null) || (this.timeRemaining() ?? null) !== (t.timeRemaining ?? null);
  });

  // Queries
  membersQuery = injectQuery(() => ({
    queryKey: ['workspace-members', this.workspaceId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getWorkspaceMembers(this.workspaceId())),
    enabled: this.open() && !!this.workspaceId(),
  }));
  environmentsQuery = injectQuery(() => ({
    queryKey: ['environments', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getEnvironments(this.projectId())),
    enabled: this.open() && !!this.projectId(),
  }));
  sprintsForTaskQuery = injectQuery(() => ({
    queryKey: ['sprints', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getSprints(this.projectId())),
    enabled: this.open() && !!this.projectId(),
  }));
  teamsQuery = injectQuery(() => ({
    queryKey: ['teams', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getTeams(this.projectId())),
    enabled: this.open() && !!this.projectId(),
  }));
  boardForTaskQuery = injectQuery(() => ({
    queryKey: ['board', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getBoard(this.projectId())),
    enabled: this.open() && !!this.projectId(),
  }));
  isScrumBoard = computed(() => {
    const boardId = this.task()?.boardId || '';
    const boards = (this.boardForTaskQuery.data() as any)?.project ? [] : []; // fallback
    // For now, check if task has sprint or if sprints exist - if no sprints, treat as Kanban
    const hasSprints = (this.sprintsForTaskQuery.data()?.length || 0) > 0;
    return hasSprints; // Scrum if project has sprints
  });

  subtasksQuery = injectQuery(() => ({
    queryKey: ['subtasks', this.task()?.id] as const,
    queryFn: () => firstValueFrom(this.projectService.getSubTasks(this.task().id)),
    enabled: this.open() && !!this.task()?.id,
  }));

  commentsQuery = injectQuery(() => ({
    queryKey: ['comments', this.task()?.id] as const,
    queryFn: () => firstValueFrom(this.projectService.getComments(this.task().id)),
    enabled: this.open() && !!this.task()?.id,
  }));

  taskDetailQuery = injectQuery(() => ({
    queryKey: ['task-detail', this.task()?.id] as const,
    queryFn: () => firstValueFrom(this.projectService.getTaskDetail(this.task().id)),
    enabled: this.open() && !!this.task()?.id,
    staleTime: 0,
  }));

  historyQuery = injectQuery(() => ({
    queryKey: ['activities-task', this.projectId(), this.task()?.id] as const,
    queryFn: () => firstValueFrom(this.projectService.getActivities(this.projectId(), 1, 20, this.task()?.id)),
    enabled: this.open() && !!this.projectId() && !!this.task()?.id && this.activeTab()==='history',
  }));

  // Mutations - invalidate board + activities for realtime (no refresh needed)
  createSubtaskMut = injectMutation(() => ({
    mutationFn: (title:string) => firstValueFrom(this.projectService.createSubTask(this.task().id, title)),
    onSuccess: () => { this.queryClient.invalidateQueries({queryKey:['subtasks', this.task().id]}); this.queryClient.invalidateQueries({queryKey:['board', this.projectId()]}); this.queryClient.invalidateQueries({queryKey:['activities', this.projectId()]}); this.queryClient.invalidateQueries({queryKey:['activities-task', this.projectId(), this.task().id]}); this.newSubtask.set(''); }
  }));
  toggleSubtaskMut = injectMutation(() => ({
    mutationFn: (id:string) => firstValueFrom(this.projectService.toggleSubTask(id)),
    onSuccess: () => { this.queryClient.invalidateQueries({queryKey:['subtasks', this.task().id]}); this.queryClient.invalidateQueries({queryKey:['board', this.projectId()]}); this.queryClient.invalidateQueries({queryKey:['activities-task', this.projectId(), this.task().id]}); }
  }));
  updateSubtaskMut = injectMutation(() => ({
    mutationFn: (vars:{id:string; title:string}) => firstValueFrom(this.projectService.updateSubTask(vars.id, vars.title)),
    onSuccess: () => { this.queryClient.invalidateQueries({queryKey:['subtasks', this.task().id]}); this.queryClient.invalidateQueries({queryKey:['activities-task', this.projectId(), this.task().id]}); this.editingSubtaskId.set(null); }
  }));
  deleteSubtaskMut = injectMutation(() => ({
    mutationFn: (id:string) => firstValueFrom(this.projectService.deleteSubTask(id)),
    onSuccess: () => { this.queryClient.invalidateQueries({queryKey:['subtasks', this.task().id]}); this.queryClient.invalidateQueries({queryKey:['board', this.projectId()]}); this.queryClient.invalidateQueries({queryKey:['activities-task', this.projectId(), this.task().id]}); }
  }));
  createCommentMut = injectMutation(() => ({
    mutationFn: (content:string) => firstValueFrom(this.projectService.createComment(this.task().id, content)),
    onSuccess: () => { this.queryClient.invalidateQueries({queryKey:['comments', this.task().id]}); this.queryClient.invalidateQueries({queryKey:['board', this.projectId()]}); this.queryClient.invalidateQueries({queryKey:['activities', this.projectId()]}); this.newComment.set(''); this.queryClient.invalidateQueries({queryKey:['activities-task', this.projectId(), this.task().id]}); }
  }));
  updateCommentMut = injectMutation(() => ({
    mutationFn: (vars:{id:string; content:string}) => firstValueFrom(this.projectService.updateComment(vars.id, vars.content)),
    onSuccess: () => { this.queryClient.invalidateQueries({queryKey:['comments', this.task().id]}); this.queryClient.invalidateQueries({queryKey:['activities-task', this.projectId(), this.task().id]}); this.editingCommentId.set(null); }
  }));
  deleteCommentMut = injectMutation(() => ({
    mutationFn: (id:string) => firstValueFrom(this.projectService.deleteComment(id)),
    onSuccess: () => { this.queryClient.invalidateQueries({queryKey:['comments', this.task().id]}); this.queryClient.invalidateQueries({queryKey:['activities-task', this.projectId(), this.task().id]}); }
  }));

  currentUserId = computed(() => this.auth.currentUser()?.id || '');

  private setTeamIdNormalized(rawId: string | null | undefined) {
    if (!rawId) { this.teamId.set(''); return; }
    const teams = this.teamsQuery.data() || [];
    const matched = teams.find((tm:any) => tm.id.toLowerCase() === rawId.toLowerCase());
    this.teamId.set(matched ? matched.id : rawId);
  }
  private setSprintIdNormalized(rawId: string | null | undefined) {
    if (!rawId) { this.sprintId.set(''); return; }
    const sprints = this.sprintsForTaskQuery.data() || [];
    const matched = sprints.find((s:any) => s.id.toLowerCase() === rawId.toLowerCase() || s.name.toLowerCase() === rawId.toLowerCase());
    this.sprintId.set(matched ? matched.id : rawId);
  }

  constructor() {
    effect(() => {
      if (this.open() && this.task()) {
        const t = this.task();
        this.title.set(t.title || '');
        this.description.set(t.description || '');
        this.priority.set(t.priority || 'Medium');
        this.listId.set(t.listId || '');
        try { const a=JSON.parse(t.labelsJson||'[]'); this.labels.set(Array.isArray(a)?a.join(', '): (t.labelsJson||'')); } catch { this.labels.set(t.labelsJson||''); }
        this.assigneeId.set(t.assigneeId || '');
        this.dueDate.set(t.dueDate ? (t.dueDate as string).slice(0,10) : '');
        this.issueType.set(t.issueType || 'Task');
        this.epic.set(t.epic || '');
        this.storyPoints.set(t.storyPoints ?? null);
        this.startDate.set(t.startDate ? (t.startDate as string).slice(0,10) : '');
        this.environmentSel.set(t.environment || '');
        this.parentIssueId.set(t.parentIssueId || '');
        this.setSprintIdNormalized(t.sprintId);
        this.setTeamIdNormalized(t.teamId);
        try { const w=JSON.parse(t.watchersJson||'[]'); this.watchers.set(Array.isArray(w)?w.join(', '):''); } catch { this.watchers.set(''); }
        try { const l=JSON.parse(t.linkedIssuesJson||'[]'); this.linkedIssues.set(Array.isArray(l)?l.join(', '):''); } catch { this.linkedIssues.set(''); }
        this.timeEstimated.set(t.timeEstimated ?? null);
        this.timeSpent.set(t.timeSpent ?? null);
        this.timeRemaining.set(t.timeRemaining ?? null);
        this.activeTab.set('comments');
      }
    });
    effect(() => {
      const detail = this.taskDetailQuery.data() as any;
      if (detail?.task && this.open()) {
        const t = detail.task;
        this.title.set(t.title || '');
        this.description.set(t.description || '');
        this.priority.set(t.priority || 'Medium');
        this.listId.set(t.listId || '');
        try { const a=JSON.parse(t.labelsJson||'[]'); this.labels.set(Array.isArray(a)?a.join(', '): (t.labelsJson||'')); } catch { this.labels.set(t.labelsJson||''); }
        this.assigneeId.set(t.assigneeId || '');
        this.dueDate.set(t.dueDate ? (t.dueDate as string).slice(0,10) : '');
        this.issueType.set(t.issueType || 'Task');
        this.epic.set(t.epic || '');
        this.storyPoints.set(t.storyPoints ?? null);
        this.startDate.set(t.startDate ? (t.startDate as string).slice(0,10) : '');
        this.environmentSel.set(t.environment || '');
        this.parentIssueId.set(t.parentIssueId || '');
        this.setSprintIdNormalized(t.sprintId);
        this.setTeamIdNormalized(t.teamId);
        try { const w=JSON.parse(t.watchersJson||'[]'); this.watchers.set(Array.isArray(w)?w.join(', '):''); } catch { this.watchers.set(''); }
        try { const l=JSON.parse(t.linkedIssuesJson||'[]'); this.linkedIssues.set(Array.isArray(l)?l.join(', '):''); } catch { this.linkedIssues.set(''); }
        this.timeEstimated.set(t.timeEstimated ?? null);
        this.timeSpent.set(t.timeSpent ?? null);
        this.timeRemaining.set(t.timeRemaining ?? null);
      }
    });
    // Re-sync when teams/sprints load to fix case-mismatch (teamId lowercase vs option id case)
    effect(() => {
      const teams = this.teamsQuery.data();
      if (teams && this.open() && this.task()) {
        const raw = this.task()?.teamId || this.teamId();
        if (raw) this.setTeamIdNormalized(raw);
      }
    });
    effect(() => {
      const sprints = this.sprintsForTaskQuery.data();
      if (sprints && this.open() && this.task()) {
        const raw = this.task()?.sprintId || this.sprintId();
        if (raw) this.setSprintIdNormalized(raw);
      }
    });
    effect(() => {
      const board = this.boardForTaskQuery.data();
      if (board && (board as any).tasks) {
        this.boardTasks.set((board as any).tasks);
      }
    });
  }

  save() {
    this.saved.emit({title: this.title().trim(), description: this.description().trim(), priority: this.priority(), listId: this.listId(), labelsJson: this.labelsJson(), assigneeId: this.assigneeId() || undefined, dueDate: this.dueDate() || undefined, issueType: this.issueType(), epic: this.epic().trim() || undefined, storyPoints: this.storyPoints() ?? undefined, startDate: this.startDate() || undefined, environment: this.environmentSel() || undefined, parentIssueId: this.parentIssueId() || undefined, sprintId: this.sprintId() || undefined, watchersJson: this.watchersJson(), linkedIssuesJson: this.linkedIssuesJson(), timeEstimated: this.timeEstimated() ?? undefined, timeSpent: this.timeSpent() ?? undefined, timeRemaining: this.timeRemaining() ?? undefined, teamId: this.teamId() || undefined});
  }
  addSubtask(){ const v=this.newSubtask().trim(); if(!v) return; this.createSubtaskMut.mutate(v); }
  addComment(){ const v=this.newComment().trim(); if(!v) return; this.createCommentMut.mutate(v); }
  startEditComment(c:any){ this.editingCommentId.set(c.id); this.editCommentContent.set(c.content); }
  saveEditComment(){ const id=this.editingCommentId(); const v=this.editCommentContent().trim(); if(!id||!v) return; this.updateCommentMut.mutate({id, content:v}); }
  startEditSubtask(s:any){ this.editingSubtaskId.set(s.id); this.editSubtaskTitle.set(s.title); }
  saveEditSubtask(){ const id=this.editingSubtaskId(); const v=this.editSubtaskTitle().trim(); if(!id||!v) return; this.updateSubtaskMut.mutate({id, title:v}); }
  confirmDeleteSubtask(id:string){ this.deleteSubtaskConfirmId.set(id); }
  doDeleteSubtask(){ const id=this.deleteSubtaskConfirmId(); if(!id) return; this.deleteSubtaskMut.mutate(id); this.deleteSubtaskConfirmId.set(null); }
  confirmDeleteComment(id:string){ this.deleteCommentConfirmId.set(id); }
  doDeleteComment(){ const id=this.deleteCommentConfirmId(); if(!id) return; this.deleteCommentMut.mutate(id); this.deleteCommentConfirmId.set(null); }
  getAuthorDisplay(authorId:string){
    const members = this.membersQuery.data() || [];
    const m = members.find((x:any)=> x.userId===authorId);
    if (m) return { name: m.fullName, avatar: m.avatarUrl, email: m.email };
    // Fallback to current user if author is self
    if (authorId===this.currentUserId()) {
      const u=this.auth.currentUser();
      return { name: u?.fullName || 'You', avatar: u?.avatarUrl, email: u?.email || '' };
    }
    return { name: authorId.slice(0,8), avatar: undefined, email: '' };
  }
}
