import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';

@Component({
  selector: 'app-superadmin-system',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './system.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SystemComponent {
  private sa = inject(SuperAdminService);

  query = injectQuery(() => ({
    queryKey: ['superadmin-system'] as const,
    queryFn: () => firstValueFrom(this.sa.getSystemStatus()),
  }));

  get services() {
    return (this.query.data()?.services ?? []) as any[];
  }

  get checkedAt() {
    return this.query.data()?.checkedAt;
  }

  get healthyCount() {
    return this.query.data()?.healthyCount ?? 0;
  }

  get total() {
    return this.query.data()?.totalServices ?? 0;
  }

  selected = signal<any | null>(null);

  openError(service: any) {
    this.selected.set(service);
  }

  closeError() {
    this.selected.set(null);
  }

  serviceAccentClass(service: string) {
    const value = String(service || '').toLowerCase();

    if (value.includes('gateway')) {
      return {
        icon: 'bg-primary/12 text-primary',
        glow: 'from-primary/12 via-primary/5 to-transparent',
        border: 'hover:border-primary/30',
        metric: 'bg-primary/5 border-primary/10'
      };
    }

    if (value.includes('identity')) {
      return {
        icon: 'bg-secondary/12 text-secondary',
        glow: 'from-secondary/12 via-secondary/5 to-transparent',
        border: 'hover:border-secondary/30',
        metric: 'bg-secondary/5 border-secondary/10'
      };
    }

    if (value.includes('project')) {
      return {
        icon: 'bg-accent/12 text-accent',
        glow: 'from-accent/12 via-accent/5 to-transparent',
        border: 'hover:border-accent/30',
        metric: 'bg-accent/5 border-accent/10'
      };
    }

    if (value.includes('file')) {
      return {
        icon: 'bg-info/12 text-info',
        glow: 'from-info/12 via-info/5 to-transparent',
        border: 'hover:border-info/30',
        metric: 'bg-info/5 border-info/10'
      };
    }

    if (value.includes('notification')) {
      return {
        icon: 'bg-success/12 text-success',
        glow: 'from-success/12 via-success/5 to-transparent',
        border: 'hover:border-success/30',
        metric: 'bg-success/5 border-success/10'
      };
    }

    if (value.includes('sql') || value.includes('database')) {
      return {
        icon: 'bg-warning/12 text-warning',
        glow: 'from-warning/12 via-warning/5 to-transparent',
        border: 'hover:border-warning/30',
        metric: 'bg-warning/5 border-warning/10'
      };
    }

    if (value.includes('redis')) {
      return {
        icon: 'bg-error/12 text-error',
        glow: 'from-error/12 via-error/5 to-transparent',
        border: 'hover:border-error/30',
        metric: 'bg-error/5 border-error/10'
      };
    }

    return {
      icon: 'bg-primary/12 text-primary',
      glow: 'from-primary/10 via-primary/5 to-transparent',
      border: 'hover:border-primary/25',
      metric: 'bg-primary/5 border-primary/10'
    };
  }
}