import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-superadmin-organizations',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './organizations.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrganizationsComponent {
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

  deleteTarget = signal<any | null>(null);

  query = injectQuery(() => ({
    queryKey: ['superadmin-orgs', this.search(), this.page(), this.pageSize()] as const,
    queryFn: () => firstValueFrom(
      this.sa.getOrganizations(
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
    this.setPage(1);
  }

  previousPage() {
    if (this.page() > 1) {
      this.setPage(this.page() - 1);
    }
  }

  nextPage() {
    if (this.page() < this.totalPages) {
      this.setPage(this.page() + 1);
    }
  }

  lastPage() {
    this.setPage(this.totalPages);
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

  planBadgeClass(plan: string) {
    switch (plan) {
      case 'Free':
        return 'badge-ghost';
      case 'Pro':
        return 'badge-primary';
      case 'Business':
        return 'badge-secondary';
      case 'Enterprise':
        return 'badge-accent';
      default:
        return 'badge-ghost';
    }
  }

  viewMembers(row: any) {
    this.router.navigate(['/superadmin/organizations', row.id, 'members']);
  }

  suspendMutation = injectMutation(() => ({
    mutationFn: () => {
      const t = this.suspendTarget()!;
      return firstValueFrom(
        this.sa.suspendOrganization(
          t.id,
          this.suspendReason() || 'Suspended by platform',
          this.suspendMessage()
        )
      );
    },
    onSuccess: () => {
      this.toast.success('Organization suspended');
      this.closeSuspend();
      this.qc.invalidateQueries({ queryKey: ['superadmin-orgs'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Suspend failed')
  }));

  activateMutation = injectMutation(() => ({
    mutationFn: (row: any) => firstValueFrom(
      this.sa.activateOrganization(row.id)
    ),
    onSuccess: () => {
      this.toast.success('Organization activated');
      this.qc.invalidateQueries({ queryKey: ['superadmin-orgs'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Activate failed')
  }));

  deleteMutation = injectMutation(() => ({
    mutationFn: (row: any) => firstValueFrom(
      this.sa.deleteOrganization(row.id)
    ),
    onSuccess: () => {
      this.toast.success('Organization deleted with all data');
      this.closeDelete();
      this.qc.invalidateQueries({ queryKey: ['superadmin-orgs'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Delete failed')
  }));

  openSuspend(row: any) {
    this.suspendTarget.set(row);
    this.suspendReason.set('');
    this.suspendMessage.set(
      `Your organization ${row.name} has been suspended by the platform. Please contact support at superadmin@flowboard.local to appeal.`
    );
  }

  closeSuspend() {
    this.suspendTarget.set(null);
    this.suspendReason.set('');
    this.suspendMessage.set('');
  }

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

  onDelete(row: any) {
    this.deleteTarget.set(row);
  }

  closeDelete() {
    this.deleteTarget.set(null);
  }

  confirmDelete() {
    const row = this.deleteTarget();
    if (!row) return;
    this.deleteMutation.mutate(row);
  }
}