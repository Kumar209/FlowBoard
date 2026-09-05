import { Component, inject, signal, ChangeDetectionStrategy, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../core/services/project.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { ProjectModalComponent } from '../../shared/components/modals/project-modal/project-modal.component';
import { ConfirmDeleteComponent } from '../../shared/components/modals/confirm-delete/confirm-delete.component';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

/**
 * ProjectsComponent - MNC-grade: modals for Create/Update/Delete + toast + dropdown.
 */
@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, RouterLink, ProjectModalComponent, ConfirmDeleteComponent],
  templateUrl: './projects.component.html',
  styleUrls: ['./projects.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectsComponent {
  private projectService = inject(ProjectService);
  private workspaceService = inject(WorkspaceService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  private queryClient = inject(QueryClient);

  selectedWorkspaceId = signal<string>('all');
  canCreateProject = computed(() => this.auth.canCreateProject());

  createOpen = signal(false);
  editOpen = signal(false);
  deleteOpen = signal(false);
  editing = signal<any>(null);
  createError = signal<string | null>(null);

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyWorkspaces()),
  }));

  projectsQuery = injectQuery(() => ({
    queryKey: ['projects-global', this.selectedWorkspaceId()] as const,
    queryFn: async () => {
      const workspaces = await firstValueFrom(this.workspaceService.getMyWorkspaces());
      const ids = this.selectedWorkspaceId() === 'all' ? workspaces.map(w => w.id) : [this.selectedWorkspaceId()];
      const results = await Promise.all(ids.map(id => firstValueFrom(this.projectService.getProjects(id)).catch(() => ({ items: [], total: 0 } as any))));
      const all = results.flatMap(r => r.items);
      return { items: all, total: all.length };
    },
  }));

  filtered = computed(() => this.projectsQuery.data()?.items || []);

  createMutation = injectMutation(() => ({
    mutationFn: (vars: { workspaceId: string; name: string }) =>
      firstValueFrom(this.projectService.createProject(vars.workspaceId, vars.name)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['projects-global'] });
      this.queryClient.invalidateQueries({ queryKey: ['projects'] });
      this.createOpen.set(false);
      this.createError.set(null);
      this.toast.success('Project created');
    },
    onError: (err: any) => { const m= err.error?.error || 'Create failed'; this.createError.set(m); this.toast.error(m); },
  }));
  updateMutation = injectMutation(() => ({
    mutationFn: (vars:{id:string; name:string}) => firstValueFrom(this.projectService.updateProject(vars.id, vars.name)),
    onSuccess: () => { this.queryClient.invalidateQueries({ queryKey: ['projects-global'] }); this.queryClient.invalidateQueries({ queryKey: ['projects'] }); this.editOpen.set(false); this.toast.success('Project updated'); },
    onError: (err:any)=> this.toast.error(err.error?.error||'Update failed'),
  }));
  deleteMutation = injectMutation(() => ({
    mutationFn: (id:string)=> firstValueFrom(this.projectService.deleteProject(id)),
    onSuccess: () => { this.queryClient.invalidateQueries({ queryKey: ['projects-global'] }); this.queryClient.invalidateQueries({ queryKey: ['projects'] }); this.deleteOpen.set(false); this.toast.success('Project deleted'); },
    onError: (err:any)=> this.toast.error(err.error?.error||'Delete failed'),
  }));

  openCreate(){ this.createError.set(null); this.createOpen.set(true); }
  openEdit(p:any){ this.editing.set(p); this.editOpen.set(true); }
  openDelete(p:any){ this.editing.set(p); this.deleteOpen.set(true); }
  onCreateSubmit(e:{name:string; workspaceId:string}){ this.createMutation.mutate({workspaceId:e.workspaceId, name:e.name}); }
  onEditSubmit(e:{name:string}){ this.updateMutation.mutate({id:this.editing().id, name:e.name}); }
  onDeleteConfirm(){ this.deleteMutation.mutate(this.editing().id); }
}
