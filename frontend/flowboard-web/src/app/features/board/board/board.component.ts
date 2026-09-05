import { Component, inject, signal, ChangeDetectionStrategy, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TaskCardComponent } from '../../../shared/components/task-card/task-card.component';
import { ProjectService } from '../../../core/services/project.service';
import { AuthService } from '../../../core/services/auth.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

/**
 * BoardComponent - MNC-grade: OnPush + computed canCreateTask (Client/Viewer hide) + optimistic TanStack.
 * Client 403 / Viewer 403 for create/move is now hidden in UI, not just API.
 */
@Component({
  selector: 'app-board',
  standalone: true,
  imports: [CommonModule, TaskCardComponent, RouterLink],
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

  createListMutation = injectMutation(() => ({
    mutationFn: (name: string) => firstValueFrom(this.projectService.createList(this.projectId(), name)),
    onSuccess: () => { this.queryClient.invalidateQueries({ queryKey: ['board', this.projectId()] }); this.showCreateList.set(false); this.newListName.set(''); },
  }));

  createMutation = injectMutation(() => ({
    mutationFn: (vars: { listId: string; title: string }) =>
      firstValueFrom(this.projectService.createTask(this.projectId(), vars.listId, vars.title)),
    onMutate: async (vars) => {
      await this.queryClient.cancelQueries({ queryKey: ['board', this.projectId()] });
      const prev = this.queryClient.getQueryData(['board', this.projectId()]) as any;
      this.queryClient.setQueryData(['board', this.projectId()], (old: any) => {
        if (!old) return old;
        const temp = { id: 'temp-' + Date.now(), projectId: this.projectId(), listId: vars.listId, title: vars.title, priority: 'Medium', position: old.tasks.length, createdAt: new Date().toISOString() };
        return { ...old, tasks: [...old.tasks, temp] };
      });
      return { prev };
    },
    onError: (_err, _vars, ctx: any) => {
      if (ctx?.prev) this.queryClient.setQueryData(['board', this.projectId()], ctx.prev);
    },
    onSettled: () => this.queryClient.invalidateQueries({ queryKey: ['board', this.projectId()] }),
  }));

  createTask(listId: string) {
    const title = this.newTitle().trim();
    if (!title) return;
    this.selectedListId.set(listId);
    this.createMutation.mutate({ listId, title });
    this.newTitle.set('');
  }

  tasksForList(listId: string) {
    return (this.boardQuery.data()?.tasks || []).filter(t => t.listId === listId).sort((a,b) => a.position - b.position);
  }
}
