import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';
import { ToastService } from '../../../core/services/toast.service';
import { ROLE_LABEL_MAP, OrgRoleValues } from '../../../shared/constants/roles';

@Component({
  selector: 'app-superadmin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UsersComponent {
  private sa = inject(SuperAdminService);
  private router = inject(Router);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);

  search = signal('');
  searchInput = signal('');
  page = signal(1);
  pageSize = signal(10);

  suspendTarget = signal<any | null>(null);
  suspendReason = signal('');
  suspendMessage = signal('');
  suspendDays = signal(7);

  deleteTarget = signal<any | null>(null);
  deleteIsLastAdmin = signal(false);
  deleteOrgName = signal('');

  query = injectQuery(() => ({
    queryKey: ['superadmin-users', this.search(), this.page(), this.pageSize()] as const,
    queryFn: () => firstValueFrom(
      this.sa.getUsers(
        this.search() || undefined,
        this.page(),
        this.pageSize()
      )
    ),
  }));

  get items() {
    return this.query.data()?.items ?? [];
  }

  get total() {
    return this.query.data()?.total ?? 0;
  }

  get totalPages() {
    return Math.max(1, Math.ceil(this.total / this.pageSize()));
  }

  get showingFrom() {
    return this.total === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1;
  }

  get showingTo() {
    return Math.min(this.page() * this.pageSize(), this.total);
  }

  visiblePages(): number[] {
    const total = this.totalPages;
    const current = this.page();

    if (total <= 3) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    if (current <= 2) {
      return [1, 2, 3];
    }

    if (current >= total - 1) {
      return [total - 2, total - 1, total];
    }

    return [current - 1, current, current + 1];
  }

  setPage(value: number) {
    const target = Math.min(Math.max(1, value), this.totalPages);
    this.page.set(target);
  }

  firstPage() {
    if (this.page() > 1) {
      this.page.set(1);
    }
  }

  prevPage() {
    if (this.page() > 1) {
      this.page.update(v => v - 1);
    }
  }

  nextPage() {
    if (this.page() < this.totalPages) {
      this.page.update(v => v + 1);
    }
  }

  lastPage() {
    if (this.page() < this.totalPages) {
      this.page.set(this.totalPages);
    }
  }

  onPageSizeChange(value: any) {
    const size = Number(value) || 10;
    this.pageSize.set(size);
    this.page.set(1);
  }

  onSearch() {
    this.search.set(this.searchInput().trim());
    this.page.set(1);
  }

  clearSearch() {
    this.searchInput.set('');
    this.search.set('');
    this.page.set(1);
  }

  viewMembers(row: any) {
    this.router.navigate(['/superadmin/organizations', row.organizationId, 'members']);
  }

  openSuspend(row: any) {
    this.suspendTarget.set(row);
    this.suspendReason.set('');
    this.suspendMessage.set(
      'Your account is scheduled for suspension. Please promote another OrgAdmin before deadline or organization will be suspended.'
    );
    this.suspendDays.set(7);
  }

  closeSuspend() {
    this.suspendTarget.set(null);
  }

  suspendMutation = injectMutation(() => ({
    mutationFn: () => {
      const t = this.suspendTarget()!;
      return firstValueFrom(
        this.sa.suspendUser(
          t.id,
          t.organizationId,
          this.suspendReason() || 'Suspended by platform',
          this.suspendMessage(),
          this.suspendDays()
        )
      );
    },
    onSuccess: () => {
      this.toast.success('Suspension scheduled with grace period');
      this.closeSuspend();
      this.qc.invalidateQueries({ queryKey: ['superadmin-users'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Suspend failed')
  }));

  activateMutation = injectMutation(() => ({
    mutationFn: (row: any) => firstValueFrom(this.sa.activateUser(row.id)),
    onSuccess: () => {
      this.toast.success('User reactivated');
      this.qc.invalidateQueries({ queryKey: ['superadmin-users'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Activate failed')
  }));

  deleteMutation = injectMutation(() => ({
    mutationFn: (row: any) => firstValueFrom(this.sa.deleteUser(row.id)),
    onSuccess: () => {
      this.toast.success('User deleted');
      this.closeDelete();
      this.qc.invalidateQueries({ queryKey: ['superadmin-users'] });
      this.qc.invalidateQueries({ queryKey: ['superadmin-orgs'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Delete failed')
  }));

  confirmSuspend() {
    if (!this.suspendReason().trim()) {
      this.toast.error('Reason required');
      return;
    }

    this.suspendMutation.mutate();
  }

  onActivate(row: any) {
    this.activateMutation.mutate(row);
  }

  async onDelete(row: any) {
    try {
      const res: any = await firstValueFrom(
        this.sa.getOrgMembers(row.organizationId, undefined, 1, 50)
      );

      const orgAdminCount = (res.items || []).filter(
        (m: any) => m.orgRole === ROLE_LABEL_MAP[String(OrgRoleValues.OrgAdmin)]
      ).length;

      const isLast = orgAdminCount <= 1;

      this.deleteTarget.set(row);
      this.deleteIsLastAdmin.set(isLast);
      this.deleteOrgName.set(row.organizationName);
    } catch {
      this.deleteTarget.set(row);
      this.deleteIsLastAdmin.set(false);
    }
  }

  closeDelete() {
    this.deleteTarget.set(null);
    this.deleteIsLastAdmin.set(false);
  }

  confirmDelete() {
    const row = this.deleteTarget();
    if (!row) return;

    this.deleteMutation.mutate(row);
  }
}