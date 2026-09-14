import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-complaint-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './complaint-detail.component.html',
  styleUrls: ['./complaint-detail.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ComplaintDetailComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private sa = inject(SuperAdminService);
  private ws = inject(WorkspaceService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);

  complaintId = signal(this.route.snapshot.paramMap.get('id') ?? '');
  orgId = signal(this.route.snapshot.queryParamMap.get('orgId') ?? '');
  replyText = signal('');
  deleteConfirm = signal(false);

  orgsQuery = injectQuery(() => ({
    queryKey: ['organizations-detail'] as const,
    queryFn: () => firstValueFrom(this.ws.getMyOrganizations()),
    enabled: !this.orgId(),
  }));

  detailQuery = injectQuery(() => ({
    queryKey: ['complaint-detail', this.orgId(), this.complaintId()] as const,
    queryFn: async () => {
      let oid = this.orgId();
      if (!oid) {
        const orgs: any = await firstValueFrom(this.ws.getMyOrganizations());
        const arr = Array.isArray(orgs) ? orgs : (orgs.items ?? []);
        if (arr.length) oid = arr[0].id;
      }
      if (!oid) throw new Error('Organization not found');
      return firstValueFrom(this.sa.getComplaintDetail(oid, this.complaintId()));
    },
    enabled: !!this.complaintId(),
  }));

  get detail() { return this.detailQuery.data() as any; }
  get complaint() { return this.detail?.complaint ?? null; }
  get replies() { return (this.detail?.replies ?? []) as any[]; }

  replyMutation = injectMutation(() => ({
    mutationFn: () => {
      const oid = this.orgId() || this.complaint?.organizationId;
      if (!oid) throw new Error('Organization not found');
      return firstValueFrom(this.sa.replyComplaint(oid, this.complaintId(), this.replyText().trim()));
    },
    onSuccess: () => { this.toast.success('Reply sent'); this.replyText.set(''); this.qc.invalidateQueries({ queryKey: ['complaint-detail', this.orgId(), this.complaintId()] }); this.qc.invalidateQueries({ queryKey: ['complaints'] }); },
    onError: (e: any) => this.toast.error(e.error?.error || 'Reply failed')
  }));

  deleteMutation = injectMutation(() => ({
    mutationFn: () => {
      const oid = this.orgId() || this.complaint?.organizationId;
      if (!oid) throw new Error('Organization not found');
      return firstValueFrom(this.sa.deleteComplaint(oid, this.complaintId()));
    },
    onSuccess: () => { this.toast.success('Complaint deleted'); this.router.navigate(['/support']); },
    onError: (e: any) => this.toast.error(e.error?.error || 'Delete failed')
  }));

  onReply() {
    if (!this.replyText().trim()) { this.toast.error('Message required'); return; }
    this.replyMutation.mutate();
  }

  onDelete() { this.deleteConfirm.set(true); }
  confirmDelete() { this.deleteMutation.mutate(); }
  goBack() { this.router.navigate(['/support']); }
}
