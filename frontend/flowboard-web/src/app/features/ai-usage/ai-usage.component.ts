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

  filteredSummary = computed(() => {
    const list = this.summaryQuery.data() as any[] | undefined;
    if (!list) return [];

    const f = this.modelFilter();
    if (f === 'all') return list;

    return list.filter((x: any) => x.model === f || x.provider === f);
  });

  filteredLogs = computed(() => {
    const list = this.usageQuery.data() as any[] | undefined;
    if (!list) return [];

    const f = this.modelFilter();
    if (f === 'all') return list;

    return list.filter((x: any) => x.model === f || x.provider === f);
  });

  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredLogs().length / this.pageSize()))
  );

  paginatedLogs = computed(() => {
    const start = (this.page() - 1) * this.pageSize();
    return this.filteredLogs().slice(start, start + this.pageSize());
  });

  showingFrom = computed(() => {
    const total = this.filteredLogs().length;
    if (!total) return 0;
    return (this.page() - 1) * this.pageSize() + 1;
  });

  showingTo = computed(() => {
    return Math.min(
      this.page() * this.pageSize(),
      this.filteredLogs().length
    );
  });

  visiblePages = computed(() => {
    const total = this.totalPages();
    const current = this.page();

    if (total <= 3) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    if (current === 1) {
      return [1, 2, 3];
    }

    if (current === total) {
      return [total - 2, total - 1, total];
    }

    return [current - 1, current, current + 1];
  });

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

  onPageSizeChange(value: string) {
    const size = Number(value);
    if (!Number.isFinite(size) || size <= 0) return;

    this.pageSize.set(size);
    this.page.set(1);
  }
}