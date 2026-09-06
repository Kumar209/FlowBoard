import { Component, inject, signal, ChangeDetectionStrategy, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DragDropModule, CdkDragDrop, transferArrayItem, moveItemInArray } from '@angular/cdk/drag-drop';
import { TaskCardComponent } from '../../../shared/components/task-card/task-card.component';
import { TaskCreateModalComponent } from '../../../shared/components/modals/task-create-modal/task-create-modal.component';
import { TaskDetailModalComponent } from '../../../shared/components/modals/task-detail-modal/task-detail-modal.component';
import { ColumnModalComponent } from '../../../shared/components/modals/column-modal/column-modal.component';
import { ConfirmDeleteComponent } from '../../../shared/components/modals/confirm-delete/confirm-delete.component';
import { ProjectService } from '../../../core/services/project.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

/**
 * BoardComponent - Kanban with board-specific columns (Board → Columns), sprint project-owned, team per issue.
 */
@Component({
  selector: 'app-board',
  standalone: true,
  imports: [CommonModule, DragDropModule, TaskCardComponent, TaskCreateModalComponent, TaskDetailModalComponent, ColumnModalComponent, ConfirmDeleteComponent, RouterLink],
  templateUrl: './board.component.html',
  styleUrls: ['./board.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BoardComponent {
  private route = inject(ActivatedRoute);
  projectService = inject(ProjectService);
  auth = inject(AuthService);
  private toast = inject(ToastService);
  private queryClient = inject(QueryClient);

  projectId = signal<string>(this.route.snapshot.paramMap.get('pid') || this.route.parent?.snapshot.paramMap.get('pid') || '');
  workspaceId = signal<string>(this.route.snapshot.paramMap.get('wid') || this.route.parent?.snapshot.paramMap.get('wid') || '');

  canCreateTask = computed(() => this.auth.canCreateTask());
  canComment = computed(() => this.auth.canComment());
  roleLabel = computed(() => {
    const wid = this.workspaceId();
    const m = this.auth.memberships().find(x => x.workspaceId === wid);
    return m?.roleName ?? (m ? String(m.role) : '');
  });

  boardView = signal<string>(this.route.snapshot.queryParamMap.get('view') || this.route.parent?.snapshot.queryParamMap.get('view') || 'main');

  boardsQuery = injectQuery(() => ({
    queryKey: ['boards', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getBoards(this.projectId())),
    enabled: !!this.projectId(),
  }));

  selectedBoardId = computed(() => {
    const bv = this.boardView();
    const boards = this.boardsQuery.data() || [];
    const byId = boards.find((x:any)=> x.id===bv);
    if (byId) return byId.id;
    const byName = boards.find((x:any)=> x.name.toLowerCase()===bv.toLowerCase());
    if (byName) return byName.id;
    // fallback: first board if exists
    return boards[0]?.id || null;
  });

  currentBoardType = computed(() => {
    const bv = this.boardView();
    const boards = this.boardsQuery.data() || [];
    const b = boards.find((x:any)=> x.id===bv || x.name.toLowerCase().includes(bv));
    if (b) return b.type;
    if (bv==='engineering') return 'Scrum';
    if (bv==='qa' || bv==='support') return 'Kanban';
    return 'Kanban';
  });
  isScrum = computed(() => this.currentBoardType()==='Scrum');

  boardQuery = injectQuery(() => ({
    queryKey: ['board', this.projectId(), this.selectedBoardId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getBoard(this.projectId(), this.selectedBoardId() || undefined)),
    enabled: !!this.projectId(),
  }));

  newTitle = signal('');
  newListName = signal('');
  selectedListId = signal<string>('');
  openMenuListId = signal<string | null>(null);
  columnModalOpen = signal(false);
  columnModalMode = signal<'create'|'update'>('create');
  editingColumn = signal<any>(null);
  deleteColumnTarget = signal<any>(null);

  taskSearch = signal('');
  priorityFilter = signal('');
  labelFilter = signal('');

  sprintsQuery = injectQuery(() => ({
    queryKey: ['sprints', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getSprints(this.projectId())),
    enabled: !!this.projectId(),
  }));
  sprints = computed(() => {
    const api = this.sprintsQuery.data();
    if (api && api.length>0) return api.map((s:any)=> ({ id:s.id, name:s.name, start:(s.startDate||'').slice(0,10), end:(s.endDate||'').slice(0,10) }));
    return [];
  });
  selectedSprint = signal<string>(''); // will be set to first sprint id when loaded, '' = none (should not happen after fix, but fallback)
  currentBoardFilterTeamIds = computed(() => {
    const boardId = this.selectedBoardId();
    const boards = this.boardsQuery.data() || [];
    const board = boards.find((b:any) => b.id === boardId);
    if (!board?.filterJson) return [];
    try {
      const f = JSON.parse(board.filterJson);
      return f?.teamIds || [];
    } catch { return []; }
  });
  presetTeamId = computed(() => this.currentBoardFilterTeamIds()[0] || '');
  presetSprintId = computed(() => {
    if (!this.isScrum()) return ''; // Kanban always Backlog
    const sel = this.selectedSprint();
    if (!sel || sel === 'all' || sel === 'Backlog') return '';
    return sel; // now sel is sprint id
  });

  constructor() {
    queueMicrotask(() => {
      this.route.queryParamMap.subscribe(m => {
        const v = m.get('view') || this.route.parent?.snapshot.queryParamMap.get('view') || 'main';
        this.boardView.set(v);
        const s = m.get('sprint');
        if (s) this.selectedSprint.set(s);
      });
      this.route.parent?.queryParamMap?.subscribe(m => {
        const v = m.get('view') || this.route.snapshot.queryParamMap.get('view') || 'main';
        if (v) this.boardView.set(v);
      });
    });
    // Default to first sprint when sprints load and no sprint selected (remove All/Backlog per user request)
    effect(() => {
      const sprints = this.sprints();
      if (sprints.length > 0 && !this.selectedSprint()) {
        this.selectedSprint.set(sprints[0].id);
      }
    });
  }

  // Modals
  createTaskOpen = signal(false);
  createTaskListId = signal<string>('');
  createTaskListName = signal<string>('');
  detailOpen = signal(false);
  selectedTask = signal<any>(null);

  createListMutation = injectMutation(() => ({
    mutationFn: (vars: {name: string; position?: number}) => firstValueFrom(this.projectService.createList(this.projectId(), vars.name, this.selectedBoardId() || undefined, vars.position)),
    onSuccess: () => { this.queryClient.invalidateQueries({ queryKey: ['board'] }); this.columnModalOpen.set(false); this.newListName.set(''); this.openMenuListId.set(null); this.toast.success('Column created'); },
    onError: (e:any) => { this.toast.error(e?.error?.error || e?.message || 'Create column failed'); },
  }));
  renameListMutation = injectMutation(() => ({
    mutationFn: (vars: { listId: string; name: string; position?: number }) => firstValueFrom(this.projectService.renameList(this.projectId(), vars.listId, vars.name, vars.position)),
    onSuccess: () => { this.queryClient.invalidateQueries({ queryKey: ['board'] }); this.columnModalOpen.set(false); this.editingColumn.set(null); this.openMenuListId.set(null); this.toast.success('Column updated'); },
    onError: (e:any) => { this.toast.error(e?.error?.error || e?.message || 'Update failed'); },
  }));
  deleteListMutation = injectMutation(() => ({
    mutationFn: (listId: string) => firstValueFrom(this.projectService.deleteList(this.projectId(), listId)),
    onSuccess: () => { this.queryClient.invalidateQueries({ queryKey: ['board'] }); this.deleteColumnTarget.set(null); this.openMenuListId.set(null); this.toast.success('Column deleted'); },
    onError: (e:any) => { this.toast.error(e?.error?.error || e?.message || 'Cannot delete column with tasks'); },
  }));

  createMutation = injectMutation(() => ({
    mutationFn: (vars: { listId: string; title: string; description?: string; priority?: string; labelsJson?: string; assigneeId?: string; dueDate?: string; sprintId?: string; teamId?: string; issueType?: string }) =>
      firstValueFrom(this.projectService.createTask(this.projectId(), vars.listId, vars.title, vars.description, vars.priority, vars.labelsJson, vars.assigneeId, vars.dueDate, vars.issueType || 'Task', undefined, undefined, undefined, undefined, undefined, vars.sprintId, vars.teamId)),
    onMutate: async (vars) => {
      await this.queryClient.cancelQueries({ queryKey: ['board', this.projectId(), this.selectedBoardId()] });
      const prev = this.queryClient.getQueryData(['board', this.projectId(), this.selectedBoardId()]) as any;
      this.queryClient.setQueryData(['board', this.projectId(), this.selectedBoardId()], (old: any) => {
        if (!old) return old;
        const temp = { id: 'temp-' + Date.now(), projectId: this.projectId(), listId: vars.listId, title: vars.title, priority: vars.priority || 'Medium', position: old.tasks.length, createdAt: new Date().toISOString(), sprintId: vars.sprintId || null };
        return { ...old, tasks: [...old.tasks, temp] };
      });
      return { prev };
    },
    onError: (_err, _vars, ctx: any) => {
      if (ctx?.prev) this.queryClient.setQueryData(['board', this.projectId(), this.selectedBoardId()], ctx.prev);
    },
    onSettled: () => this.queryClient.invalidateQueries({ queryKey: ['board'] }),
  }));

  updateMutation = injectMutation(() => ({
    mutationFn: (vars: { id:string; title:string; description:string; priority:string; listId:string; labelsJson?:string; assigneeId?:string; dueDate?:string; issueType?:string; epic?:string; storyPoints?:number; startDate?:string; environment?:string; parentIssueId?:string; sprintId?:string; watchersJson?:string; linkedIssuesJson?:string; timeEstimated?:number; timeSpent?:number; timeRemaining?:number; teamId?:string }) =>
      firstValueFrom(this.projectService.updateTask(vars.id, vars.title, vars.description, vars.priority, vars.listId, vars.labelsJson, vars.assigneeId, vars.dueDate, vars.issueType, vars.epic, vars.storyPoints, vars.startDate, vars.environment, vars.parentIssueId, vars.sprintId, vars.watchersJson, vars.linkedIssuesJson, vars.timeEstimated, vars.timeSpent, vars.timeRemaining, vars.teamId)),
    onSuccess: (_data, vars:any) => { this.queryClient.invalidateQueries({ queryKey: ['board'] }); this.queryClient.invalidateQueries({ queryKey: ['task-detail', vars.id] }); this.queryClient.invalidateQueries({ queryKey: ['activities'] }); this.detailOpen.set(false); this.toast.success('Issue updated'); },
    onError: (e:any) => this.toast.error(e.error?.error || 'Update failed'),
  }));

  moveMutation = injectMutation(() => ({
    mutationFn: (vars: { taskId:string; toListId:string; newPosition:number }) =>
      firstValueFrom(this.projectService.moveTask(vars.taskId, vars.toListId, vars.newPosition)),
    onSuccess: () => this.queryClient.invalidateQueries({ queryKey: ['board'] }),
  }));

  boardSettingsOpen = signal(false);
  openBoardSettings(){ this.boardSettingsOpen.set(true); }

  openCreateTask(listId:string, listName:string) { this.createTaskListId.set(listId); this.createTaskListName.set(listName); this.createTaskOpen.set(true); }
  openCreateIssue(){
    const first = this.boardQuery.data()?.lists?.[0];
    if(first) this.openCreateTask(first.id, first.name);
    else if(this.boardQuery.data()?.lists?.length) this.openCreateTask(this.boardQuery.data()!.lists[0].id, this.boardQuery.data()!.lists[0].name);
    else this.toast.error('Create a column first');
  }
  onCreateTaskSubmit(e:{title:string; description:string; priority:string; labels:string; dueDate:string; teamId?:string; sprintId?:string; issueType:string}) {
    const labelsJson = e.labels ? JSON.stringify(e.labels.split(',').map(s=>s.trim()).filter(Boolean)) : undefined;
    // Respect user's explicit sprint choice: No Sprint (undefined) → backlog, even when board filtered to Sprint 1
    let sprintId: string | undefined = e.sprintId || undefined;
    let teamId: string | undefined = e.teamId || undefined;
    // If board has team filter and user didn't pick a team, default to board's filtered team so issue appears on this board
    const filteredTeams = this.currentBoardFilterTeamIds();
    if (!teamId && filteredTeams.length > 0) {
      teamId = filteredTeams[0];
    }
    // If board is filtered to a specific sprint (selectedSprint is a sprint id/name) and user picked No Sprint, keep as backlog (do not auto-assign)
    // Only auto-assign sprint if user didn't have a picker? But modal always has picker, so respect explicit choice.
    this.createMutation.mutate({ listId: this.createTaskListId(), title: e.title, description: e.description, priority: e.priority, labelsJson, dueDate: e.dueDate || undefined, sprintId, teamId } as any);
    this.createTaskOpen.set(false);
  }

  openDetail(task:any) { this.selectedTask.set(task); this.detailOpen.set(true); }
  onDetailSave(e:{title:string; description:string; priority:string; listId:string; labelsJson?:string; assigneeId?:string; dueDate?:string; issueType?:string; epic?:string; storyPoints?:number; startDate?:string; environment?:string; parentIssueId?:string; sprintId?:string; watchersJson?:string; linkedIssuesJson?:string; timeEstimated?:number; timeSpent?:number; timeRemaining?:number; teamId?:string}) {
    const t = this.selectedTask();
    if(!t) return;
    if(e.listId !== t.listId){
      this.moveMutation.mutate({ taskId: t.id, toListId: e.listId, newPosition: 0 });
    }
    this.updateMutation.mutate({ id: t.id, title: e.title, description: e.description, priority: e.priority, listId: e.listId, labelsJson: e.labelsJson, assigneeId: e.assigneeId, dueDate: e.dueDate, issueType: e.issueType, epic: e.epic, storyPoints: e.storyPoints, startDate: e.startDate, environment: e.environment, parentIssueId: e.parentIssueId, sprintId: e.sprintId, watchersJson: e.watchersJson, linkedIssuesJson: e.linkedIssuesJson, timeEstimated: e.timeEstimated, timeSpent: e.timeSpent, timeRemaining: e.timeRemaining, teamId: e.teamId });
  }
  openCreateColumn(){ this.columnModalMode.set('create'); this.editingColumn.set(null); this.columnModalOpen.set(true); }
  openEditColumn(list:any){ this.columnModalMode.set('update'); this.editingColumn.set(list); this.columnModalOpen.set(true); this.openMenuListId.set(null); }
  onColumnSubmit(e:{name:string; position:number}){
    if(this.columnModalMode()==='create') this.createListMutation.mutate({name: e.name, position: e.position});
    else if(this.editingColumn()) this.renameListMutation.mutate({listId: this.editingColumn().id, name: e.name, position: e.position});
  }
  confirmDeleteColumn(list:any){ this.deleteColumnTarget.set(list); this.openMenuListId.set(null); }
  onDeleteColumnConfirm(){ const t=this.deleteColumnTarget(); if(t) this.deleteListMutation.mutate(t.id); }
  toggleListMenu(listId:string, e:MouseEvent) { e.stopPropagation(); this.openMenuListId.set(this.openMenuListId()===listId ? null : listId); }
  closeMenus() { this.openMenuListId.set(null); }
  prevSprint(){
    const list=this.sprints();
    const idx=list.findIndex(s=>s.id===this.selectedSprint());
    if(idx>0) this.selectedSprint.set(list[idx-1].id);
  }
  nextSprint(){
    const list=this.sprints();
    const idx=list.findIndex(s=>s.id===this.selectedSprint());
    if(idx>=0 && idx < list.length-1) this.selectedSprint.set(list[idx+1].id);
  }

  drop(event: CdkDragDrop<any[]>, listId: string) {
    const prevListId = event.previousContainer.id.replace('list-','');
    const task = event.item.data;
    if(event.previousContainer === event.container){
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      transferArrayItem(event.previousContainer.data, event.container.data, event.previousIndex, event.currentIndex);
      this.moveMutation.mutate({ taskId: task.id, toListId: listId, newPosition: event.currentIndex });
    }
  }
  dropColumn(event: CdkDragDrop<any[]>) {
    const lists = this.boardQuery.data()?.lists;
    if (!lists) return;
    moveItemInArray(lists, event.previousIndex, event.currentIndex);
    this.queryClient.invalidateQueries({ queryKey: ['board'] });
  }

  tasksForList(listId: string) {
    let tasks = (this.boardQuery.data()?.tasks || []).filter(t => t.listId === listId);
    const sel = this.selectedSprint();
    // Sprint filter: project-owned sprints - filter by SprintId
    if (sel && sel !== 'all') {
      if (sel === 'Backlog') tasks = tasks.filter((t:any)=> !t.sprintId);
      else {
        const sprint = this.sprints().find(s=> s.name===sel || s.id===sel);
        const sid = sprint?.id || sel;
        tasks = tasks.filter((t:any)=> t.sprintId === sid);
      }
    }
    const s = this.taskSearch().toLowerCase();
    const p = this.priorityFilter();
    const l = this.labelFilter().toLowerCase();
    if (s) tasks = tasks.filter(t => t.title.toLowerCase().includes(s) || (t.description||'').toLowerCase().includes(s));
    if (p) tasks = tasks.filter(t => t.priority === p);
    if (l) tasks = tasks.filter(t => (t.labelsJson||'').toLowerCase().includes(l));
    return tasks.sort((a,b) => a.position - b.position);
  }

  filteredTasksCount = computed(() => {
    let tasks = this.boardQuery.data()?.tasks || [];
    const sel = this.selectedSprint();
    if (sel && sel !== 'all') {
      if (sel === 'Backlog') tasks = tasks.filter((t:any)=> !t.sprintId);
      else {
        const sprint = this.sprints().find(s=> s.name===sel || s.id===sel);
        const sid = sprint?.id || sel;
        tasks = tasks.filter((t:any)=> t.sprintId === sid);
      }
    }
    const s = this.taskSearch().toLowerCase();
    const p = this.priorityFilter();
    const l = this.labelFilter().toLowerCase();
    if (s) tasks = tasks.filter(t => t.title.toLowerCase().includes(s) || (t.description||'').toLowerCase().includes(s));
    if (p) tasks = tasks.filter(t => t.priority === p);
    if (l) tasks = tasks.filter(t => (t.labelsJson||'').toLowerCase().includes(l));
    return tasks.length;
  });
}
