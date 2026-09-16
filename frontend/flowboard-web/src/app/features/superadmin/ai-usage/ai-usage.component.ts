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
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AiUsageComponent {
  private sa = inject(SuperAdminService);

  query = injectQuery(() => ({
    queryKey: ['superadmin-ai-usage'] as const,
    queryFn: () => firstValueFrom(this.sa.getAiUsage()),
  }));

  get overview() {
    return this.query.data()?.overview;
  }

  get providers() {
    return this.query.data()?.providers ?? [];
  }

  get models() {
    return this.query.data()?.models ?? [];
  }

  get organizations() {
    return this.query.data()?.organizations ?? [];
  }

  get operations() {
    return this.query.data()?.operations ?? [];
  }

  get failures() {
    return this.query.data()?.failures ?? [];
  }

  providerChart() {
    const prov = this.providers;
    if (!prov.length) return null;

    return {
      chart: {
        type: 'donut' as const,
        height: 250,
        toolbar: { show: false }
      },
      series: prov.map(p => p.requests),
      labels: prov.map(p => p.provider),
      colors: ['#6366f1', '#06b6d4', '#22c55e', '#f59e0b'],
      legend: {
        position: 'bottom' as const,
        fontSize: '12px',
        labels: {
          colors: 'var(--fallback-bc, currentColor)'
        }
      },
      dataLabels: {
        enabled: true
      },
      stroke: {
        width: 3
      },
      plotOptions: {
        pie: {
          donut: {
            size: '68%',
            labels: {
              show: true,
              total: {
                show: true,
                label: 'Requests',
                formatter: () => String(
                  prov.reduce((sum, item) => sum + Number(item.requests || 0), 0)
                )
              }
            }
          }
        }
      }
    };
  }

  operationChart() {
    const ops = this.operations;
    if (!ops.length) return null;

    return {
      chart: {
        type: 'bar' as const,
        height: 250,
        toolbar: { show: false }
      },
      series: [{
        name: 'Requests',
        data: ops.map(o => o.requests)
      }],
      xaxis: {
        categories: ops.map(o => o.operation),
        labels: {
          style: {
            colors: ops.map(() => 'var(--fallback-bc, currentColor)'),
            fontSize: '11px'
          }
        }
      },
      yaxis: {
        labels: {
          style: {
            colors: ['var(--fallback-bc, currentColor)'],
            fontSize: '11px'
          }
        }
      },
      colors: ['#8b5cf6'],
      plotOptions: {
        bar: {
          borderRadius: 7,
          columnWidth: '45%',
          distributed: true
        }
      },
      dataLabels: {
        enabled: false
      },
      grid: {
        borderColor: 'rgba(127,127,127,0.16)',
        strokeDashArray: 4
      }
    };
  }

  formatCost(v: number) {
    return '$' + Number(v).toFixed(2);
  }
}