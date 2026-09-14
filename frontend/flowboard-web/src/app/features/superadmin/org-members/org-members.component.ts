import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';

@Component({
  selector: 'app-superadmin-org-members',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './org-members.component.html',
  styleUrls: ['./org-members.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrgMembersComponent {
  private sa = inject(SuperAdminService);
  private route = inject(ActivatedRoute);
  orgId = signal(this.route.snapshot.paramMap.get('orgId') || '');
  search = signal('');
  searchInput = signal('');
  page = signal(1);
  pageSize = 10;

  query = injectQuery(() => ({
    queryKey: ['superadmin-org-members', this.orgId(), this.search(), this.page()] as const,
    queryFn: () => firstValueFrom(this.sa.getOrgMembers(this.orgId(), this.search() || undefined, this.page(), this.pageSize)),
    enabled: !!this.orgId(),
  }));

  get items() { return this.query.data()?.items ?? []; }
  get total() { return this.query.data()?.total ?? 0; }
  get totalPages() { return Math.max(1, Math.ceil(this.total / this.pageSize)); }

  onSearch() { this.search.set(this.searchInput().trim()); this.page.set(1); }
  clearSearch() { this.searchInput.set(''); this.search.set(''); this.page.set(1); }
  nextPage() { if (this.page() < this.totalPages) this.page.update(v => v + 1); }
  prevPage() { if (this.page() > 1) this.page.update(v => v - 1); }

  roleBadgeClass(role: string) {
    return role === 'OrgAdmin' ? 'badge-primary' : role === 'Client' ? 'badge-ghost' : 'badge-secondary';
  }
}
