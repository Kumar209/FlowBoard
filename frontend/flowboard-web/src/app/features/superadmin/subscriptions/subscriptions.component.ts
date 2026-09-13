import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgApexchartsModule } from 'ng-apexcharts';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-superadmin-subscriptions',
  standalone: true,
  imports: [CommonModule, FormsModule, NgApexchartsModule],
  templateUrl: './subscriptions.component.html',
  styleUrls: ['./subscriptions.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SubscriptionsComponent {
  private sa = inject(SuperAdminService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);

  query = injectQuery(() => ({
    queryKey: ['superadmin-subscriptions'] as const,
    queryFn: () => firstValueFrom(this.sa.getSubscriptions()),
  }));

  get overview() { return this.query.data()?.overview; }
  get subscriptions() { return this.query.data()?.subscriptions ?? []; }
  get plans() { return this.query.data()?.plans ?? []; }
  get revenueHistory() { return this.query.data()?.revenueHistory ?? []; }

  editPlan = signal<any>(null);
  assignOrgId = signal<string | null>(null);
  assignPlanId = signal<string>('');

  openEdit(plan: any) { this.editPlan.set({ ...plan }); }
  closeEdit() { this.editPlan.set(null); }

  updatePlanMut = injectMutation(() => ({
    mutationFn: () => {
      const p = this.editPlan();
      return firstValueFrom(this.sa.updatePlan(p.id, p));
    },
    onSuccess: () => { this.toast.success('Plan updated'); this.qc.invalidateQueries({ queryKey: ['superadmin-subscriptions'] }); this.closeEdit(); },
    onError: (e: any) => this.toast.error(e.error?.error || 'Update failed')
  }));

  assignMut = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.sa.assignPlan(this.assignOrgId()!, this.assignPlanId())),
    onSuccess: () => { this.toast.success('Plan assigned'); this.qc.invalidateQueries({ queryKey: ['superadmin-subscriptions'] }); this.assignOrgId.set(null); },
    onError: (e: any) => this.toast.error(e.error?.error || 'Assign failed')
  }));

  planBadgeClass(plan: string) {
    switch (plan) {
      case 'Free': return 'badge-ghost';
      case 'Pro': return 'badge-primary';
      case 'Business': return 'badge-secondary';
      case 'Enterprise': return 'badge-accent';
      default: return 'badge-ghost';
    }
  }

  revenueChart() {
    const hist = this.revenueHistory;
    if (!hist.length) return null;
    return {
      chart: { type: 'area' as const, height: 200, toolbar: { show: false } },
      series: [{ name: 'Revenue', data: hist }],
      xaxis: { categories: ['Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep'] },
      colors: ['#6366f1'],
      stroke: { curve: 'smooth' as const, width: 2 },
      fill: { type: 'gradient' as const, gradient: { opacityFrom: 0.4, opacityTo: 0 } },
      dataLabels: { enabled: false }
    };
  }
}
