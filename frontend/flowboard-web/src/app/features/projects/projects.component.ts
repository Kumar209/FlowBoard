import { Component, inject, signal, ChangeDetectionStrategy, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../core/services/project.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { AuthService } from '../../core/services/auth.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

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
  private queryClient = inject(QueryClient);

  selectedWorkspaceId = signal<string>('all');
  canCreateProject = computed(() => this.auth.canCreateProject());

  showCreate = signal(false);
  newName = signal('');
  createWsId = signal<string>('');
  createError = signal<string | null>(null);

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

  createMutation = injectMutation(() => ({
    mutationFn: (vars: { workspaceId: string; name: string }) =>
      firstValueFrom(this.projectService.createProject(vars.workspaceId, vars.name)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['projects-global'] });
      this.queryClient.invalidateQueries({ queryKey: ['projects'] });
      this.showCreate.set(false);
      this.newName.set('');
      this.createError.set(null);
    },
    onError: (err: any) => this.createError.set(err.error?.error || err.error?.message || 'Create failed - need ProjectManager/OrgAdmin'),
  }));

  toggleCreate() {
    this.showCreate.update(v => !v);
    if (this.showCreate() && !this.createWsId() && this.workspacesQuery.data()?.length) {
      this.createWsId.set(this.workspacesQuery.data()![0].id);
    }
  }

  createProject() {
    const name = this.newName().trim();
    if (!name) { this.createError.set('Name required'); return; }
    const wid = this.createWsId() || this.selectedWorkspaceId() !== 'all' ? this.createWsId() || this.selectedWorkspaceId() : this.workspacesQuery.data()?.[0]?.id;
    if (!wid || wid === 'all') { this.createError.set('Select workspace'); return; }
    this.createMutation.mutate({ workspaceId: wid, name });
  }
}
