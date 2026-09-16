import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
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

  get overview() {
    return this.query.data()?.overview;
  }

  get subscriptions() {
    return this.query.data()?.subscriptions ?? [];
  }

  get plans() {
    return this.query.data()?.plans ?? [];
  }

  get revenueHistory() {
    return this.query.data()?.revenueHistory ?? [];
  }

  subscriptionPage = signal(1);
  subscriptionPageSize = signal(10);

  get subscriptionTotal() {
    return this.subscriptions.length;
  }

  get subscriptionTotalPages() {
    return Math.max(1, Math.ceil(this.subscriptionTotal / this.subscriptionPageSize()));
  }

  get subscriptionShowingFrom() {
    return this.subscriptionTotal === 0
      ? 0
      : (this.subscriptionPage() - 1) * this.subscriptionPageSize() + 1;
  }

  get subscriptionShowingTo() {
    return Math.min(
      this.subscriptionPage() * this.subscriptionPageSize(),
      this.subscriptionTotal
    );
  }

  paginatedSubscriptions = computed(() => {
    const start = (this.subscriptionPage() - 1) * this.subscriptionPageSize();
    return this.subscriptions.slice(start, start + this.subscriptionPageSize());
  });

  visibleSubscriptionPages(): number[] {
    const total = this.subscriptionTotalPages;
    const current = this.subscriptionPage();

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

  setSubscriptionPage(page: number) {
    const target = Math.min(Math.max(page, 1), this.subscriptionTotalPages);
    this.subscriptionPage.set(target);
  }

  firstSubscriptionPage() {
    if (this.subscriptionPage() > 1) {
      this.subscriptionPage.set(1);
    }
  }

  previousSubscriptionPage() {
    if (this.subscriptionPage() > 1) {
      this.subscriptionPage.update(v => v - 1);
    }
  }

  nextSubscriptionPage() {
    if (this.subscriptionPage() < this.subscriptionTotalPages) {
      this.subscriptionPage.update(v => v + 1);
    }
  }

  lastSubscriptionPage() {
    if (this.subscriptionPage() < this.subscriptionTotalPages) {
      this.subscriptionPage.set(this.subscriptionTotalPages);
    }
  }

  onSubscriptionPageSizeChange(value: any) {
    const size = Number(value) || 10;
    this.subscriptionPageSize.set(size);
    this.subscriptionPage.set(1);
  }

  editPlan = signal<any>(null);
  assignOrgId = signal<string | null>(null);
  assignPlanId = signal<string>('');

  openEdit(plan: any) {
    this.editPlan.set({ ...plan });
  }

  closeEdit() {
    this.editPlan.set(null);
  }

  updatePlanMut = injectMutation(() => ({
    mutationFn: () => {
      const p = this.editPlan();
      return firstValueFrom(this.sa.updatePlan(p.id, p));
    },
    onSuccess: () => {
      this.toast.success('Plan updated');
      this.qc.invalidateQueries({ queryKey: ['superadmin-subscriptions'] });
      this.closeEdit();
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Update failed')
  }));

  assignMut = injectMutation(() => ({
    mutationFn: () => firstValueFrom(
      this.sa.assignPlan(this.assignOrgId()!, this.assignPlanId())
    ),
    onSuccess: () => {
      this.toast.success('Plan assigned');
      this.qc.invalidateQueries({ queryKey: ['superadmin-subscriptions'] });
      this.assignOrgId.set(null);
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Assign failed')
  }));

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

  revenueChart() {
    const hist = this.revenueHistory;
    if (!hist.length) return null;

    return {
      chart: {
        type: 'area' as const,
        height: 200,
        toolbar: { show: false }
      },
      series: [{ name: 'Revenue', data: hist }],
      xaxis: {
        categories: ['Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep']
      },
      colors: ['#6366f1'],
      stroke: {
        curve: 'smooth' as const,
        width: 2
      },
      fill: {
        type: 'gradient' as const,
        gradient: {
          opacityFrom: 0.4,
          opacityTo: 0
        }
      },
      dataLabels: {
        enabled: false
      }
    };
  }
}