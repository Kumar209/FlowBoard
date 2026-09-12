import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgApexchartsModule } from 'ng-apexcharts';
import type { ApexOptions } from 'ng-apexcharts';
import type { ProjectChartData } from '../../../core/services/stats.service';

@Component({
  selector: 'app-project-charts',
  standalone: true,
  imports: [CommonModule, NgApexchartsModule],
  templateUrl: './project-charts.component.html',
  styleUrls: ['./project-charts.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectChartsComponent {
  chartData = input.required<ProjectChartData | null>();

  statusDonut = computed<ApexOptions>(() => {
    const d = this.chartData();
    const labels = d?.issuesByStatus.map(x => x.label) ?? [];
    const series = d?.issuesByStatus.map(x => x.value) ?? [];
    return {
      chart: { type: 'donut', height: 300 },
      labels: labels.length ? labels : ['No data'],
      series: series.length ? series : [1],
      colors: ['#6366f1','#06b6d4','#f59e0b','#10b981','#ef4444'],
      legend: { position: 'bottom' },
      dataLabels: { enabled: true },
      plotOptions: { pie: { donut: { size: '58%' } } }
    };
  });

  typeDonut = computed<ApexOptions>(() => {
    const d = this.chartData();
    const labels = d?.issuesByType.map(x => x.label) ?? [];
    const series = d?.issuesByType.map(x => x.value) ?? [];
    return {
      chart: { type: 'donut', height: 300 },
      labels: labels.length ? labels : ['No data'],
      series: series.length ? series : [1],
      colors: ['#8b5cf6','#ec4899','#14b8a6','#f97316'],
      legend: { position: 'bottom' },
      dataLabels: { enabled: true },
      plotOptions: { pie: { donut: { size: '58%' } } }
    };
  });

  priorityBar = computed<ApexOptions>(() => {
    const d = this.chartData();
    const cats = d?.issuesByPriority.map(x => x.label) ?? [];
    const vals = d?.issuesByPriority.map(x => x.value) ?? [];
    return {
      chart: { type: 'bar', height: 300, toolbar: { show: false } },
      series: [{ name: 'Issues', data: vals.length ? vals : [0] }],
      xaxis: { categories: cats.length ? cats : ['No data'] },
      colors: ['#f59e0b'],
      plotOptions: { bar: { borderRadius: 6, columnWidth: '42%' } },
      dataLabels: { enabled: false }
    };
  });

  assigneeBar = computed<ApexOptions>(() => {
    const d = this.chartData();
    const cats = d?.assigneeWorkload.map(x => x.assigneeName) ?? [];
    const vals = d?.assigneeWorkload.map(x => x.issueCount) ?? [];
    return {
      chart: { type: 'bar', height: 300, toolbar: { show: false } },
      series: [{ name: 'Issues', data: vals.length ? vals : [0] }],
      xaxis: { categories: cats.length ? cats : ['No data'], labels: { rotate: -20 } },
      colors: ['#06b6d4'],
      plotOptions: { bar: { borderRadius: 6, columnWidth: '38%' } },
      dataLabels: { enabled: false }
    };
  });

  velocityBar = computed<ApexOptions>(() => {
    const d = this.chartData();
    const cats = d?.sprintVelocity.map(x => x.sprintName) ?? [];
    const total = d?.sprintVelocity.map(x => x.storyPoints) ?? [];
    const completed = d?.sprintVelocity.map(x => x.completedPoints) ?? [];
    return {
      chart: { type: 'bar', height: 300, toolbar: { show: false }, stacked: false },
      series: [
        { name: 'Total SP', data: total.length ? total : [0] },
        { name: 'Completed SP', data: completed.length ? completed : [0] }
      ],
      xaxis: { categories: cats.length ? cats : ['No data'] },
      colors: ['#6366f1','#10b981'],
      plotOptions: { bar: { borderRadius: 6, columnWidth: '42%' } },
      dataLabels: { enabled: false },
      legend: { position: 'top' }
    };
  });
}
