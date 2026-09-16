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
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ActivityComponent {
  private sa = inject(SuperAdminService);

  search = signal('');
  action = signal('All');
  page = signal(1);
  pageSize = signal(10);

  actions = [
    'All',
    'OrganizationCreated',
    'OrganizationSuspended',
    'OrganizationReactivated',
    'OrganizationDeleted',
    'UserSuspended',
    'UserReactivated',
    'MemberAdded',
    'WorkspaceCreated'
  ];

  query = injectQuery(() => ({
    queryKey: ['superadmin-activities', this.search(), this.action(), this.page(), this.pageSize()] as const,
    queryFn: () => firstValueFrom(
      this.sa.getPlatformActivities(
        this.search() || undefined,
        this.action(),
        this.page(),
        this.pageSize()
      )
    ),
  }));

  get items() {
    return (this.query.data()?.items ?? []) as any[];
  }

  get total() {
    return this.query.data()?.total ?? 0;
  }

  get totalPages() {
    return Math.max(1, Math.ceil(this.total / this.pageSize()));
  }

  get showingFrom() {
    return this.total === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1;
  }

  get showingTo() {
    return Math.min(this.page() * this.pageSize(), this.total);
  }

  onSearch(value: string) {
    this.search.set(value);
    this.page.set(1);
  }

  onAction(value: string) {
    this.action.set(value);
    this.page.set(1);
  }

  visiblePages(): number[] {
    const total = this.totalPages;
    const current = this.page();

    if (total <= 3) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    if (current <= 2) {
      return [1, 2, 3];
    }

    if (current >= total - 1) {
      return [total - 2, total - 1, total];
    }

    return [current - 1, current, current + 1];
  }

  setPage(value: number) {
    const target = Math.min(Math.max(value, 1), this.totalPages);
    this.page.set(target);
  }

  firstPage() {
    if (this.page() > 1) {
      this.page.set(1);
    }
  }

  previousPage() {
    if (this.page() > 1) {
      this.page.update(v => v - 1);
    }
  }

  nextPage() {
    if (this.page() < this.totalPages) {
      this.page.update(v => v + 1);
    }
  }

  lastPage() {
    if (this.page() < this.totalPages) {
      this.page.set(this.totalPages);
    }
  }

  onPageSizeChange(value: any) {
    this.pageSize.set(Number(value) || 10);
    this.page.set(1);
  }
}