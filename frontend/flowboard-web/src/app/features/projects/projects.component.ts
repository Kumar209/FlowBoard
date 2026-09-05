import { Component, inject, signal, ChangeDetectionStrategy, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../core/services/project.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { AuthService } from '../../core/services/auth.service';
import { injectQuery } from '@tanstack/angular-query-experimental';

/**
 * ProjectsComponent - MNC-grade: global projects directory across all workspaces (Option B).
 * Fetches all workspaces, then all projects per workspace (combine). Filter by workspace via Signal.
 * OnPush + firstValueFrom + queryKey as const + computed filtered.
 */
@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './projects.component.html',
  styleUrls: ['./projects.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectsComponent {
  private projectService = inject(ProjectService);
  private workspaceService = inject(WorkspaceService);
  private auth = inject(AuthService);

  selectedWorkspaceId = signal<string>('all');
  canCreateProject = computed(() => this.auth.canCreateProject());

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyWorkspaces()),
  }));

  projectsQuery = injectQuery(() => ({
    queryKey: ['projects-global', this.selectedWorkspaceId()] as const,
    queryFn: async () => {
      const workspaces = await firstValueFrom(this.workspaceService.getMyWorkspaces());
      const ids = this.selectedWorkspaceId() === 'all' ? workspaces.map(w => w.id) : [this.selectedWorkspaceId()];
      const results = await Promise.all(ids.map(id => firstValueFrom(this.projectService.getProjects(id)).catch(() => ({ items: [], total: 0 } as any))));
      const all = results.flatMap(r => r.items);
      return { items: all, total: all.length };
    },
  }));

  filtered = computed(() => this.projectsQuery.data()?.items || []);
}
