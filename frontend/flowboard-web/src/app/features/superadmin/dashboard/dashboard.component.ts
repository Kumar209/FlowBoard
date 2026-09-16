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
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent {
  private sa = inject(SuperAdminService);

  query = injectQuery(() => ({
    queryKey: ['superadmin-dashboard'] as const,
    queryFn: () => firstValueFrom(this.sa.getDashboard()),
  }));

  get kpis() {
    return this.query.data()?.kpis;
  }

  get health() {
    return this.query.data()?.health ?? [];
  }

  get growth() {
    return this.query.data()?.growth;
  }

  get healthyServices() {
    return this.health.filter((service: any) =>
      String(service.status || '').toLowerCase() === 'healthy'
    ).length;
  }

  get activeUserRate() {
    const total = Number(this.kpis?.totalUsers ?? 0);
    const active = Number(this.kpis?.activeUsers ?? 0);

    if (!total) {
      return 0;
    }

    return Math.round((active / total) * 100);
  }

  get totalLatency() {
    if (!this.health.length) {
      return 0;
    }

    return Math.round(
      this.health.reduce((sum: number, service: any) => {
        return sum + Number(service.latencyMs ?? 0);
      }, 0) / this.health.length
    );
  }

  chartOptions(labels: string[], data: number[], color: string) {
    return {
      chart: {
        type: 'area' as const,
        height: 190,
        sparkline: { enabled: false },
        toolbar: { show: false },
        zoom: { enabled: false }
      },
      series: [
        {
          name: 'Count',
          data
        }
      ],
      xaxis: {
        categories: labels,
        labels: {
          style: {
            fontSize: '9px'
          },
          trim: true,
          hideOverlappingLabels: true
        },
        axisBorder: {
          show: false
        },
        axisTicks: {
          show: false
        }
      },
      yaxis: {
        labels: {
          style: {
            fontSize: '9px'
          }
        }
      },
      colors: [color],
      stroke: {
        curve: 'smooth' as const,
        width: 2
      },
      fill: {
        type: 'gradient' as const,
        gradient: {
          opacityFrom: 0.28,
          opacityTo: 0.02,
          stops: [0, 100]
        }
      },
      grid: {
        borderColor: 'rgba(127,127,127,0.14)',
        strokeDashArray: 4,
        padding: {
          left: 4,
          right: 4,
          top: 0,
          bottom: 0
        }
      },
      markers: {
        size: 0,
        hover: {
          size: 4
        }
      },
      dataLabels: {
        enabled: false
      },
      tooltip: {
        enabled: true,
        theme: 'auto'
      }
    };
  }

  revenueOptions() {
    const g = this.growth;

    if (!g) {
      return null;
    }

    return {
      chart: {
        type: 'bar' as const,
        height: 220,
        toolbar: { show: false },
        zoom: { enabled: false }
      },
      series: [
        {
          name: 'Revenue',
          data: g.revenue
        }
      ],
      xaxis: {
        categories: g.labels,
        labels: {
          style: {
            fontSize: '9px'
          },
          trim: true,
          hideOverlappingLabels: true
        },
        axisBorder: {
          show: false
        },
        axisTicks: {
          show: false
        }
      },
      yaxis: {
        labels: {
          style: {
            fontSize: '9px'
          },
          formatter: (value: number) => `$${value}`
        }
      },
      colors: ['#6366f1'],
      plotOptions: {
        bar: {
          borderRadius: 5,
          columnWidth: '42%'
        }
      },
      grid: {
        borderColor: 'rgba(127,127,127,0.14)',
        strokeDashArray: 4,
        padding: {
          left: 4,
          right: 4,
          top: 0,
          bottom: 0
        }
      },
      dataLabels: {
        enabled: false
      },
      tooltip: {
        enabled: true,
        theme: 'auto'
      }
    };
  }
}