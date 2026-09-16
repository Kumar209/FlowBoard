import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';
import { ROLE_LABEL_MAP, OrgRoleValues } from '../../../shared/constants/roles';

@Component({
  selector: 'app-superadmin-org-members',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './org-members.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrgMembersComponent {
  private sa = inject(SuperAdminService);
  private route = inject(ActivatedRoute);

  orgId = signal(this.route.snapshot.paramMap.get('orgId') || '');
  search = signal('');
  searchInput = signal('');
  page = signal(1);
  pageSize = signal(10);

  query = injectQuery(() => ({
    queryKey: ['superadmin-org-members', this.orgId(), this.search(), this.page(), this.pageSize()] as const,
    queryFn: () => firstValueFrom(
      this.sa.getOrgMembers(
        this.orgId(),
        this.search() || undefined,
        this.page(),
        this.pageSize()
      )
    ),
    enabled: !!this.orgId(),
  }));

  get items() {
    return this.query.data()?.items ?? [];
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
    const target = Math.min(Math.max(1, value), this.totalPages);
    this.page.set(target);
  }

  firstPage() {
    if (this.page() > 1) {
      this.page.set(1);
    }
  }

  prevPage() {
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
    const size = Number(value) || 10;
    this.pageSize.set(size);
    this.page.set(1);
  }

  onSearch() {
    this.search.set(this.searchInput().trim());
    this.page.set(1);
  }

  clearSearch() {
    this.searchInput.set('');
    this.search.set('');
    this.page.set(1);
  }

  roleBadgeClass(role: string) {
    return role === ROLE_LABEL_MAP[String(OrgRoleValues.OrgAdmin)]
      ? 'badge-primary'
      : role === ROLE_LABEL_MAP[String(OrgRoleValues.Client)]
        ? 'badge-ghost'
        : 'badge-secondary';
  }
}