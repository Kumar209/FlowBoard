import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-superadmin-support',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './support.component.html',
  styleUrls: ['./support.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SupportComponent {
  private sa = inject(SuperAdminService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);
  private router = inject(Router);
  deleteConfirmId = signal<string | null>(null);
  page = signal(1);
  pageSize = signal(10);

  query = injectQuery(() => ({
    queryKey: ['superadmin-complaints', this.page(), this.pageSize()] as const,
    queryFn: () => firstValueFrom(this.sa.getSuperAdminComplaints(undefined, this.page(), this.pageSize())),
  }));

  get complaints() {
    const d: any = this.query.data();
    if (!d) return [];
    if (Array.isArray(d)) return d;
    return d.items ?? [];
  }
  get total() {
    const d: any = this.query.data();
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

  deleteMutation = injectMutation(() => ({
    mutationFn: (id: string) => firstValueFrom(this.sa.deleteSuperAdminComplaint(id)),
    onSuccess: () => { this.toast.success('Complaint deleted'); this.deleteConfirmId.set(null); this.qc.invalidateQueries({ queryKey: ['superadmin-complaints'] }); },
    onError: (e: any) => this.toast.error(e.error?.error || 'Delete failed')
  }));

  onView(c: any) {
    this.router.navigate(['/superadmin/support', c.id]);
  }

  onDelete(c: any) {
    this.deleteConfirmId.set(c.id);
  }

  confirmDelete() {
    const id = this.deleteConfirmId();
    if (id) (this.deleteMutation as any).mutate(id);
  }
}
