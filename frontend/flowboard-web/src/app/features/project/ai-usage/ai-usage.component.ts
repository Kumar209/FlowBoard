import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AiService } from '../../../core/services/ai.service';
import { injectQuery } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-project-ai-usage',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ai-usage.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectAiUsageComponent {
  private route = inject(ActivatedRoute);
  private ai = inject(AiService);

  projectId = signal(this.route.snapshot.paramMap.get('pid') || this.route.parent?.snapshot.paramMap.get('pid') || '');
  modelFilter = signal<string>('all');
  page = signal(1);
  pageSize = signal(10);

  constructor() {
    this.route.paramMap.subscribe(m => {
      const pid = m.get('pid') || this.route.snapshot.paramMap.get('pid') || '';
      if (pid) this.projectId.set(pid);
    });

    this.route.parent?.paramMap.subscribe(m => {
      const pid = m.get('pid');
      if (pid) this.projectId.set(pid);
    });
  }

  usageQuery = injectQuery(() => ({
    queryKey: ['ai-usage-project', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ai.usage(undefined, this.projectId())),
    enabled: !!this.projectId(),
    staleTime: 60 * 1000
  }));

  summaryQuery = injectQuery(() => ({
    queryKey: ['ai-usage-summary-project', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ai.usageSummary(undefined, this.projectId())),
    enabled: !!this.projectId(),
    staleTime: 60 * 1000
  }));

  filteredSummary = computed(() => {
    const list = this.summaryQuery.data() as any[] | undefined;
    if (!list) return [];

    const filter = this.modelFilter();

    if (filter === 'all') return list;

    return list.filter((x: any) => x.model === filter);
  });

  filteredLogs = computed(() => {
    const list = this.usageQuery.data() as any[] | undefined;
    if (!list) return [];

    const filter = this.modelFilter();

    if (filter === 'all') return list;

    return list.filter((x: any) => x.model === filter);
  });

  totalPages = computed(() => Math.max(1, Math.ceil(this.filteredLogs().length / this.pageSize())));

  paginatedLogs = computed(() => {
    const currentPage = Math.min(this.page(), this.totalPages());
    const start = (currentPage - 1) * this.pageSize();
    return this.filteredLogs().slice(start, start + this.pageSize());
  });

  visiblePages = computed(() => {
    const total = this.totalPages();
    const current = this.page();

    if (total <= 3) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    if (current <= 2) return [1, 2, 3];
    if (current >= total - 1) return [total - 2, total - 1, total];

    return [current - 1, current, current + 1];
  });

  showingFrom = computed(() => {
    const total = this.filteredLogs().length;
    if (!total) return 0;
    return (this.page() - 1) * this.pageSize() + 1;
  });

  showingTo = computed(() => {
    const total = this.filteredLogs().length;
    if (!total) return 0;
    return Math.min(this.page() * this.pageSize(), total);
  });

  setPage(page: number) {
    const nextPage = Math.max(1, Math.min(page, this.totalPages()));
    this.page.set(nextPage);
  }

  firstPage() {
    this.setPage(1);
  }

  previousPage() {
    this.setPage(this.page() - 1);
  }

  nextPage() {
    this.setPage(this.page() + 1);
  }

  lastPage() {
    this.setPage(this.totalPages());
  }

  onPageSizeChange(value: string | number) {
    const size = Number(value);
    if (!size) return;

    this.pageSize.set(size);
    this.page.set(1);
  }

  onModelChange(value: string) {
    this.modelFilter.set(value);
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