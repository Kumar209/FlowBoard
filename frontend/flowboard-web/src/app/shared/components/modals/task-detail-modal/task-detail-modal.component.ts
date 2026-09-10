import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
  effect,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../../core/services/project.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { ConfirmDeleteComponent } from '../confirm-delete/confirm-delete.component';
import { LoaderComponent } from '../../loader/loader.component';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

/**
 * TaskDetailModal - Jira-grade: Subtasks CRUD, Comments CRUD, Assignee picker, Priority/Labels/Due editable, History.
 * MNC-grade: OnPush + input.required + signals + computed + firstValueFrom + injectQuery.
 */
@Component({
  selector: 'app-task-detail-modal',
  standalone: true,
  imports: [CommonModule, ConfirmDeleteComponent, LoaderComponent],
  templateUrl: './task-detail-modal.component.html',
  styleUrls: ['./task-detail-modal.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDetailModalComponent {
  open = input<boolean>(false);
  task = input<any>(null);
  lists = input<any[]>([]);
  projectId = input<string>('');
  workspaceId = input<string>('');
  loading = input<boolean>(false);
  closed = output<void>();
  saved = output<{
    title: string;
    description: string;
    priority: string;
    listId: string;
    labelsJson?: string;
    assigneeId?: string;
    dueDate?: string;
    issueType?: string;
    epic?: string;
    storyPoints?: number;
    startDate?: string;
    environment?: string;
    parentIssueId?: string;
    sprintId?: string;
    watchersJson?: string;
    linkedIssuesJson?: string;
    timeEstimated?: number;
    timeSpent?: number;
    timeRemaining?: number;
    teamId?: string;
    statusId?: string;
  }>();

  private projectService = inject(ProjectService);
  auth = inject(AuthService);
  private toast = inject(ToastService);
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
  statusId = signal('');
  readOnly = input<boolean>(false);
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
    const list = this.projectMembersList();
    if (!q) return list.slice(0, 5);
    return list
      .filter(
        (m: any) =>
          (m.email || '').toLowerCase().includes(q) || (m.fullName || '').toLowerCase().includes(q),
      )
      .slice(0, 5);
  });
  filteredParents = computed(() => {
    const q = this.parentSearch().toLowerCase().trim();
    const tasks = this.boardTasks();
    if (!q) return [];
    return tasks
      .filter((t: any) => t.title.toLowerCase().includes(q) || t.id.toLowerCase().includes(q))
      .slice(0, 5);
  });
  filteredLinked = computed(() => {
    const q = this.linkedSearch().toLowerCase().trim();
    const tasks = this.boardTasks();
    if (!q) return [];
    return tasks
      .filter((t: any) => t.title.toLowerCase().includes(q) || t.id.toLowerCase().includes(q))
      .slice(0, 5);
  });
  parentDisplayName = computed(() => {
    const pid = this.parentIssueId();
    if (!pid) return '';
    const t = this.boardTasks().find((x: any) => x.id === pid);
    return t ? `${t.title} (${t.id.slice(0, 6)})` : pid.slice(0, 6);
  });
  childIssues = computed(() => {
    const tid = this.task()?.id;
    if (!tid) return [];
    return this.boardTasks().filter((t: any) => t.parentIssueId === tid);
  });
  boardTasks = signal<any[]>([]);
  newSubtask = signal('');
  newComment = signal('');
  editingCommentId = signal<string | null>(null);
  editCommentContent = signal('');
  editingSubtaskId = signal<string | null>(null);
  editSubtaskTitle = signal('');
  activeTab = signal<'comments' | 'history'>('comments');
  deleteSubtaskConfirmId = signal<string | null>(null);
  deleteCommentConfirmId = signal<string | null>(null);

  // Derived
  labelsJson = computed(() => {
    const raw = this.labels().trim();
    if (!raw) return undefined;
    const arr = raw
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean);
    return JSON.stringify(arr);
  });
  watchersJson = computed(() => {
    const raw = this.watchers().trim();
    if (!raw) return undefined;
    return JSON.stringify(
      raw
        .split(',')
        .map((s) => s.trim())
        .filter(Boolean),
    );
  });
  linkedIssuesJson = computed(() => {
    const raw = this.linkedIssues().trim();
    if (!raw) return undefined;
    return JSON.stringify(
      raw
        .split(',')
        .map((s) => s.trim())
        .filter(Boolean),
    );
  });
  isDirty = computed(() => {
    const t = this.task();
    if (!t) return false;
    const due = t.dueDate ? (t.dueDate as string).slice(0, 10) : '';
    const start = t.startDate ? (t.startDate as string).slice(0, 10) : '';
    const labelsDisplay = (() => {
      try {
        const a = JSON.parse(t.labelsJson || '[]');
        return Array.isArray(a) ? a.join(', ') : '';
      } catch {
        return t.labelsJson || '';
      }
    })();
    const watchersDisplay = (() => {
      try {
        const a = JSON.parse(t.watchersJson || '[]');
        return Array.isArray(a) ? a.join(', ') : '';
      } catch {
        return '';
      }
    })();
    const linkedDisplay = (() => {
      try {
        const a = JSON.parse(t.linkedIssuesJson || '[]');
        return Array.isArray(a) ? a.join(', ') : '';
      } catch {
        return '';
      }
    })();
    return (
      this.title() !== t.title ||
      this.description() !== (t.description || '') ||
      this.priority() !== t.priority ||
      this.listId() !== t.listId ||
      this.labels() !== labelsDisplay ||
      (this.assigneeId() || '') !== (t.assigneeId || '') ||
      this.dueDate() !== due ||
      this.issueType() !== (t.issueType || 'Task') ||
      this.epic() !== (t.epic || '') ||
      (this.storyPoints() ?? null) !== (t.storyPoints ?? null) ||
      this.startDate() !== start ||
      this.environmentSel() !== (t.environment || '') ||
      this.parentIssueId() !== (t.parentIssueId || '') ||
      this.sprintId() !== (t.sprintId || '') ||
      this.teamId() !== (t.teamId || '') ||
      this.statusId() !== (t.statusId || '') ||
      this.watchers() !== watchersDisplay ||
      this.linkedIssues() !== linkedDisplay ||
      (this.timeEstimated() ?? null) !== (t.timeEstimated ?? null) ||
      (this.timeSpent() ?? null) !== (t.timeSpent ?? null) ||
      (this.timeRemaining() ?? null) !== (t.timeRemaining ?? null)
    );
  });

  // Queries — MNC: assignee/watchers derive from Project Members (not workspace). WorkspaceMembers only used to populate Project Members.
  effectiveProjectId = computed(() => this.projectId() || this.task()?.projectId || '');


  membersQuery = injectQuery(() => ({
    queryKey: ['assignee-candidates', this.effectiveProjectId()] as const,
    queryFn: async () => {
      const pid = this.effectiveProjectId();
      try {
        const res: any = await firstValueFrom(this.projectService.getAssigneeCandidates(pid));
        if (Array.isArray(res)) return res;
        return res?.items ?? res?.Items ?? [];
      } catch {
        // fallback to project members if candidates endpoint unavailable
        const res: any = await firstValueFrom(this.projectService.getProjectMembers(pid, 1, 100));
        const items = res?.items ?? res?.Items ?? (Array.isArray(res) ? res : []);
        return Array.isArray(items) ? items : [];
      }
    },
    enabled: !!this.effectiveProjectId(),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  }));
  // Normalized for template @for (handles paginated object vs array) — now intersection org ∩ workspace ∩ project
  projectMembersList = computed(() => {
    const raw: any = this.membersQuery.data();
    if (!raw) return [];
    if (Array.isArray(raw)) return raw;
    const items = raw.items ?? raw.Items ?? [];
    return Array.isArray(items) ? items : [];
  });
  environmentsQuery = injectQuery(() => ({
    queryKey: ['environments', this.effectiveProjectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getEnvironments(this.effectiveProjectId())),
    enabled: !!this.effectiveProjectId(),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  }));
  sprintsForTaskQuery = injectQuery(() => ({
    queryKey: ['sprints', this.effectiveProjectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getSprints(this.effectiveProjectId())),
    enabled: !!this.effectiveProjectId(),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  }));
  teamsQuery = injectQuery(() => ({
    queryKey: ['teams', this.effectiveProjectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getTeams(this.effectiveProjectId())),
    enabled: !!this.effectiveProjectId(),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  }));
  statusesQuery = injectQuery(() => ({
    queryKey: ['statuses', this.effectiveProjectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getStatuses(this.effectiveProjectId())),
    enabled: !!this.effectiveProjectId(),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  }));
  boardForTaskQuery = injectQuery(() => ({
    queryKey: ['board', this.effectiveProjectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getBoard(this.effectiveProjectId())),
    enabled: !!this.effectiveProjectId(),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  }));
  isDetailReady = computed(() => {
    const detailTask = (this.taskDetailQuery.data() as any)?.task;
    const t = detailTask ?? this.task();
    if (!t) return false;
    if (this.taskDetailQuery.isPending() || this.statusesQuery.isPending()) return false;
    if (this.membersQuery.isPending() || this.teamsQuery.isPending() || this.sprintsForTaskQuery.isPending()) return false;
    const teamId = (t.teamId || '').toString().toLowerCase();
    const sprintId = (t.sprintId || '').toString().toLowerCase();
    const assigneeId = (t.assigneeId || '').toString().toLowerCase();
    const statusId = (t.statusId || '').toString().toLowerCase();
    if (teamId && !this.teamsQuery.data()?.some((x: any) => (x.id || '').toString().toLowerCase() === teamId)) return false;
    if (sprintId && !this.sprintsForTaskQuery.data()?.some((x: any) => (x.id || '').toString().toLowerCase() === sprintId)) return false;
    if (assigneeId && !this.projectMembersList().some((m: any) => (m.userId || '').toString().toLowerCase() === assigneeId)) return false;
    if (statusId && !this.statusesQuery.data()?.some((x: any) => (x.id || '').toString().toLowerCase() === statusId)) {
      // Fallback: if statusId not in project statuses (old foreign status), still consider ready and show status name as fallback
    }
    return true;
  });
  isScrumBoard = computed(() => {
    const boardId = this.task()?.boardId || '';
    const boards = (this.boardForTaskQuery.data() as any)?.project ? [] : []; // fallback
    // For now, check if task has sprint or if sprints exist - if no sprints, treat as Kanban
    const hasSprints = (this.sprintsForTaskQuery.data()?.length || 0) > 0;
    return hasSprints; // Scrum if project has sprints
  });

  // Subtasks/Comments derived from detail (single GET /tasks/:id/detail already returns subTasks/comments) — no extra Network on open
  subtasksList = computed(() => (this.taskDetailQuery.data() as any)?.subTasks ?? []);
  commentsList = computed(() => (this.taskDetailQuery.data() as any)?.comments ?? []);

  taskDetailQuery = injectQuery(() => ({
    queryKey: ['task-detail', this.task()?.id] as const,
    queryFn: () => firstValueFrom(this.projectService.getTaskDetail(this.task().id)),
    enabled: this.open() && !!this.task()?.id,
    staleTime: 0,
  }));

  historyQuery = injectQuery(() => ({
    queryKey: ['activities-task', this.projectId(), this.task()?.id] as const,
    queryFn: () =>
      firstValueFrom(this.projectService.getActivities(this.projectId(), 1, 20, this.task()?.id)),
    enabled:
      this.open() && !!this.projectId() && !!this.task()?.id && this.activeTab() === 'history',
  }));

  // Mutations - invalidate board + activities for realtime (no refresh needed)
  createSubtaskMut = injectMutation(() => ({
    mutationFn: (title: string) =>
      firstValueFrom(this.projectService.createSubTask(this.task().id, title)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['subtasks', this.task().id] });
      this.queryClient.invalidateQueries({ queryKey: ['board', this.projectId()] });
      this.queryClient.invalidateQueries({ queryKey: ['activities', this.projectId()] });
      this.queryClient.invalidateQueries({
        queryKey: ['activities-task', this.projectId(), this.task().id],
      });
      this.queryClient.invalidateQueries({queryKey:['task-detail', this.task().id]});
      this.newSubtask.set('');
    },
  }));
  toggleSubtaskMut = injectMutation(() => ({
    mutationFn: (id: string) => firstValueFrom(this.projectService.toggleSubTask(id)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['subtasks', this.task().id] });
      this.queryClient.invalidateQueries({ queryKey: ['board', this.projectId()] });
      this.queryClient.invalidateQueries({
        queryKey: ['activities-task', this.projectId(), this.task().id],
      });
    },
  }));
  updateSubtaskMut = injectMutation(() => ({
    mutationFn: (vars: { id: string; title: string }) =>
      firstValueFrom(this.projectService.updateSubTask(vars.id, vars.title)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['subtasks', this.task().id] });
      this.queryClient.invalidateQueries({
        queryKey: ['activities-task', this.projectId(), this.task().id],
      });
      this.editingSubtaskId.set(null);
    },
  }));
  deleteSubtaskMut = injectMutation(() => ({
    mutationFn: (id: string) => firstValueFrom(this.projectService.deleteSubTask(id)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['subtasks', this.task().id] });
      this.queryClient.invalidateQueries({ queryKey: ['board', this.projectId()] });
      this.queryClient.invalidateQueries({
        queryKey: ['activities-task', this.projectId(), this.task().id],
      });
    },
  }));
  createCommentMut = injectMutation(() => ({
    mutationFn: (content: string) =>
      firstValueFrom(this.projectService.createComment(this.task().id, content)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['comments', this.task().id] });
      this.queryClient.invalidateQueries({ queryKey: ['task-detail', this.task().id] });
      this.queryClient.invalidateQueries({ queryKey: ['board', this.projectId()] });
      this.queryClient.invalidateQueries({ queryKey: ['board', this.effectiveProjectId()] });
      this.queryClient.invalidateQueries({ queryKey: ['activities', this.projectId()] });
      this.newComment.set('');
      this.queryClient.invalidateQueries({
        queryKey: ['activities-task', this.projectId(), this.task().id],
      });
    },
  }));
  updateCommentMut = injectMutation(() => ({
    mutationFn: (vars: { id: string; content: string }) =>
      firstValueFrom(this.projectService.updateComment(vars.id, vars.content)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['comments', this.task().id] });
      this.queryClient.invalidateQueries({ queryKey: ['task-detail', this.task().id] });
      this.queryClient.invalidateQueries({
        queryKey: ['activities-task', this.projectId(), this.task().id],
      });
      this.editingCommentId.set(null);
    },
  }));
  deleteCommentMut = injectMutation(() => ({
    mutationFn: (id: string) => firstValueFrom(this.projectService.deleteComment(id)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['comments', this.task().id] });
      this.queryClient.invalidateQueries({ queryKey: ['task-detail', this.task().id] });
      this.queryClient.invalidateQueries({
        queryKey: ['activities-task', this.projectId(), this.task().id],
      });
    },
  }));

  currentUserId = computed(() => this.auth.currentUser()?.id || '');

  private populateForm(t: any) {
    this.title.set(t.title || '');
    this.description.set(t.description || '');
    this.priority.set(t.priority || 'Medium');
    this.listId.set(t.listId || '');
    this.statusId.set((t.statusId || '').toString().trim());
    this.assigneeId.set((t.assigneeId || '').toString().trim());
    this.teamId.set((t.teamId || '').toString().trim());
    this.sprintId.set((t.sprintId || '').toString().trim());
    this.dueDate.set(t.dueDate ? (t.dueDate as string).slice(0, 10) : '');
    this.issueType.set(t.issueType || 'Task');
    this.epic.set(t.epic || '');
    this.storyPoints.set(t.storyPoints ?? null);
    this.startDate.set(t.startDate ? (t.startDate as string).slice(0, 10) : '');
    this.environmentSel.set(t.environment || '');
    this.parentIssueId.set(t.parentIssueId || '');
    try {
      const a = JSON.parse(t.labelsJson || '[]');
      this.labels.set(Array.isArray(a) ? a.join(', ') : t.labelsJson || '');
    } catch {
      this.labels.set(t.labelsJson || '');
    }
    try {
      const w = JSON.parse(t.watchersJson || '[]');
      this.watchers.set(Array.isArray(w) ? w.join(', ') : '');
    } catch {
      this.watchers.set('');
    }
    try {
      const l = JSON.parse(t.linkedIssuesJson || '[]');
      this.linkedIssues.set(Array.isArray(l) ? l.join(', ') : '');
    } catch {
      this.linkedIssues.set('');
    }
    this.timeEstimated.set(t.timeEstimated ?? null);
    this.timeSpent.set(t.timeSpent ?? null);
    this.timeRemaining.set(t.timeRemaining ?? null);
    this.activeTab.set('comments');
  }

  constructor() {
    // Single populateForm — detail is authoritative, fallback to task input
    effect(
      () => {
        if (!this.open()) return;
        const detailTask = (this.taskDetailQuery.data() as any)?.task;
        const t = detailTask ?? this.task();
        if (t) this.populateForm(t);
      },
      { allowSignalWrites: true },
    );
    effect(
      () => {
        const board = this.boardForTaskQuery.data();
        if (board && (board as any).tasks) {
          this.boardTasks.set((board as any).tasks);
        }
      },
      { allowSignalWrites: true },
    );
  }

  openParentIssue() {
    const pid = this.parentIssueId();
    if (!pid) return;
    const parent = this.boardTasks().find((t: any) => t.id === pid);
    if (parent) {
      this.closed.emit();
      setTimeout(() => {
        const event = new CustomEvent('openTask', { detail: parent });
        window.dispatchEvent(event);
      }, 100);
    }
  }
  openChildIssue(child: any) {
    this.closed.emit();
    setTimeout(() => {
      const event = new CustomEvent('openTask', { detail: child });
      window.dispatchEvent(event);
    }, 100);
  }
  removeLinkedIssue(id: string) {
    try {
      const arr = JSON.parse(this.linkedIssues() ? this.linkedIssues() : '[]');
      const filtered = Array.isArray(arr)
        ? arr.filter((x: any) => (typeof x === 'string' ? x !== id : x.id !== id))
        : [];
      // Also handle comma-separated string case
      if (!Array.isArray(arr) || filtered.length === arr.length) {
        const parts = this.linkedIssues()
          .split(',')
          .map((s) => s.trim())
          .filter(Boolean);
        const newParts = parts.filter((p) => p !== id);
        this.linkedIssues.set(newParts.join(', '));
      } else {
        this.linkedIssues.set(JSON.stringify(filtered));
      }
    } catch {
      const parts = this.linkedIssues()
        .split(',')
        .map((s) => s.trim())
        .filter(Boolean);
      this.linkedIssues.set(parts.filter((p) => p !== id).join(', '));
    }
  }
  getLinkedDisplayIds(): string[] {
    try {
      const arr = JSON.parse(this.linkedIssues() || '[]');
      if (Array.isArray(arr)) return arr.map((x: any) => (typeof x === 'string' ? x : x.id || x));
    } catch {}
    return this.linkedIssues()
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean);
  }
  getLinkedTask(lid: string): any {
    return (
      this.boardTasks().find((x: any) => x.id === lid) || {
        title: lid.slice(0, 8),
        id: lid,
        priority: '',
        status: '',
      }
    );
  }
  save() {
    this.saved.emit({
      title: this.title().trim(),
      description: this.description().trim(),
      priority: this.priority(),
      listId: this.listId(),
      labelsJson: this.labelsJson(),
      assigneeId: this.assigneeId() || undefined,
      dueDate: this.dueDate() || undefined,
      issueType: this.issueType(),
      epic: this.epic().trim() || undefined,
      storyPoints: this.storyPoints() ?? undefined,
      startDate: this.startDate() || undefined,
      environment: this.environmentSel() || undefined,
      parentIssueId: this.parentIssueId() || undefined,
      sprintId: this.sprintId() || undefined,
      watchersJson: this.watchersJson(),
      linkedIssuesJson: this.linkedIssuesJson(),
      timeEstimated: this.timeEstimated() ?? undefined,
      timeSpent: this.timeSpent() ?? undefined,
      timeRemaining: this.timeRemaining() ?? undefined,
      teamId: this.teamId() || undefined,
      statusId: this.statusId() || undefined,
    });
  }
  addSubtask() {
    const v = this.newSubtask().trim();
    if (!v) return;
    this.createSubtaskMut.mutate(v);
  }
  addComment() {
    const v = this.newComment().trim();
    if (!v) return;
    this.createCommentMut.mutate(v);
  }
  startEditComment(c: any) {
    this.editingCommentId.set(c.id);
    this.editCommentContent.set(c.content);
  }
  saveEditComment() {
    const id = this.editingCommentId();
    const v = this.editCommentContent().trim();
    if (!id || !v) return;
    this.updateCommentMut.mutate({ id, content: v });
  }
  startEditSubtask(s: any) {
    this.editingSubtaskId.set(s.id);
    this.editSubtaskTitle.set(s.title);
  }
  saveEditSubtask() {
    const id = this.editingSubtaskId();
    const v = this.editSubtaskTitle().trim();
    if (!id || !v) return;
    this.updateSubtaskMut.mutate({ id, title: v });
  }
  confirmDeleteSubtask(id: string) {
    this.deleteSubtaskConfirmId.set(id);
  }
  doDeleteSubtask() {
    const id = this.deleteSubtaskConfirmId();
    if (!id) return;
    this.deleteSubtaskMut.mutate(id);
    this.deleteSubtaskConfirmId.set(null);
  }
  confirmDeleteComment(id: string) {
    this.deleteCommentConfirmId.set(id);
  }
  doDeleteComment() {
    const id = this.deleteCommentConfirmId();
    if (!id) return;
    this.deleteCommentMut.mutate(id);
    this.deleteCommentConfirmId.set(null);
  }
  getAuthorDisplay(authorId: string) {
    const members = this.projectMembersList();
    const m = members.find((x: any) => x.userId === authorId);
    if (m) return { name: m.fullName, avatar: m.avatarUrl, email: m.email };
    // Fallback to current user if author is self
    if (authorId === this.currentUserId()) {
      const u = this.auth.currentUser();
      return { name: u?.fullName || 'You', avatar: u?.avatarUrl, email: u?.email || '' };
    }
    return { name: authorId.slice(0, 8), avatar: undefined, email: '' };
  }
}
