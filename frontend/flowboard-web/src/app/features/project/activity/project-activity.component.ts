import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { injectQuery } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-project-activity',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './project-activity.component.html',
  styleUrls: ['./project-activity.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectActivityComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  projectId = signal(this.route.parent?.snapshot.paramMap.get('pid') || this.route.snapshot.paramMap.get('pid') || '');
  page = signal(1);
  pageSize = 20;

  activitiesQuery = injectQuery(() => ({
    queryKey: ['activities', this.projectId(), this.page()] as const,
    queryFn: () => firstValueFrom(this.ps.getActivities(this.projectId(), this.page(), this.pageSize)),
    enabled: !!this.projectId(),
  }));

  total = computed(() => this.activitiesQuery.data()?.total || 0);
  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));
}
