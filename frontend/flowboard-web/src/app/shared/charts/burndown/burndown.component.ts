import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgApexchartsModule } from 'ng-apexcharts';
import type { ApexOptions } from 'ng-apexcharts';
import type { BurndownData } from '../../../core/services/stats.service';

@Component({
  selector: 'app-burndown',
  standalone: true,
  imports: [CommonModule, NgApexchartsModule],
  templateUrl: './burndown.component.html',
  styleUrls: ['./burndown.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BurndownComponent {
  data = input.required<BurndownData | null>();

  options = computed<ApexOptions>(() => {
    const d = this.data();
    const cats = d?.points.map(p => p.date.slice(5)) ?? [];
    const remaining = d?.points.map(p => p.remaining) ?? [];
    const ideal = d?.points.map(p => p.ideal) ?? [];
    const total = d?.points[0]?.total ?? 0;
    return {
      chart: { type: 'area', height: 320, toolbar: { show: false } },
      series: [
        { name: 'Remaining', data: remaining.length ? remaining : [0] },
        { name: 'Ideal', data: ideal.length ? ideal : [0] }
      ],
      xaxis: { categories: cats.length ? cats : ['No data'] },
      yaxis: { min: 0, max: total || undefined },
      stroke: { curve: 'smooth', width: 2, dashArray: [0, 6] },
      colors: ['#6366f1', '#94a3b8'],
      fill: { type: 'gradient', gradient: { opacityFrom: 0.45, opacityTo: 0.05 } },
      dataLabels: { enabled: false },
      legend: { position: 'top' },
      grid: { borderColor: '#e5e7eb' }
    };
  });
}
