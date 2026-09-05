import { Component, inject, signal, ChangeDetectionStrategy, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../core/services/project.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { ProjectModalComponent } from '../../shared/components/modals/project-modal/project-modal.component';
import { ConfirmDeleteComponent } from '../../shared/components/modals/confirm-delete/confirm-delete.component';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

/**
 * WorkspaceComponent - MNC-grade: modals for Create/Update/Delete project + toast + dropdown.
 */
@Component({
  selector: 'app-workspace',
  standalone: true,
  imports: [CommonModule, RouterLink, ProjectModalComponent, ConfirmDeleteComponent],
  templateUrl: './workspace.component.html',
  styleUrls: ['./workspace.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkspaceComponent {
  private route = inject(ActivatedRoute);
  projectService = inject(ProjectService);
  auth = inject(AuthService);
  private toast = inject(ToastService);
  private workspaceService = inject(WorkspaceService);
  private queryClient = inject(QueryClient);

  workspaceId = signal<string>(this.route.snapshot.paramMap.get('wid') || '11111111-1111-1111-1111-111111111111');
  createOpen = signal(false);
  editOpen = signal(false);
  deleteOpen = signal(false);
  editing = signal<any>(null);
  createError = signal<string | null>(null);

  // Fetch workspace name for title (Image 3 fix: show Marketing not generic Workspace)
  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyWorkspaces()),
  }));
  workspaceName = computed(() => {
    const ws = this.workspacesQuery.data()?.find(w => w.id === this.workspaceId());
    return ws?.name || 'Workspace';
  });

  canCreateProject = computed(() => {
    const wid = this.workspaceId();
    return this.auth.isSuperAdmin() || this.auth.isOrgAdmin() || this.auth.isManagerFor(wid) || this.auth.isOrgAdminFor(wid) || this.auth.canCreateProject();
  });
  roleLabel = computed(() => {
    const wid = this.workspaceId();
    const m = this.auth.memberships().find(x => x.workspaceId === wid);
    const raw = m?.roleName ?? m?.role;
    const map: Record<string,string> = { '0':'Member','1':'ProjectManager','2':'OrgAdmin','3':'Client','4':'Viewer','5':'SuperAdmin' };
    if (raw !== undefined) return map[String(raw)] ?? String(raw);
    // fallback to workspace role from workspacesQuery (covers direct ws.role number)
    const ws = this.workspacesQuery.data()?.find(w => w.id === wid);
    if (ws?.role !== undefined) return map[String(ws.role)] ?? String(ws.role);
    return 'Member';
  });

  projectsQuery = injectQuery(() => ({
    queryKey: ['projects', this.workspaceId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getProjects(this.workspaceId())),
  }));

  createMutation = injectMutation(() => ({
    mutationFn: (vars: { name: string }) => firstValueFrom(this.projectService.createProject(this.workspaceId(), vars.name)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['projects', this.workspaceId()] });
      this.queryClient.invalidateQueries({ queryKey: ['projects-global'] });
      this.createOpen.set(false);
      this.createError.set(null);
      this.toast.success('Project created');
    },
    onError: (err: any) => { const m = err.error?.error || 'Create failed'; this.createError.set(m); this.toast.error(m); },
  }));
  updateMutation = injectMutation(() => ({
    mutationFn: (vars:{id:string; name:string}) => firstValueFrom(this.projectService.updateProject(vars.id, vars.name)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['projects', this.workspaceId()] });
      this.queryClient.invalidateQueries({ queryKey: ['projects-global'] });
      this.editOpen.set(false); this.toast.success('Project updated');
    },
    onError: (err:any)=> this.toast.error(err.error?.error||'Update failed'),
  }));
  deleteMutation = injectMutation(() => ({
    mutationFn: (id:string)=> firstValueFrom(this.projectService.deleteProject(id)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['projects', this.workspaceId()] });
      this.queryClient.invalidateQueries({ queryKey: ['projects-global'] });
      this.deleteOpen.set(false); this.toast.success('Project deleted');
    },
    onError: (err:any)=> this.toast.error(err.error?.error||'Delete failed'),
  }));

  openCreate() { this.createError.set(null); this.createOpen.set(true); }
  openEdit(p:any){ this.editing.set(p); this.editOpen.set(true); }
  openDelete(p:any){ this.editing.set(p); this.deleteOpen.set(true); }
  onCreateSubmit(e:{name:string}){ this.createMutation.mutate({name:e.name}); }
  onEditSubmit(e:{name:string}){ this.updateMutation.mutate({id:this.editing().id, name:e.name}); }
  onDeleteConfirm(){ this.deleteMutation.mutate(this.editing().id); }
}
