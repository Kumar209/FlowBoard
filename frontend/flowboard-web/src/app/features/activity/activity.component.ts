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
  page = signal(1);
  pageSize = 20;

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyWorkspaces()),
  }));

  // Org-wide: fetch all projects across all workspaces, then all activities
  allProjectsQuery = injectQuery(() => ({
    queryKey: ['org-projects'] as const,
    queryFn: async () => {
      const workspaces = await firstValueFrom(this.workspaceService.getMyWorkspaces());
      const all: any[] = [];
      for (const ws of workspaces) {
        try {
          const res = await firstValueFrom(this.projectService.getProjects(ws.id));
          all.push(...(res.items || []));
        } catch {}
      }
      return all;
    },
    enabled: () => !!this.workspacesQuery.data()?.length,
  }));

  orgActivitiesQuery = injectQuery(() => ({
    queryKey: ['org-activities', this.page()] as const,
    queryFn: async () => {
      const projects = this.allProjectsQuery.data() || [];
      if (!projects.length) return { items: [], total: 0 };
      const allActivities: any[] = [];
      for (const p of projects.slice(0,10)) {
        try {
          const res = await firstValueFrom(this.projectService.getActivities(p.id, 1, 20));
          allActivities.push(...(res.items || []).map((a:any) => ({...a, projectName: p.name, projectKey: p.key})));
        } catch {}
      }
      // Sort by occurredAt desc and paginate
      allActivities.sort((a,b) => new Date(b.occurredAt).getTime() - new Date(a.occurredAt).getTime());
      const start = (this.page()-1)*this.pageSize;
      return { items: allActivities.slice(start, start+this.pageSize), total: allActivities.length };
    },
    enabled: () => !!this.allProjectsQuery.data()?.length,
  }));

  total = computed(() => this.orgActivitiesQuery.data()?.total || 0);
  totalPages = computed(() => Math.max(1, Math.ceil(this.total()/this.pageSize)));
}
