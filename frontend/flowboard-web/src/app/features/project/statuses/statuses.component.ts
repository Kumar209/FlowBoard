import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ProjectService } from '../../../core/services/project.service';
import { ToastService } from '../../../core/services/toast.service';
import { PermissionService } from '../../../core/services/permission.service';
import { AuthService } from '../../../core/services/auth.service';
import { PermissionKeys } from '../../../shared/constants/permissions';
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

  PermissionKeys = PermissionKeys;
  canView = computed(() => this.perm.hasPermissionSync(this.workspaceId(), PermissionKeys.StatusView));
  canCreate = computed(() => this.perm.hasPermissionSync(this.workspaceId(), PermissionKeys.StatusCreate));
  canUpdate = computed(() => this.perm.hasPermissionSync(this.workspaceId(), PermissionKeys.StatusUpdate));
  canDelete = computed(() => this.perm.hasPermissionSync(this.workspaceId(), PermissionKeys.StatusDelete));

  constructor() {
    this.route.parent?.paramMap.subscribe(m => {
      const pid = m.get('pid');
      if (pid) this.projectId.set(pid);

      const wid = m.get('wid');
      if (wid) this.workspaceId.set(wid);
    });
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