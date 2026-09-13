import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgApexchartsModule } from 'ng-apexcharts';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';

@Component({
  selector: 'app-superadmin-ai-usage',
  standalone: true,
  imports: [CommonModule, NgApexchartsModule],
  templateUrl: './ai-usage.component.html',
  styleUrls: ['./ai-usage.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AiUsageComponent {
  private sa = inject(SuperAdminService);

  query = injectQuery(() => ({
    queryKey: ['superadmin-ai-usage'] as const,
    queryFn: () => firstValueFrom(this.sa.getAiUsage()),
  }));

  get overview() { return this.query.data()?.overview; }
  get providers() { return this.query.data()?.providers ?? []; }
  get models() { return this.query.data()?.models ?? []; }
  get organizations() { return this.query.data()?.organizations ?? []; }
  get operations() { return this.query.data()?.operations ?? []; }
  get failures() { return this.query.data()?.failures ?? []; }

  providerChart() {
    const prov = this.providers;
    if (!prov.length) return null;
    return {
      chart: { type: 'donut' as const, height: 220 },
      series: prov.map(p => p.requests),
      labels: prov.map(p => p.provider),
      colors: ['#6366f1', '#22c55e', '#f59e0b', '#ef4444'],
      legend: { position: 'bottom' as const },
      dataLabels: { enabled: true }
    };
  }

  operationChart() {
    const ops = this.operations;
    if (!ops.length) return null;
    return {
      chart: { type: 'bar' as const, height: 220, toolbar: { show: false } },
      series: [{ name: 'Requests', data: ops.map(o => o.requests) }],
      xaxis: { categories: ops.map(o => o.operation) },
      colors: ['#06b6d4'],
      plotOptions: { bar: { borderRadius: 6, columnWidth: '45%' } },
      dataLabels: { enabled: false }
    };
  }

  formatCost(v: number) { return '$' + Number(v).toFixed(2); }
}
