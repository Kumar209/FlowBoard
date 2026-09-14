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
  styleUrls: ['./system.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SystemComponent {
  private sa = inject(SuperAdminService);

  query = injectQuery(() => ({
    queryKey: ['superadmin-system'] as const,
    queryFn: () => firstValueFrom(this.sa.getSystemStatus()),
  }));

  get services() { return (this.query.data()?.services ?? []) as any[]; }
  get checkedAt() { return this.query.data()?.checkedAt; }
  get healthyCount() { return this.query.data()?.healthyCount ?? 0; }
  get total() { return this.query.data()?.totalServices ?? 0; }

  selected = signal<any | null>(null);
  openError(s: any) { this.selected.set(s); }
  closeError() { this.selected.set(null); }
}
