import { Component, inject, ChangeDetectionStrategy, computed, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { WorkspaceService } from '../../core/services/workspace.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { PermissionService } from '../../core/services/permission.service';
import { WorkspaceModalComponent } from '../../shared/components/modals/workspace-modal/workspace-modal.component';
import { ConfirmDeleteComponent } from '../../shared/components/modals/confirm-delete/confirm-delete.component';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { getRoleLabel } from '../../shared/constants/roles';
import { PermissionKeys } from '../../shared/constants/permissions';

@Component({
  selector: 'app-workspaces',
  standalone: true,
  imports: [CommonModule, RouterLink, WorkspaceModalComponent, ConfirmDeleteComponent],
  templateUrl: './workspaces.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkspacesComponent {
  private workspaceService = inject(WorkspaceService);
  auth = inject(AuthService);
  private toast = inject(ToastService);
  private queryClient = inject(QueryClient);
  private perm = inject(PermissionService);

  page = signal(1);
  pageSize = signal(10);
  search = signal('');
  private searchDebounce: any;

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces', this.page(), this.pageSize(), this.search()] as const,
    queryFn: () => firstValueFrom(
      this.workspaceService.getMyWorkspacesPaginated(
        this.page(),
        this.pageSize(),
        this.search() || undefined
      )
    )
  }));

  allWorkspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces-all'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyWorkspaces())
  }));

  orgsQuery = injectQuery(() => ({
    queryKey: ['organizations'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyOrganizations())
  }));

  PermissionKeys = PermissionKeys;
  canCreateWorkspace = computed(() => this.auth.canCreateWorkspace());
  hasCustomCreate = signal(false);
  canCreateWorkspaceEffective = computed(() => this.canCreateWorkspace() || this.hasCustomCreate());
  // 13.3 single helper per workspace — uses PermissionKeys, OrgAdmin bypass inside hasPermissionSync (5m dedup)
  canView = (wsId: string) => this.perm.hasPermissionSync(wsId, PermissionKeys.WorkspaceView);
  canUpdate = (wsId: string) => this.perm.hasPermissionSync(wsId, PermissionKeys.WorkspaceUpdate);
  canDeleteWs = (wsId: string) => this.perm.hasPermissionSync(wsId, PermissionKeys.WorkspaceDelete);

  total = computed(() => this.workspacesQuery.data()?.total || 0);

  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));

  showingFrom = computed(() => {
    if (this.total() === 0) return 0;
    return (this.page() - 1) * this.pageSize() + 1;
  });

  showingTo = computed(() => {
    if (this.total() === 0) return 0;
    return Math.min(this.page() * this.pageSize(), this.total());
  });

  createOpen = signal(false);
  editOpen = signal(false);
  deleteOpen = signal(false);
  editing = signal<any>(null);
  createError = signal<string | null>(null);

  constructor() {
    effect(async () => {
      const orgs: any = this.orgsQuery.data();
      if (!orgs || !Array.isArray(orgs) || orgs.length === 0) return;

      for (const org of orgs) {
        try {
          if (await this.perm.hasOrgPermission(org.id, PermissionKeys.WorkspaceCreate)) {
            this.hasCustomCreate.set(true);
            break;
          }
        } catch {}
      }
    }, { allowSignalWrites: true });
  }

  getRoleLabel(v: any) {
    return getRoleLabel(v);
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

      // Single shared session refresh - use deduped me, fallback to deduped refresh only if me 401
      this.auth.meDeduped().subscribe({
        next: m => this.auth.hydrateFromMe(m as any),
        error: () => {
          this.auth.refreshDeduped().subscribe({
            next: res => {
              this.auth.accessToken.set(res.accessToken);
              this.auth.meDeduped().subscribe({ next: mm => this.auth.hydrateFromMe(mm as any), error: () => {} });
            },
            error: () => {
              this.auth.meDeduped().subscribe({ next: mm => this.auth.hydrateFromMe(mm as any), error: () => {} });
            }
          });
        }
      });
    },
    onError: (err: any) => {
      const m = err.error?.error || 'Create failed';
      this.createError.set(m);
      this.toast.error(m);
    }
  }));

  updateMutation = injectMutation(() => ({
    mutationFn: (vars: { id: string; name: string; slug: string }) =>
      firstValueFrom(this.workspaceService.updateWorkspace(vars.id, vars.name, vars.slug)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['workspaces'] });
      this.queryClient.invalidateQueries({ queryKey: ['workspaces-all'] });
      this.editOpen.set(false);
      this.toast.success('Workspace updated');
    },
    onError: (err: any) => this.toast.error(err.error?.error || 'Update failed')
  }));

  deleteMutation = injectMutation(() => ({
    mutationFn: (id: string) =>
      firstValueFrom(this.workspaceService.deleteWorkspace(id)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['workspaces'] });
      this.queryClient.invalidateQueries({ queryKey: ['workspaces-all'] });
      this.deleteOpen.set(false);
      this.toast.success('Workspace deleted');
    },
    onError: (err: any) => this.toast.error(err.error?.error || 'Delete failed')
  }));

  onSearch(val: string) {
    clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => {
      this.search.set(val);
      this.page.set(1);
    }, 300);
  }

  onPageSizeChange(value: string) {
    this.pageSize.set(Number(value));
    this.page.set(1);
  }

  openCreate() {
    this.createError.set(null);
    this.createOpen.set(true);
  }

  openEdit(ws: any) {
    this.editing.set(ws);
    this.editOpen.set(true);
  }

  openDelete(ws: any) {
    this.editing.set(ws);
    this.deleteOpen.set(true);
  }

  onCreateSubmit(e: { name: string; slug: string; organizationId: string }) {
    this.createMutation.mutate({
      organizationId: e.organizationId,
      name: e.name
    });
  }

  onEditSubmit(e: { name: string; slug: string }) {
    this.updateMutation.mutate({
      id: this.editing().id,
      name: e.name,
      slug: e.slug
    });
  }

  onDeleteConfirm() {
    this.deleteMutation.mutate(this.editing().id);
  }
}