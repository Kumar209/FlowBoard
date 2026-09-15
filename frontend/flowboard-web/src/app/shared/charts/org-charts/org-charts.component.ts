import { Component, ChangeDetectionStrategy, input, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgApexchartsModule } from 'ng-apexcharts';
import type { ApexOptions } from 'ng-apexcharts';
import type { OrgChartData } from '../../../core/services/stats.service';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-org-charts',
  standalone: true,
  imports: [CommonModule, NgApexchartsModule],
  templateUrl: './org-charts.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrgChartsComponent {
  private themeService = inject(ThemeService);

  chartData = input.required<OrgChartData | null>();

  private themeColors = computed(() => {
    this.themeService.currentTheme();

    return {
      primary: this.getDaisyColor('--p', '#6366f1'),
      secondary: this.getDaisyColor('--s', '#06b6d4'),
      accent: this.getDaisyColor('--a', '#f59e0b'),
      success: this.getDaisyColor('--su', '#10b981'),
      warning: this.getDaisyColor('--wa', '#f59e0b'),
      error: this.getDaisyColor('--er', '#ef4444'),
      info: this.getDaisyColor('--in', '#06b6d4'),
      baseContent: this.getDaisyColor('--bc', '#64748b'),
      base300: this.getDaisyColor('--b3', '#cbd5e1')
    };
  });

  private getDaisyColor(variable: string, fallback: string): string {
    if (typeof document === 'undefined') {
      return fallback;
    }

    const raw = getComputedStyle(document.documentElement)
      .getPropertyValue(variable)
      .trim();

    if (!raw) {
      return fallback;
    }

    let colorValue = raw;

    // DaisyUI 5 commonly exposes colors as OKLCH components:
    // 62.45% 0.278 3.83636
    if (/^-?\d+(\.\d+)?%\s+-?\d+(\.\d+)?\s+-?\d+(\.\d+)?$/.test(raw)) {
      colorValue = `oklch(${raw})`;
    }
    // Support HSL component values as a fallback:
    // 259 94% 51%
    else if (/^-?\d+(\.\d+)?\s+-?\d+(\.\d+)?%\s+-?\d+(\.\d+)?%$/.test(raw)) {
      colorValue = `hsl(${raw})`;
    }

    const probe = document.createElement('span');

    probe.style.position = 'absolute';
    probe.style.width = '0';
    probe.style.height = '0';
    probe.style.opacity = '0';
    probe.style.pointerEvents = 'none';
    probe.style.color = colorValue;

    document.body.appendChild(probe);

    const resolved = getComputedStyle(probe).color;

    probe.remove();

    if (resolved && resolved !== 'rgba(0, 0, 0, 0)') {
      return resolved;
    }

    return fallback;
  }

  private getTooltipTheme(): 'light' | 'dark' {
    const theme = this.themeService.currentTheme();

    const lightThemes = [
      'light',
      'cupcake',
      'bumblebee',
      'emerald',
      'corporate',
      'retro',
      'garden',
      'aqua',
      'lofi',
      'pastel',
      'fantasy',
      'wireframe',
      'cmyk',
      'autumn',
      'acid',
      'lemonade',
      'winter',
      'nord'
    ];

    return lightThemes.includes(theme) ? 'light' : 'dark';
  }

  private baseChartOptions(): Partial<ApexOptions> {
    const colors = this.themeColors();

    return {
      grid: {
        borderColor: colors.base300,
        strokeDashArray: 4
      },
      tooltip: {
        theme: this.getTooltipTheme()
      }
    };
  }

  statusDonut = computed<ApexOptions>(() => {
    const d = this.chartData();
    const colors = this.themeColors();

    const labels = d?.issuesByStatus?.map(x => x.label) ?? [];
    const series = d?.issuesByStatus?.map(x => x.value) ?? [];

    return {
      ...this.baseChartOptions(),
      chart: {
        type: 'donut',
        height: 300,
        toolbar: {
          show: false
        }
      },
      labels: labels.length ? labels : ['No data'],
      series: series.length ? series : [1],
      colors: [
        colors.primary,
        colors.secondary,
        colors.warning,
        colors.success,
        colors.error,
        colors.accent
      ],
      legend: {
        position: 'bottom',
        fontSize: '12px',
        fontWeight: 500,
        labels: {
          colors: colors.baseContent
        },
        itemMargin: {
          horizontal: 8,
          vertical: 4
        }
      },
      dataLabels: {
        enabled: true,
        style: {
          fontSize: '11px',
          fontWeight: 700
        },
        dropShadow: {
          enabled: false
        }
      },
      stroke: {
        width: 2,
        colors: ['transparent']
      },
      plotOptions: {
        pie: {
          expandOnClick: true,
          donut: {
            size: '62%',
            labels: {
              show: true,
              name: {
                show: true,
                fontSize: '11px',
                fontWeight: 600,
                color: colors.baseContent
              },
              value: {
                show: true,
                fontSize: '22px',
                fontWeight: 800,
                color: colors.baseContent
              },
              total: {
                show: true,
                showAlways: true,
                label: 'Total',
                fontSize: '11px',
                fontWeight: 600,
                color: colors.baseContent
              }
            }
          }
        }
      },
      tooltip: {
        theme: this.getTooltipTheme()
      },
      responsive: [
        {
          breakpoint: 640,
          options: {
            chart: {
              height: 280
            },
            legend: {
              fontSize: '11px'
            }
          }
        }
      ]
    };
  });

  priorityBar = computed<ApexOptions>(() => {
    const d = this.chartData();
    const colors = this.themeColors();

    const cats = d?.issuesByPriority?.map(x => x.label) ?? [];
    const vals = d?.issuesByPriority?.map(x => x.value) ?? [];

    return {
      ...this.baseChartOptions(),
      chart: {
        type: 'bar',
        height: 300,
        toolbar: {
          show: false
        }
      },
      series: [
        {
          name: 'Issues',
          data: vals.length ? vals : [0]
        }
      ],
      xaxis: {
        categories: cats.length ? cats : ['No data'],
        labels: {
          style: {
            colors: colors.baseContent,
            fontSize: '11px',
            fontWeight: 500
          }
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
            colors: colors.baseContent,
            fontSize: '10px'
          }
        }
      },
      colors: [colors.warning],
      plotOptions: {
        bar: {
          borderRadius: 7,
          columnWidth: '44%'
        }
      },
      dataLabels: {
        enabled: false
      },
      grid: {
        borderColor: colors.base300,
        strokeDashArray: 4,
        padding: {
          left: 8,
          right: 8
        }
      },
      tooltip: {
        theme: this.getTooltipTheme()
      },
      responsive: [
        {
          breakpoint: 640,
          options: {
            chart: {
              height: 280
            }
          }
        }
      ]
    };
  });

  workspaceBar = computed<ApexOptions>(() => {
    const d = this.chartData();
    const colors = this.themeColors();

    const cats = d?.issuesByWorkspace?.map(x => x.label) ?? [];
    const vals = d?.issuesByWorkspace?.map(x => x.value) ?? [];

    return {
      ...this.baseChartOptions(),
      chart: {
        type: 'bar',
        height: 300,
        toolbar: {
          show: false
        }
      },
      series: [
        {
          name: 'Issues',
          data: vals.length ? vals : [0]
        }
      ],
      xaxis: {
        categories: cats.length ? cats : ['No data'],
        labels: {
          rotate: -30,
          style: {
            colors: colors.baseContent,
            fontSize: '10px',
            fontWeight: 500
          }
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
            colors: colors.baseContent,
            fontSize: '10px'
          }
        }
      },
      colors: [colors.primary],
      plotOptions: {
        bar: {
          borderRadius: 7,
          horizontal: false,
          columnWidth: '42%'
        }
      },
      dataLabels: {
        enabled: false
      },
      grid: {
        borderColor: colors.base300,
        strokeDashArray: 4,
        padding: {
          left: 8,
          right: 8
        }
      },
      tooltip: {
        theme: this.getTooltipTheme()
      },
      responsive: [
        {
          breakpoint: 640,
          options: {
            chart: {
              height: 280
            },
            xaxis: {
              labels: {
                rotate: -45,
                style: {
                  colors: colors.baseContent,
                  fontSize: '9px'
                }
              }
            }
          }
        }
      ]
    };
  });

  activityLine = computed<ApexOptions>(() => {
    const d = this.chartData();
    const colors = this.themeColors();

    const cats = d?.activityTrend?.map(x => x.date.slice(5)) ?? [];
    const vals = d?.activityTrend?.map(x => x.count) ?? [];

    return {
      ...this.baseChartOptions(),
      chart: {
        type: 'line',
        height: 300,
        toolbar: {
          show: false
        },
        zoom: {
          enabled: false
        }
      },
      series: [
        {
          name: 'Activities',
          data: vals.length ? vals : [0]
        }
      ],
      xaxis: {
        categories: cats.length ? cats : ['No data'],
        labels: {
          style: {
            colors: colors.baseContent,
            fontSize: '10px',
            fontWeight: 500
          }
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
            colors: colors.baseContent,
            fontSize: '10px'
          }
        }
      },
      stroke: {
        curve: 'smooth',
        width: 3
      },
      colors: [colors.success],
      markers: {
        size: 3,
        strokeWidth: 2,
        hover: {
          size: 5
        }
      },
      grid: {
        borderColor: colors.base300,
        strokeDashArray: 4,
        padding: {
          left: 8,
          right: 8
        }
      },
      tooltip: {
        theme: this.getTooltipTheme()
      },
      responsive: [
        {
          breakpoint: 640,
          options: {
            chart: {
              height: 280
            }
          }
        }
      ]
    };
  });

  aiUsageLine = computed<ApexOptions>(() => {
    const d = this.chartData();
    const colors = this.themeColors();

    const cats = d?.aiUsage?.map(x => x.date.slice(5)) ?? [];
    const tokens = d?.aiUsage?.map(x => x.tokens) ?? [];

    return {
      ...this.baseChartOptions(),
      chart: {
        type: 'area',
        height: 300,
        toolbar: {
          show: false
        },
        zoom: {
          enabled: false
        }
      },
      series: [
        {
          name: 'Tokens',
          data: tokens.length ? tokens : [0]
        }
      ],
      xaxis: {
        categories: cats.length ? cats : ['No data'],
        labels: {
          style: {
            colors: colors.baseContent,
            fontSize: '10px'
          }
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
            colors: colors.baseContent,
            fontSize: '10px'
          }
        }
      },
      stroke: {
        curve: 'smooth',
        width: 3
      },
      colors: [colors.secondary],
      markers: {
        size: 2,
        hover: {
          size: 5
        }
      },
      fill: {
        type: 'gradient',
        gradient: {
          shadeIntensity: 1,
          opacityFrom: 0.35,
          opacityTo: 0.04,
          stops: [0, 90, 100]
        }
      },
      dataLabels: {
        enabled: false
      },
      grid: {
        borderColor: colors.base300,
        strokeDashArray: 4,
        padding: {
          left: 8,
          right: 8
        }
      },
      tooltip: {
        theme: this.getTooltipTheme()
      },
      responsive: [
        {
          breakpoint: 640,
          options: {
            chart: {
              height: 280
            }
          }
        }
      ]
    };
  });
}