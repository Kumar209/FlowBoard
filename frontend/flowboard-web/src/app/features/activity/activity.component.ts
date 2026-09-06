import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ProjectService } from '../../core/services/project.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { injectQuery } from '@tanstack/angular-query-experimental';

/**
 * ActivityComponent - MNC-grade: OnPush + TanStack paginated timeline (GET /api/projects/{pid}/activities).
 * Select workspace -> project -> timeline. DaisyUI timeline vertical, responsive.
 */
@Component({
  selector: 'app-activity',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './activity.component.html',
  styleUrls: ['./activity.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ActivityComponent {
  auth = inject(AuthService);
  private projectService = inject(ProjectService);
  private workspaceService = inject(WorkspaceService);

  selectedWorkspaceId = signal<string>('');
  selectedProjectId = signal<string>('');
  page = signal(1);
  pageSize = 20;

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyWorkspaces()),
  }));

  projectsQuery = injectQuery(() => ({
    queryKey: ['activity-projects', this.selectedWorkspaceId()] as const,
    queryFn: async () => {
      const wsId = this.selectedWorkspaceId() || this.workspacesQuery.data()?.[0]?.id;
      if (!wsId) return { items: [], total: 0 };
      const res = await firstValueFrom(this.projectService.getProjects(wsId));
      return res;
    },
    enabled: () => !!this.workspacesQuery.data()?.length,
  }));

  activitiesQuery = injectQuery(() => ({
    queryKey: ['activities', this.selectedProjectId(), this.page()] as const,
    queryFn: () => firstValueFrom(this.projectService.getActivities(this.selectedProjectId(), this.page(), this.pageSize)),
    enabled: () => !!this.selectedProjectId(),
  }));

  total = computed(() => this.activitiesQuery.data()?.total || 0);
  totalPages = computed(() => Math.max(1, Math.ceil(this.total()/this.pageSize)));
}
