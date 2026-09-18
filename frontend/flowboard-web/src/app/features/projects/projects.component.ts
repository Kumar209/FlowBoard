import { Component, inject, signal, ChangeDetectionStrategy, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../core/services/project.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { PermissionService } from '../../core/services/permission.service';
import { ProjectModalComponent } from '../../shared/components/modals/project-modal/project-modal.component';
import { ConfirmDeleteComponent } from '../../shared/components/modals/confirm-delete/confirm-delete.component';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { PermissionKeys } from '../../shared/constants/permissions';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, RouterLink, ProjectModalComponent, ConfirmDeleteComponent],
  templateUrl: './projects.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectsComponent {
  private projectService = inject(ProjectService);
  private workspaceService = inject(WorkspaceService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  private queryClient = inject(QueryClient);
  private perm = inject(PermissionService);

  selectedWorkspaceId = signal<string>('all');
  wsSearch = signal<string>('');
  wsDropdownOpen = signal<boolean>(false);
  projectSearch = signal<string>('');
  page = signal(1);
  pageSize = signal(10);

  PermissionKeys = PermissionKeys;
  // single 5m helper — global OrgAdmin/SuperAdmin bypass inside hasPermissionSync, no hardcoded "project:create"
  canCreateProject = computed(() => this.perm.hasPermissionSync(this.selectedWorkspaceId() === 'all' ? (this.workspacesQuery.data()?.[0]?.id || '') : this.selectedWorkspaceId(), PermissionKeys.ProjectCreate));
  hasCustomProjectCreate = signal(false);
  canCreateProjectEffective = computed(() => {
    if (this.auth.isSuperAdmin() || this.auth.isOrgAdmin()) return true;
    const wss: any[] = this.workspacesQuery.data() || [];
    const ids = this.selectedWorkspaceId() === 'all' ? wss.map(w => w.id) : [this.selectedWorkspaceId()];
    return ids.some(id => this.perm.hasPermissionSync(id, PermissionKeys.ProjectCreate));
  });
  canViewProject = (workspaceId: string) => this.perm.hasPermissionSync(workspaceId, PermissionKeys.ProjectView);
  canUpdateProject = (workspaceId: string) => this.perm.hasPermissionSync(workspaceId, PermissionKeys.ProjectUpdate);
  canDeleteProject = (workspaceId: string) => this.perm.hasPermissionSync(workspaceId, PermissionKeys.ProjectDelete);

  selectedWorkspaceName = computed(() => {
    if (this.selectedWorkspaceId() === 'all') {
      return `All workspaces (${this.workspacesQuery.data()?.length || 0})`;
    }
    return this.workspacesQuery.data()?.find(w => w.id === this.selectedWorkspaceId())?.name || 'Select workspace';
  });

  createOpen = signal(false);
  editOpen = signal(false);
  deleteOpen = signal(false);
  editing = signal<any>(null);
  createError = signal<string | null>(null);

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyWorkspaces())
  }));

  constructor() {
    effect(() => {
      const wsId = this.selectedWorkspaceId();
      const wss: any = this.workspacesQuery.data();
      if (!wss) return;
      const ids = wsId === 'all' ? (wss as any[]).map(w => w.id) : [wsId];
      const hasAny = ids.some(id => this.perm.hasPermissionSync(id, PermissionKeys.ProjectCreate));
      this.hasCustomProjectCreate.set(hasAny);
    }, { allowSignalWrites: true });
  }

  visiblePages = computed(() => {
    const total = this.totalPages();
    const current = this.page();

    if (total <= 3) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    if (current === 1) {
      return [1, 2, 3];
    }

    if (current === total) {
      return [total - 2, total - 1, total];
    }

    return [current - 1, current, current + 1];
  });

  filteredWorkspaces = computed(() => {
    const s = this.wsSearch().toLowerCase().trim();
    const ws = this.workspacesQuery.data() || [];
    return s ? ws.filter(w => w.name.toLowerCase().includes(s)) : ws;
  });

  projectsQuery = injectQuery(() => ({
    queryKey: ['projects-global', this.selectedWorkspaceId()] as const,
    queryFn: async () => {
      const workspaces = this.workspacesQuery.data() ??
        await firstValueFrom(this.workspaceService.getMyWorkspaces());

      const ids = this.selectedWorkspaceId() === 'all'
        ? (workspaces as any[]).map(w => w.id)
        : [this.selectedWorkspaceId()];

      const results = await Promise.all(
        ids.map(id =>
          firstValueFrom(this.projectService.getProjects(id))
            .catch(() => ({ items: [], total: 0 } as any))
        )
      );

      const all = results.flatMap(r => r.items);
      return { items: all, total: all.length };
    }
  }));

  filtered = computed(() => {
    const s = this.projectSearch().toLowerCase().trim();
    const items = this.projectsQuery.data()?.items || [];

    return s
      ? items.filter((p: any) =>
          p.name.toLowerCase().includes(s) ||
          p.key.toLowerCase().includes(s)
        )
      : items;
  });

  total = computed(() => this.filtered().length);

  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize()))
  );

  paginated = computed(() => {
    const start = (this.page() - 1) * this.pageSize();
    return this.filtered().slice(start, start + this.pageSize());
  });

  showingFrom = computed(() => {
    if (this.total() === 0) return 0;
    return (this.page() - 1) * this.pageSize() + 1;
  });

  showingTo = computed(() => {
    if (this.total() === 0) return 0;
    return Math.min(this.page() * this.pageSize(), this.total());
  });

  createMutation = injectMutation(() => ({
    mutationFn: (vars: { workspaceId: string; name: string; description?: string }) =>
      firstValueFrom(
        this.projectService.createProject(
          vars.workspaceId,
          vars.name,
          vars.description
        )
      ),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['projects-global'] });
      this.queryClient.invalidateQueries({ queryKey: ['projects'] });
      this.createOpen.set(false);
      this.createError.set(null);
      this.toast.success('Project created');
    },
    onError: (err: any) => {
      const m = err.error?.error || 'Create failed';
      this.createError.set(m);
      this.toast.error(m);
    }
  }));

  updateMutation = injectMutation(() => ({
    mutationFn: (vars: { id: string; name: string; description?: string }) =>
      firstValueFrom(
        this.projectService.updateProject(
          vars.id,
          vars.name,
          vars.description
        )
      ),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['projects-global'] });
      this.queryClient.invalidateQueries({ queryKey: ['projects'] });
      this.editOpen.set(false);
      this.toast.success('Project updated');
    },
    onError: (err: any) =>
      this.toast.error(err.error?.error || 'Update failed')
  }));

  deleteMutation = injectMutation(() => ({
    mutationFn: (id: string) =>
      firstValueFrom(this.projectService.deleteProject(id)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['projects-global'] });
      this.queryClient.invalidateQueries({ queryKey: ['projects'] });
      this.deleteOpen.set(false);
      this.toast.success('Project deleted');
    },
    onError: (err: any) =>
      this.toast.error(err.error?.error || 'Delete failed')
  }));

  onProjectSearch(value: string) {
    this.projectSearch.set(value);
    this.page.set(1);
  }

  onPageSizeChange(value: string) {
    this.pageSize.set(Number(value));
    this.page.set(1);
  }

  selectWorkspace(id: string) {
    this.selectedWorkspaceId.set(id);
    this.wsDropdownOpen.set(false);
    this.wsSearch.set('');
    this.page.set(1);
  }

  openCreate() {
    this.createError.set(null);
    this.createOpen.set(true);
  }

  openEdit(p: any) {
    this.editing.set(p);
    this.editOpen.set(true);
  }

  openDelete(p: any) {
    this.editing.set(p);
    this.deleteOpen.set(true);
  }

  onCreateSubmit(e: { name: string; description: string; workspaceId: string }) {
    this.createMutation.mutate({
      workspaceId: e.workspaceId,
      name: e.name,
      description: e.description
    });
  }

  onEditSubmit(e: { name: string; description: string }) {
    this.updateMutation.mutate({
      id: this.editing().id,
      name: e.name,
      description: e.description
    });
  }

  onDeleteConfirm() {
    this.deleteMutation.mutate(this.editing().id);
  }
}