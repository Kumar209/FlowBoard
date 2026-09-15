import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ProjectService } from '../../../core/services/project.service';
import { ToastService } from '../../../core/services/toast.service';
import { PermissionService } from '../../../core/services/permission.service';
import { AuthService } from '../../../core/services/auth.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-statuses',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './statuses.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatusesComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);
  private perm = inject(PermissionService);
  private auth = inject(AuthService);

  workspaceId = signal(
    this.route.parent?.snapshot.paramMap.get('wid') ||
    this.route.snapshot.paramMap.get('wid') ||
    ''
  );

  projectId = signal(
    this.route.parent?.snapshot.paramMap.get('pid') ||
    this.route.snapshot.paramMap.get('pid') ||
    ''
  );

  search = signal('');
  showCreate = signal(false);

  canCreate = signal(false);
  canUpdate = signal(false);
  canDelete = signal(false);

  constructor() {
    this.route.parent?.paramMap.subscribe(m => {
      const pid = m.get('pid');
      if (pid) this.projectId.set(pid);

      const wid = m.get('wid');
      if (wid) this.workspaceId.set(wid);
    });

    const check = async () => {
      const wid = this.workspaceId();

      if (!wid) return;

      const c =
        this.auth.hasPermission(wid, 'status:create') ||
        await this.perm.hasPermission(wid, 'status:create').catch(() => false);

      const u =
        this.auth.hasPermission(wid, 'status:update') ||
        await this.perm.hasPermission(wid, 'status:update').catch(() => false);

      const d =
        this.auth.hasPermission(wid, 'status:delete') ||
        await this.perm.hasPermission(wid, 'status:delete').catch(() => false);

      this.canCreate.set(
        c || this.auth.isOrgAdmin() || this.auth.isSuperAdmin()
      );

      this.canUpdate.set(
        u || this.auth.isOrgAdmin() || this.auth.isSuperAdmin()
      );

      this.canDelete.set(
        d || this.auth.isOrgAdmin() || this.auth.isSuperAdmin()
      );
    };

    setTimeout(check, 400);
  }

  newName = signal('');
  editing = signal<any>(null);
  editName = signal('');
  deleteTarget = signal<any>(null);

  statusesQuery = injectQuery(() => ({
    queryKey: ['statuses', this.projectId()] as const,
    queryFn: () =>
      firstValueFrom(
        this.ps.getStatuses(this.projectId())
      ),
    enabled: !!this.projectId()
  }));

  allStatuses = computed(
    () => (this.statusesQuery.data() as any[]) || []
  );

  filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    const list = this.allStatuses();

    if (!q) return list;

    return list.filter((s: any) =>
      s.name.toLowerCase().includes(q)
    );
  });

  createMutation = injectMutation(() => ({
    mutationFn: () =>
      firstValueFrom(
        this.ps.createStatus(
          this.projectId(),
          this.newName().trim()
        )
      ),
    onSuccess: () => {
      this.qc.invalidateQueries({
        queryKey: ['statuses', this.projectId()]
      });

      this.showCreate.set(false);
      this.newName.set('');
      this.toast.success('Status created');
    },
    onError: (e: any) =>
      this.toast.error(
        e.error?.error || 'Create failed'
      )
  }));

  updateMutation = injectMutation(() => ({
    mutationFn: () =>
      firstValueFrom(
        this.ps.updateStatus(
          this.editing()!.id,
          this.editName().trim()
        )
      ),
    onSuccess: () => {
      this.qc.invalidateQueries({
        queryKey: ['statuses', this.projectId()]
      });

      this.editing.set(null);
      this.editName.set('');
      this.toast.success('Status updated');
    },
    onError: (e: any) =>
      this.toast.error(
        e.error?.error || 'Update failed'
      )
  }));

  deleteMutation = injectMutation(() => ({
    mutationFn: (id: string) =>
      firstValueFrom(
        this.ps.deleteStatus(id)
      ),
    onSuccess: () => {
      this.qc.invalidateQueries({
        queryKey: ['statuses', this.projectId()]
      });

      this.deleteTarget.set(null);
      this.toast.success('Status deleted');
    },
    onError: (e: any) =>
      this.toast.error(
        e.error?.error || 'Delete failed'
      )
  }));

  openCreate() {
    this.newName.set('');
    this.showCreate.set(true);
  }

  closeCreate() {
    if (this.createMutation.isPending()) return;

    this.showCreate.set(false);
    this.newName.set('');
  }

  doCreate() {
    const name = this.newName().trim();

    if (!name || this.createMutation.isPending()) return;

    this.createMutation.mutate();
  }

  openEdit(status: any) {
    this.editing.set(status);
    this.editName.set(status.name);
  }

  closeEdit() {
    if (this.updateMutation.isPending()) return;

    this.editing.set(null);
    this.editName.set('');
  }

  doUpdate() {
    const name = this.editName().trim();

    if (!name || this.updateMutation.isPending()) return;

    this.updateMutation.mutate();
  }

  openDelete(status: any) {
    this.deleteTarget.set(status);
  }

  closeDelete() {
    if (this.deleteMutation.isPending()) return;

    this.deleteTarget.set(null);
  }

  confirmDelete() {
    const target = this.deleteTarget();

    if (!target || this.deleteMutation.isPending()) return;

    this.deleteMutation.mutate(target.id);
  }

  onSearchChange(value: string) {
    this.search.set(value);
  }
}