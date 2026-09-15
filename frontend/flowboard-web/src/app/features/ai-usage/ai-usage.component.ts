import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { AiService } from '../../core/services/ai.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { injectQuery } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-ai-usage',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ai-usage.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AiUsageComponent {
  private ai = inject(AiService);
  private wsService = inject(WorkspaceService);

  modelFilter = signal<string>('all');
  page = signal(1);
  pageSize = signal(10);

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.wsService.getMyWorkspaces()),
    staleTime: 2 * 60 * 1000
  }));

  orgId = computed(() => {
    const ws = this.workspacesQuery.data() as any[];
    if (!ws || !ws.length) return '';
    return ws[0]?.organizationId || ws[0]?.orgId || '';
  });

  usageQuery = injectQuery(() => ({
    queryKey: ['ai-usage-org', this.orgId()] as const,
    queryFn: () => firstValueFrom(this.ai.usage(this.orgId() || undefined, undefined)),
    enabled: !!this.orgId(),
    staleTime: 60 * 1000
  }));

  summaryQuery = injectQuery(() => ({
    queryKey: ['ai-usage-summary-org', this.orgId()] as const,
    queryFn: () => firstValueFrom(this.ai.usageSummary(this.orgId() || undefined, undefined)),
    enabled: !!this.orgId(),
    staleTime: 60 * 1000
  }));

  allUsageLogs = computed(() => {
    const data = this.usageQuery.data() as any;
    if (!Array.isArray(data)) return [];
    return data;
  });

  allSummary = computed(() => {
    const data = this.summaryQuery.data() as any;
    if (!Array.isArray(data)) return [];
    return data;
  });

  availableModels = computed(() => {
    const models = new Set<string>();

    for (const item of this.allSummary()) {
      const model = String(item.model || '').trim();
      if (model) models.add(model);
    }

    for (const item of this.allUsageLogs()) {
      const model = String(item.model || '').trim();
      if (model) models.add(model);
    }

    return Array.from(models).sort((a, b) => a.localeCompare(b));
  });

  filteredSummary = computed(() => {
    const list = this.allSummary();
    const filter = this.modelFilter().trim().toLowerCase();

    if (filter === 'all') return list;

    return list.filter((item: any) =>
      String(item.model || '').trim().toLowerCase() === filter
    );
  });

  filteredLogs = computed(() => {
    const list = this.allUsageLogs();
    const filter = this.modelFilter().trim().toLowerCase();

    if (filter === 'all') return list;

    return list.filter((item: any) =>
      String(item.model || '').trim().toLowerCase() === filter
    );
  });

  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredLogs().length / this.pageSize()))
  );

  paginatedLogs = computed(() => {
    const totalPages = this.totalPages();
    const currentPage = Math.min(this.page(), totalPages);
    const start = (currentPage - 1) * this.pageSize();

    return this.filteredLogs().slice(
      start,
      start + this.pageSize()
    );
  });

  showingFrom = computed(() => {
    const total = this.filteredLogs().length;

    if (!total) return 0;

    return (Math.min(this.page(), this.totalPages()) - 1) * this.pageSize() + 1;
  });

  showingTo = computed(() => {
    const total = this.filteredLogs().length;

    if (!total) return 0;

    return Math.min(
      Math.min(this.page(), this.totalPages()) * this.pageSize(),
      total
    );
  });

  visiblePages = computed(() => {
    const total = this.totalPages();
    const current = Math.min(this.page(), total);

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
  });

  onModelChange(value: string) {
    this.modelFilter.set(value);
    this.page.set(1);
  }

  firstPage() {
    this.page.set(1);
  }

  previousPage() {
    if (this.page() > 1) {
      this.page.set(this.page() - 1);
    }
  }

  nextPage() {
    if (this.page() < this.totalPages()) {
      this.page.set(this.page() + 1);
    }
  }

  lastPage() {
    this.page.set(this.totalPages());
  }

  setPage(page: number) {
    if (page < 1 || page > this.totalPages()) return;
    this.page.set(page);
  }

  onPageSizeChange(value: string) {
    const size = Number(value);

    if (!Number.isFinite(size) || size <= 0) return;

    this.pageSize.set(size);
    this.page.set(1);
  }

  statusClass(status: string) {
    switch (status) {
      case 'Success':
        return 'badge-success';
      case 'RateLimited':
        return 'badge-warning';
      default:
        return 'badge-error';
    }
  }
}