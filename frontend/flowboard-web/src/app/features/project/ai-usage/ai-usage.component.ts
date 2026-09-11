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
  styleUrls: ['./ai-usage.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectAiUsageComponent {
  private route = inject(ActivatedRoute);
  private ai = inject(AiService);

  projectId = signal(this.route.snapshot.paramMap.get('pid') || this.route.parent?.snapshot.paramMap.get('pid') || '');
  modelFilter = signal<string>('all');

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
    const f = this.modelFilter();
    if (f === 'all') return list;
    return list.filter((x: any) => x.model === f || x.provider === f);
  });

  filteredLogs = computed(() => {
    const list = this.usageQuery.data() as any[] | undefined;
    if (!list) return [];
    const f = this.modelFilter();
    if (f === 'all') return list.slice(0, 100);
    return list.filter((x: any) => x.model === f || x.provider === f).slice(0, 100);
  });
}
