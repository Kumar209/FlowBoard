import { Component, inject, ChangeDetectionStrategy, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { WorkspaceService } from '../../core/services/workspace.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { WorkspaceModalComponent } from '../../shared/components/modals/workspace-modal/workspace-modal.component';
import { ConfirmDeleteComponent } from '../../shared/components/modals/confirm-delete/confirm-delete.component';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

/**
 * WorkspacesComponent - MNC-grade: modals for Create/Update + Delete warning + hash gradient icon.
 * OrgAdmin only sees ⋮ Edit/Delete; others view-only.
 */
@Component({
  selector: 'app-workspaces',
  standalone: true,
  imports: [CommonModule, RouterLink, WorkspaceModalComponent, ConfirmDeleteComponent],
  templateUrl: './workspaces.component.html',
  styleUrls: ['./workspaces.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkspacesComponent {
  private workspaceService = inject(WorkspaceService);
  auth = inject(AuthService);
  private toast = inject(ToastService);
  private queryClient = inject(QueryClient);

  page = signal(1);
  pageSize = 12;
  search = signal('');

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces', this.page(), this.search()] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyWorkspacesPaginated(this.page(), this.pageSize, this.search() || undefined)),
  }));

  // For dropdown modals (create workspace needs orgs, not paginated workspaces)
  allWorkspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces-all'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyWorkspaces()),
  }));

  orgsQuery = injectQuery(() => ({
    queryKey: ['organizations'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyOrganizations()),
  }));

  canCreateWorkspace = computed(() => this.auth.canCreateWorkspace());
  total = computed(() => this.workspacesQuery.data()?.total || 0);
  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));

  // Modals signals
  createOpen = signal(false);
  editOpen = signal(false);
  deleteOpen = signal(false);
  editing = signal<any>(null);
  createError = signal<string | null>(null);

  gradientFor(slug: string) {
    const grads = ['from-violet-600 to-primary', 'from-cyan-500 to-blue-500', 'from-emerald-500 to-teal-500', 'from-amber-500 to-orange-500', 'from-pink-500 to-rose-500', 'from-indigo-500 to-purple-500'];
    let h = 0; for (let i=0;i<slug.length;i++) h = (h*31 + slug.charCodeAt(i)) >>>0;
    return grads[h % grads.length];
  }

  createMutation = injectMutation(() => ({
    mutationFn: (vars: { organizationId: string; name: string }) =>
      firstValueFrom(this.workspaceService.createWorkspace(vars.organizationId, vars.name)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['workspaces'] });
      this.queryClient.invalidateQueries({ queryKey: ['workspaces-all'] });
      this.queryClient.invalidateQueries({ queryKey: ['organizations'] });
      this.createOpen.set(false);
      this.createError.set(null);
      this.toast.success('Workspace created');
      // Refresh token + memberships so new workspace_id appears in JWT (Image 2 fix: avoid Forbidden on next project create)
      this.auth.refresh().subscribe({
        next: res => {
          this.auth.accessToken.set(res.accessToken);
          this.auth.me().subscribe({ next: m => this.auth.hydrateFromMe(m as any), error: () => {} });
        },
        error: () => {
          this.auth.me().subscribe({ next: m => this.auth.hydrateFromMe(m as any), error: () => {} });
        }
      });
    },
    onError: (err: any) => { const m = err.error?.error || 'Create failed'; this.createError.set(m); this.toast.error(m); },
  }));

  updateMutation = injectMutation(() => ({
    mutationFn: (vars: { id:string; name:string; slug:string }) =>
      firstValueFrom(this.workspaceService.updateWorkspace(vars.id, vars.name, vars.slug)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['workspaces'] });
      this.queryClient.invalidateQueries({ queryKey: ['workspaces-all'] });
      this.editOpen.set(false);
      this.toast.success('Workspace updated');
    },
    onError: (err: any) => this.toast.error(err.error?.error || 'Update failed'),
  }));

  deleteMutation = injectMutation(() => ({
    mutationFn: (id:string) => firstValueFrom(this.workspaceService.deleteWorkspace(id)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['workspaces'] });
      this.queryClient.invalidateQueries({ queryKey: ['workspaces-all'] });
      this.deleteOpen.set(false);
      this.toast.success('Workspace deleted');
    },
    onError: (err:any) => this.toast.error(err.error?.error || 'Delete failed'),
  }));

  openCreate() { this.createError.set(null); this.createOpen.set(true); }
  openEdit(ws:any) { this.editing.set(ws); this.editOpen.set(true); }
  openDelete(ws:any) { this.editing.set(ws); this.deleteOpen.set(true); }

  onCreateSubmit(e:{name:string; slug:string; organizationId:string}) { this.createMutation.mutate({ organizationId: e.organizationId, name: e.name }); }
  onEditSubmit(e:{name:string; slug:string}) { this.updateMutation.mutate({ id: this.editing().id, name: e.name, slug: e.slug }); }
  onDeleteConfirm() { this.deleteMutation.mutate(this.editing().id); }
}
