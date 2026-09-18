import { Component, inject, signal, ChangeDetectionStrategy, computed, effect } from '@angular/core';
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
import { ROLE_LABEL_MAP, OrgRoleValues } from '../../shared/constants/roles';
import { PermissionService } from '../../core/services/permission.service';
import { PermissionKeys } from '../../shared/constants/permissions';

@Component({
  selector: 'app-workspace',
  standalone: true,
  imports: [CommonModule, RouterLink, ProjectModalComponent, ConfirmDeleteComponent],
  templateUrl: './workspace.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkspaceComponent {
  private route = inject(ActivatedRoute);
  projectService = inject(ProjectService);
  auth = inject(AuthService);
  private toast = inject(ToastService);
  private workspaceService = inject(WorkspaceService);
  private perm = inject(PermissionService);
  private queryClient = inject(QueryClient);

  workspaceId = signal<string>(
    this.route.snapshot.paramMap.get('wid') || '11111111-1111-1111-1111-111111111111',
  );

  createOpen = signal(false);
  editOpen = signal(false);
  deleteOpen = signal(false);
  editing = signal<any>(null);
  createError = signal<string | null>(null);
  search = signal('');
  page = signal(1);
  pageSize = signal(10);

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyWorkspaces()),
  }));

  workspaceName = computed(() => {
    const ws = this.workspacesQuery.data()?.find((w) => w.id === this.workspaceId());
    return ws?.name || 'Workspace';
  });

  PermissionKeys = PermissionKeys;
  canViewProject = computed(() => this.perm.hasPermissionSync(this.workspaceId(), PermissionKeys.ProjectView));
  canCreateProject = computed(() => this.perm.hasPermissionSync(this.workspaceId(), PermissionKeys.ProjectCreate));
  canUpdateProject = (project?: any) => this.perm.hasPermissionSync(this.workspaceId(), PermissionKeys.ProjectUpdate);
  canDeleteProject = (project?: any) => this.perm.hasPermissionSync(this.workspaceId(), PermissionKeys.ProjectDelete);

  roleLabel = computed(() => {
    const wid = this.workspaceId();
    const m = this.auth.memberships().find((x) => x.workspaceId === wid);
    const raw = m?.roleName ?? m?.role;

    if (raw !== undefined) {
      return (ROLE_LABEL_MAP as any)[String(raw)] ?? String(raw);
    }

    const ws = this.workspacesQuery.data()?.find((w) => w.id === wid);

    if (ws?.role !== undefined) {
      return (ROLE_LABEL_MAP as any)[String(ws.role)] ?? String(ws.role);
    }

    return ROLE_LABEL_MAP[String(OrgRoleValues.Member)];
  });

  projectsQuery = injectQuery(() => ({
    queryKey: ['projects', this.workspaceId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getProjects(this.workspaceId())),
  }));

  projectMemberMap = signal<Map<string, boolean>>(new Map());

  constructor() {
    effect(async () => {
      const items: any[] = this.projectsQuery.data()?.items || [];
      if (!items.length) { this.projectMemberMap.set(new Map()); return; }
      if (this.auth.isOrgAdmin() || this.auth.isSuperAdmin()) { this.projectMemberMap.set(new Map()); return; }
      const map = new Map<string, boolean>();
      for (const p of items as any[]) {
        try {
          const res: any = await firstValueFrom(this.projectService.getProjectMembers(p.id, 1, 100));
          const members = res?.items || res?.Items || (Array.isArray(res) ? res : []);
          const isMember = members.some((m: any) => m.userId === this.auth.currentUser()?.id || m.UserId === this.auth.currentUser()?.id);
          map.set(p.id, isMember || p.ownerId === this.auth.currentUser()?.id);
        } catch { map.set(p.id, false); }
      }
      this.projectMemberMap.set(map);
    }, { allowSignalWrites: true });
  }

  filteredProjects = computed(() => {
    const s = this.search().toLowerCase().trim();
    let items: any[] = this.projectsQuery.data()?.items || [];
    // MNC grade: only show projects where user is member unless OrgAdmin/SuperAdmin — private projects
    if (!this.auth.isOrgAdmin() && !this.auth.isSuperAdmin()) {
      const map = this.projectMemberMap();
      // If map empty (still loading), show all to avoid flicker, else filter
      if (map.size > 0) items = items.filter((p: any) => map.get(p.id) === true);
    }

    return s
      ? items.filter(
          (p: any) =>
            p.name.toLowerCase().includes(s) ||
            p.key.toLowerCase().includes(s),
        )
      : items;
  });

  total = computed(() => this.filteredProjects().length);

  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize()))
  );

  paginatedProjects = computed(() => {
    const start = (this.page() - 1) * this.pageSize();
    return this.filteredProjects().slice(start, start + this.pageSize());
  });

  showingFrom = computed(() => {
    const total = this.total();
    if (!total) return 0;
    return (this.page() - 1) * this.pageSize() + 1;
  });

  showingTo = computed(() =>
    Math.min(this.page() * this.pageSize(), this.total())
  );

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

  createMutation = injectMutation(() => ({
    mutationFn: (vars: { name: string; description?: string }) =>
      firstValueFrom(
        this.projectService.createProject(
          this.workspaceId(),
          vars.name,
          vars.description,
        ),
      ),
    onSuccess: () => {
      this.queryClient.invalidateQueries({
        queryKey: ['projects', this.workspaceId()],
      });
      this.queryClient.invalidateQueries({ queryKey: ['projects-global'] });
      this.queryClient.invalidateQueries({ queryKey: ['board'] });
      this.createOpen.set(false);
      this.createError.set(null);
      this.toast.success('Project created');
    },
    onError: (err: any) => {
      const m = err.error?.error || 'Create failed';
      this.createError.set(m);
      this.toast.error(m);
    },
  }));

  updateMutation = injectMutation(() => ({
    mutationFn: (vars: { id: string; name: string; description?: string }) =>
      firstValueFrom(
        this.projectService.updateProject(
          vars.id,
          vars.name,
          vars.description,
        ),
      ),
    onSuccess: () => {
      this.queryClient.invalidateQueries({
        queryKey: ['projects', this.workspaceId()],
      });
      this.queryClient.invalidateQueries({ queryKey: ['projects-global'] });
      this.queryClient.invalidateQueries({ queryKey: ['board'] });
      this.editOpen.set(false);
      this.toast.success('Project updated');
    },
    onError: (err: any) =>
      this.toast.error(err.error?.error || 'Update failed'),
  }));

  deleteMutation = injectMutation(() => ({
    mutationFn: (id: string) =>
      firstValueFrom(this.projectService.deleteProject(id)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({
        queryKey: ['projects', this.workspaceId()],
      });
      this.queryClient.invalidateQueries({ queryKey: ['projects-global'] });
      this.deleteOpen.set(false);
      this.toast.success('Project deleted');
    },
    onError: (err: any) =>
      this.toast.error(err.error?.error || 'Delete failed'),
  }));

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

  onCreateSubmit(e: { name: string; description: string }) {
    this.createMutation.mutate({
      name: e.name,
      description: e.description,
    });
  }

  onEditSubmit(e: { name: string; description: string }) {
    const project = this.editing();
    if (!project) return;

    this.updateMutation.mutate({
      id: project.id,
      name: e.name,
      description: e.description,
    });
  }

  onDeleteConfirm() {
    const project = this.editing();
    if (!project) return;

    this.deleteMutation.mutate(project.id);
  }

  firstPage() {
    this.page.set(1);
  }

  previousPage() {
    if (this.page() > 1) {
      this.page.set(this.page() - 1);
    }
  }

  nextPage() {
    if (this.page() < this.totalPages()) {
      this.page.set(this.page() + 1);
    }
  }

  lastPage() {
    this.page.set(this.totalPages());
  }

  onPageSizeChange(value: string) {
    const size = Number(value);

    if (!Number.isFinite(size) || size <= 0) return;

    this.pageSize.set(size);
    this.page.set(1);
  }
}