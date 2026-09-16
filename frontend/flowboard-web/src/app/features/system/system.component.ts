import { Component, ChangeDetectionStrategy, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { WorkspaceService } from '../../core/services/workspace.service';

@Component({
  selector: 'app-system',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './system.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SystemComponent {
  private ws = inject(WorkspaceService);

  orgId = signal<string>('');

  orgsQuery = injectQuery(() => ({
    queryKey: ['organizations'] as const,
    queryFn: () => firstValueFrom(this.ws.getMyOrganizations())
  }));

  constructor() {
    effect(() => {
      const data: any = this.orgsQuery.data();
      const list = Array.isArray(data) ? data : (data?.items ?? []);
      if (list.length && !this.orgId()) this.orgId.set(list[0].id);
    });
  }

  query = injectQuery(() => ({
    queryKey: ['org-system', this.orgId()] as const,
    queryFn: () => firstValueFrom(this.ws.getSystem(this.orgId())),
    enabled: !!this.orgId()
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

  get healthPercent() {
    return this.total ? Math.round((this.healthyCount / this.total) * 100) : 0;
  }

  get hasIssues() {
    return this.total > 0 && this.healthyCount < this.total;
  }
}