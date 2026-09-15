import { Component, ChangeDetectionStrategy, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../core/services/superadmin.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-support',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './support.component.html',
  styleUrls: ['./support.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SupportComponent {
  private sa = inject(SuperAdminService);
  private ws = inject(WorkspaceService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);
  private router = inject(Router);
  orgId = signal<string>('');
  subject = signal('');
  message = signal('');
  showCreateModal = signal(false);
  deleteConfirmId = signal<string | null>(null);
  page = signal(1);
  pageSize = signal(10);

  orgsQuery = injectQuery(() => ({
    queryKey: ['organizations'] as const,
    queryFn: () => firstValueFrom(this.ws.getMyOrganizations()),
  }));

  complaintsQuery = injectQuery(() => ({
    queryKey: ['complaints', this.orgId(), this.page(), this.pageSize()] as const,
    queryFn: () => firstValueFrom(this.sa.getComplaints(this.orgId(), this.page(), this.pageSize())),
    enabled: !!this.orgId(),
  }));

  get complaints() {
    const d: any = this.complaintsQuery.data();
    if (!d) return [];
    if (Array.isArray(d)) return d;
    return d.items ?? [];
  }
  get total() {
    const d: any = this.complaintsQuery.data();
    if (!d) return 0;
    if (Array.isArray(d)) return d.length;
    return d.total ?? 0;
  }
  get totalPages() { return Math.max(1, Math.ceil(this.total / this.pageSize())); }
  get showingFrom() { return this.total === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1; }
  get showingTo() { return Math.min(this.page() * this.pageSize(), this.total); }
  visiblePages(): number[] {
    const total = this.totalPages; const cur = this.page(); const range = 2;
    let start = Math.max(1, cur - range), end = Math.min(total, cur + range);
    if (cur <= 3) end = Math.min(total, 5);
    if (cur >= total - 2) start = Math.max(1, total - 4);
    const pages: number[] = []; for (let i = start; i <= end; i++) pages.push(i); return pages;
  }
  previousPage() { if (this.page() > 1) this.page.set(this.page() - 1); }
  nextPage() { if (this.page() < this.totalPages) this.page.set(this.page() + 1); }
  firstPage() { this.page.set(1); }
  lastPage() { this.page.set(this.totalPages); }
  onPageSizeChange(v: any) { this.pageSize.set(Number(v) || 10); this.page.set(1); }

  getOrgs(): any[] {
    const data: any = this.orgsQuery.data();
    if (!data) return [];
    if (Array.isArray(data)) return data;
    if (data.items) return data.items;
    return [];
  }

  constructor() {
    effect(() => {
      const orgs: any = this.orgsQuery.data();
      if (orgs && Array.isArray(orgs) && orgs.length && !this.orgId()) this.orgId.set(orgs[0].id);
      if (orgs && !Array.isArray(orgs) && (orgs as any).items && (orgs as any).items.length && !this.orgId()) this.orgId.set((orgs as any).items[0].id);
    });
  }

  createMutation = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.sa.createComplaint(this.orgId(), this.subject().trim(), this.message().trim())),
    onSuccess: () => { this.toast.success('Complaint sent to platform'); this.subject.set(''); this.message.set(''); this.showCreateModal.set(false); this.page.set(1); this.qc.invalidateQueries({ queryKey: ['complaints'] }); },
    onError: (e: any) => this.toast.error(e.error?.error || 'Failed to send')
  }));

  deleteMutation = injectMutation(() => ({
    mutationFn: (complaintId: string) => firstValueFrom(this.sa.deleteComplaint(this.orgId(), complaintId)),
    onSuccess: () => { this.toast.success('Complaint deleted'); this.deleteConfirmId.set(null); this.qc.invalidateQueries({ queryKey: ['complaints'] }); },
    onError: (e: any) => this.toast.error(e.error?.error || 'Delete failed')
  }));

  onCreate() {
    if (!this.subject().trim() || !this.message().trim()) { this.toast.error('Subject and message required'); return; }
    if (!this.orgId()) { this.toast.error('Organization not found'); return; }
    this.createMutation.mutate();
  }

  onView(c: any) {
    this.router.navigate(['/support', c.id], { queryParams: { orgId: this.orgId() } });
  }

  onDelete(c: any) {
    this.deleteConfirmId.set(c.id);
  }

  confirmDelete() {
    const id = this.deleteConfirmId();
    if (id) (this.deleteMutation as any).mutate(id);
  }
}
