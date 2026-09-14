import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgApexchartsModule } from 'ng-apexcharts';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';

@Component({
  selector: 'app-superadmin-dashboard',
  standalone: true,
  imports: [CommonModule, NgApexchartsModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent {
  private sa = inject(SuperAdminService);

  query = injectQuery(() => ({
    queryKey: ['superadmin-dashboard'] as const,
    queryFn: () => firstValueFrom(this.sa.getDashboard()),
  }));

  get kpis() { return this.query.data()?.kpis; }
  get health() { return this.query.data()?.health ?? []; }
  get growth() { return this.query.data()?.growth; }

  chartOptions(labels: string[], data: number[], color: string) {
    return {
      chart: { type: 'area' as const, height: 120, sparkline: { enabled: false }, toolbar: { show: false } },
      series: [{ name: 'count', data }],
      xaxis: { categories: labels },
      colors: [color],
      stroke: { curve: 'smooth' as const, width: 2 },
      fill: { type: 'gradient' as const, gradient: { opacityFrom: 0.3, opacityTo: 0 } },
      dataLabels: { enabled: false },
      tooltip: { enabled: true }
    };
  }

  revenueOptions() {
    const g = this.growth; if (!g) return null;
    return {
      chart: { type: 'bar' as const, height: 220, toolbar: { show: false } },
      series: [{ name: 'Revenue', data: g.revenue }],
      xaxis: { categories: g.labels },
      colors: ['#6366f1'],
      plotOptions: { bar: { borderRadius: 6, columnWidth: '40%' } },
      dataLabels: { enabled: false }
    };
  }
}
