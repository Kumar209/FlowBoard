import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';

@Component({
  selector: 'app-superadmin-activity',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './activity.component.html',
  styleUrls: ['./activity.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ActivityComponent {
  private sa = inject(SuperAdminService);
  search = signal('');
  action = signal('All');
  page = signal(1);
  pageSize = 10;

  actions = ['All', 'OrganizationCreated', 'OrganizationSuspended', 'OrganizationReactivated', 'OrganizationDeleted', 'UserSuspended', 'UserReactivated', 'MemberAdded', 'WorkspaceCreated'];

  query = injectQuery(() => ({
    queryKey: ['superadmin-activities', this.search(), this.action(), this.page()] as const,
    queryFn: () => firstValueFrom(this.sa.getPlatformActivities(this.search() || undefined, this.action(), this.page(), this.pageSize)),
  }));

  get items() { return (this.query.data()?.items ?? []) as any[]; }
  get total() { return this.query.data()?.total ?? 0; }
  get totalPages() { return Math.max(1, Math.ceil(this.total / this.pageSize)); }

  onSearch(v: string) { this.search.set(v); this.page.set(1); }
  onAction(v: string) { this.action.set(v); this.page.set(1); }
  prev() { if (this.page() > 1) this.page.update(v => v - 1); }
  next() { if (this.page() < this.totalPages) this.page.update(v => v + 1); }
}
