import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../../core/services/superadmin.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-superadmin-complaint-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './complaint-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ComplaintDetailComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private sa = inject(SuperAdminService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);

  complaintId = signal(this.route.snapshot.paramMap.get('id') ?? '');
  replyText = signal('');
  deleteConfirm = signal(false);

  detailQuery = injectQuery(() => ({
    queryKey: ['superadmin-complaint-detail', this.complaintId()] as const,
    queryFn: () => firstValueFrom(this.sa.getSuperAdminComplaintDetail(this.complaintId())),
    enabled: !!this.complaintId(),
  }));

  get detail() {
    return this.detailQuery.data() as any;
  }

  get complaint() {
    return this.detail?.complaint ?? null;
  }

  get replies() {
    return (this.detail?.replies ?? []) as any[];
  }

  replyMutation = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.sa.replyAsSuperAdmin(this.complaintId(), this.replyText().trim())),
    onSuccess: () => {
      this.toast.success('Reply sent');
      this.replyText.set('');
      this.qc.invalidateQueries({ queryKey: ['superadmin-complaint-detail', this.complaintId()] });
      this.qc.invalidateQueries({ queryKey: ['superadmin-complaints'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Reply failed')
  }));

  deleteMutation = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.sa.deleteSuperAdminComplaint(this.complaintId())),
    onSuccess: () => {
      this.toast.success('Complaint deleted');
      this.router.navigate(['/superadmin/support']);
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Delete failed')
  }));

  onReply() {
    if (!this.replyText().trim()) {
      this.toast.error('Message required');
      return;
    }

    this.replyMutation.mutate();
  }

  onDelete() {
    this.deleteConfirm.set(true);
  }

  confirmDelete() {
    this.deleteMutation.mutate();
  }

  goBack() {
    this.router.navigate(['/superadmin/support']);
  }
}