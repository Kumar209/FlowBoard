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

  query = injectQuery(() => ({
    queryKey: ['superadmin-complaints'] as const,
    queryFn: () => firstValueFrom(this.sa.getSuperAdminComplaints()),
  }));

  get complaints() { return this.query.data() ?? []; }

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
