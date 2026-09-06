import { Component, inject, signal, ChangeDetectionStrategy, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DragDropModule, CdkDragDrop, transferArrayItem, moveItemInArray } from '@angular/cdk/drag-drop';
import { TaskCardComponent } from '../../../shared/components/task-card/task-card.component';
import { TaskCreateModalComponent } from '../../../shared/components/modals/task-create-modal/task-create-modal.component';
import { TaskDetailModalComponent } from '../../../shared/components/modals/task-detail-modal/task-detail-modal.component';
import { ProjectService } from '../../../core/services/project.service';
import { AuthService } from '../../../core/services/auth.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

/**
 * BoardComponent - MNC-grade: 4-column (To Do, In Progress, In Review, Done) + DragDrop + Task modals + OnPush.
 */
@Component({
  selector: 'app-board',
  standalone: true,
  imports: [CommonModule, DragDropModule, TaskCardComponent, TaskCreateModalComponent, TaskDetailModalComponent, RouterLink],
  templateUrl: './board.component.html',
  styleUrls: ['./board.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BoardComponent {
  private route = inject(ActivatedRoute);
  projectService = inject(ProjectService);
  auth = inject(AuthService);
  private queryClient = inject(QueryClient);

  projectId = signal<string>(this.route.snapshot.paramMap.get('pid') || '');
  workspaceId = signal<string>(this.route.snapshot.paramMap.get('wid') || '');

  canCreateTask = computed(() => this.auth.canCreateTask());
  canComment = computed(() => this.auth.canComment());
  roleLabel = computed(() => {
    const wid = this.workspaceId();
    const m = this.auth.memberships().find(x => x.workspaceId === wid);
    return m?.roleName ?? (m ? String(m.role) : '');
  });

  boardQuery = injectQuery(() => ({
    queryKey: ['board', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getBoard(this.projectId())),
    enabled: !!this.projectId(),
  }));

  newTitle = signal('');
  newListName = signal('');
  selectedListId = signal<string>('');
  showCreateList = signal(false);

  // Filters for Task 2.5 (search FULLTEXT, priority, label)
  taskSearch = signal('');
  priorityFilter = signal('');
  labelFilter = signal('');

  // Modals
  createTaskOpen = signal(false);
  createTaskListId = signal<string>('');
  createTaskListName = signal<string>('');
  detailOpen = signal(false);
  selectedTask = signal<any>(null);

  createListMutation = injectMutation(() => ({
    mutationFn: (name: string) => firstValueFrom(this.projectService.createList(this.projectId(), name)),
    onSuccess: () => { this.queryClient.invalidateQueries({ queryKey: ['board', this.projectId()] }); this.showCreateList.set(false); this.newListName.set(''); },
  }));

  createMutation = injectMutation(() => ({
    mutationFn: (vars: { listId: string; title: string; description?: string; priority?: string }) =>
      firstValueFrom(this.projectService.createTask(this.projectId(), vars.listId, vars.title, vars.description, vars.priority)),
    onMutate: async (vars) => {
      await this.queryClient.cancelQueries({ queryKey: ['board', this.projectId()] });
      const prev = this.queryClient.getQueryData(['board', this.projectId()]) as any;
      this.queryClient.setQueryData(['board', this.projectId()], (old: any) => {
        if (!old) return old;
        const temp = { id: 'temp-' + Date.now(), projectId: this.projectId(), listId: vars.listId, title: vars.title, priority: vars.priority || 'Medium', position: old.tasks.length, createdAt: new Date().toISOString() };
        return { ...old, tasks: [...old.tasks, temp] };
      });
      return { prev };
    },
    onError: (_err, _vars, ctx: any) => {
      if (ctx?.prev) this.queryClient.setQueryData(['board', this.projectId()], ctx.prev);
    },
    onSettled: () => this.queryClient.invalidateQueries({ queryKey: ['board', this.projectId()] }),
  }));

  updateMutation = injectMutation(() => ({
    mutationFn: (vars: { id:string; title:string; description:string; priority:string; listId:string }) =>
      firstValueFrom(this.projectService.updateTask(vars.id, vars.title, vars.description, vars.priority, vars.listId)),
    onSuccess: () => { this.queryClient.invalidateQueries({ queryKey: ['board', this.projectId()] }); this.detailOpen.set(false); },
  }));

  moveMutation = injectMutation(() => ({
    mutationFn: (vars: { taskId:string; toListId:string; newPosition:number }) =>
      firstValueFrom(this.projectService.moveTask(vars.taskId, vars.toListId, vars.newPosition)),
    onSuccess: () => this.queryClient.invalidateQueries({ queryKey: ['board', this.projectId()] }),
  }));

  openCreateTask(listId:string, listName:string) { this.createTaskListId.set(listId); this.createTaskListName.set(listName); this.createTaskOpen.set(true); }
  onCreateTaskSubmit(e:{title:string; description:string; priority:string}) {
    this.createMutation.mutate({ listId: this.createTaskListId(), title: e.title, description: e.description, priority: e.priority });
    this.createTaskOpen.set(false);
  }

  openDetail(task:any) { this.selectedTask.set(task); this.detailOpen.set(true); }
  onDetailSave(e:{title:string; description:string; priority:string; listId:string}) {
    const t = this.selectedTask();
    if(!t) return;
    // If list changed, move first
    if(e.listId !== t.listId){
      this.moveMutation.mutate({ taskId: t.id, toListId: e.listId, newPosition: 0 });
    }
    this.updateMutation.mutate({ id: t.id, title: e.title, description: e.description, priority: e.priority, listId: e.listId });
  }

  // Drag-drop handler
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

  tasksForList(listId: string) {
    let tasks = (this.boardQuery.data()?.tasks || []).filter(t => t.listId === listId);
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
    const s = this.taskSearch().toLowerCase();
    const p = this.priorityFilter();
    const l = this.labelFilter().toLowerCase();
    if (s) tasks = tasks.filter(t => t.title.toLowerCase().includes(s) || (t.description||'').toLowerCase().includes(s));
    if (p) tasks = tasks.filter(t => t.priority === p);
    if (l) tasks = tasks.filter(t => (t.labelsJson||'').toLowerCase().includes(l));
    return tasks.length;
  });
}
