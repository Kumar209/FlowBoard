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
  styleUrls: ['./organizations.component.css'],
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
  pageSize = 10;
  suspendTarget = signal<any | null>(null);
  suspendReason = signal('');
  suspendMessage = signal('');

  query = injectQuery(() => ({
    queryKey: ['superadmin-orgs', this.search(), this.page()] as const,
    queryFn: () => firstValueFrom(this.sa.getOrganizations(this.search() || undefined, this.page(), this.pageSize)),
  }));

  get items() { return this.query.data()?.items ?? []; }
  get total() { return this.query.data()?.total ?? 0; }
  get totalPages() { return Math.max(1, Math.ceil(this.total / this.pageSize)); }

  onSearch() {
    this.search.set(this.searchInput().trim());
    this.page.set(1);
  }
  clearSearch() {
    this.searchInput.set('');
    this.search.set('');
    this.page.set(1);
  }
  nextPage() { if (this.page() < this.totalPages) this.page.update(v => v + 1); }
  prevPage() { if (this.page() > 1) this.page.update(v => v - 1); }

  planBadgeClass(plan: string) {
    switch (plan) {
      case 'Free': return 'badge-ghost';
      case 'Pro': return 'badge-primary';
      case 'Business': return 'badge-secondary';
      case 'Enterprise': return 'badge-accent';
      default: return 'badge-ghost';
    }
  }

  viewMembers(row: any) { this.router.navigate(['/superadmin/organizations', row.id, 'members']); }

  suspendMutation = injectMutation(() => ({
    mutationFn: () => {
      const t = this.suspendTarget()!;
      return firstValueFrom(this.sa.suspendOrganization(t.id, this.suspendReason() || 'Suspended by platform', this.suspendMessage()));
    },
    onSuccess: () => { this.toast.success('Organization suspended'); this.closeSuspend(); this.qc.invalidateQueries({ queryKey: ['superadmin-orgs'] }); },
    onError: (e: any) => this.toast.error(e.error?.error || 'Suspend failed')
  }));
  activateMutation = injectMutation(() => ({
    mutationFn: (row: any) => firstValueFrom(this.sa.activateOrganization(row.id)),
    onSuccess: () => { this.toast.success('Organization activated'); this.qc.invalidateQueries({ queryKey: ['superadmin-orgs'] }); },
    onError: (e: any) => this.toast.error(e.error?.error || 'Activate failed')
  }));
  deleteMutation = injectMutation(() => ({
    mutationFn: (row: any) => firstValueFrom(this.sa.deleteOrganization(row.id)),
    onSuccess: () => { this.toast.success('Organization deleted with all data'); this.qc.invalidateQueries({ queryKey: ['superadmin-orgs'] }); },
    onError: (e: any) => this.toast.error(e.error?.error || 'Delete failed')
  }));

  openSuspend(row: any) {
    this.suspendTarget.set(row);
    this.suspendReason.set('');
    this.suspendMessage.set(`Your organization ${row.name} has been suspended by the platform. Please contact support at superadmin@flowboard.local to appeal.`);
  }
  closeSuspend() { this.suspendTarget.set(null); this.suspendReason.set(''); this.suspendMessage.set(''); }
  confirmSuspend() {
    if (!this.suspendReason().trim()) { this.toast.error('Reason required'); return; }
    this.suspendMutation.mutate();
  }
  onActivate(row: any) { this.activateMutation.mutate(row); }
  onDelete(row: any) { if (!confirm(`Delete organization ${row.name}? This will delete all workspaces, projects, tasks and members permanently.`)) return; this.deleteMutation.mutate(row); }
}
