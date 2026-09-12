import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgApexchartsModule } from 'ng-apexcharts';
import type { ApexOptions } from 'ng-apexcharts';
import type { OrgChartData } from '../../../core/services/stats.service';

@Component({
  selector: 'app-org-charts',
  standalone: true,
  imports: [CommonModule, NgApexchartsModule],
  templateUrl: './org-charts.component.html',
  styleUrls: ['./org-charts.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrgChartsComponent {
  chartData = input.required<OrgChartData | null>();

  statusDonut = computed<ApexOptions>(() => {
    const d = this.chartData();
    const labels = d?.issuesByStatus?.map(x => x.label) ?? [];
    const series = d?.issuesByStatus?.map(x => x.value) ?? [];
    return {
      chart: { type: 'donut', height: 300 },
      labels: labels.length ? labels : ['No data'],
      series: series.length ? series : [1],
      colors: ['#6366f1','#06b6d4','#f59e0b','#10b981','#ef4444','#8b5cf6'],
      legend: { position: 'bottom' },
      dataLabels: { enabled: true },
      plotOptions: { pie: { donut: { size: '58%' } } }
    };
  });

  priorityBar = computed<ApexOptions>(() => {
    const d = this.chartData();
    const cats = d?.issuesByPriority?.map(x => x.label) ?? [];
    const vals = d?.issuesByPriority?.map(x => x.value) ?? [];
    return {
      chart: { type: 'bar', height: 300, toolbar: { show: false } },
      series: [{ name: 'Issues', data: vals.length ? vals : [0] }],
      xaxis: { categories: cats.length ? cats : ['No data'] },
      colors: ['#f59e0b'],
      plotOptions: { bar: { borderRadius: 6, columnWidth: '42%' } },
      dataLabels: { enabled: false }
    };
  });

  workspaceBar = computed<ApexOptions>(() => {
    const d = this.chartData();
    const cats = d?.issuesByWorkspace?.map(x => x.label) ?? [];
    const vals = d?.issuesByWorkspace?.map(x => x.value) ?? [];
    return {
      chart: { type: 'bar', height: 300, toolbar: { show: false } },
      series: [{ name: 'Issues', data: vals.length ? vals : [0] }],
      xaxis: { categories: cats.length ? cats : ['No data'], labels: { rotate: -30 } },
      colors: ['#6366f1'],
      plotOptions: { bar: { borderRadius: 6, horizontal: false } },
      dataLabels: { enabled: false }
    };
  });

  activityLine = computed<ApexOptions>(() => {
    const d = this.chartData();
    const cats = d?.activityTrend?.map(x => x.date.slice(5)) ?? [];
    const vals = d?.activityTrend?.map(x => x.count) ?? [];
    return {
      chart: { type: 'line', height: 300, toolbar: { show: false } },
      series: [{ name: 'Activities', data: vals.length ? vals : [0] }],
      xaxis: { categories: cats.length ? cats : ['No data'] },
      stroke: { curve: 'smooth', width: 2 },
      colors: ['#10b981'],
      markers: { size: 3 },
      grid: { borderColor: '#e5e7eb' }
    };
  });

  aiUsageLine = computed<ApexOptions>(() => {
    const d = this.chartData();
    const cats = d?.aiUsage?.map(x => x.date.slice(5)) ?? [];
    const tokens = d?.aiUsage?.map(x => x.tokens) ?? [];
    return {
      chart: { type: 'area', height: 300, toolbar: { show: false } },
      series: [{ name: 'Tokens', data: tokens.length ? tokens : [0] }],
      xaxis: { categories: cats.length ? cats : ['No data'] },
      stroke: { curve: 'smooth', width: 2 },
      colors: ['#8b5cf6'],
      fill: { type: 'gradient', gradient: { opacityFrom: 0.45, opacityTo: 0.05 } },
      dataLabels: { enabled: false }
    };
  });
}
