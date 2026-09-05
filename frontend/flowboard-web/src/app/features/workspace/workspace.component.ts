import { Component, inject, signal, ChangeDetectionStrategy, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../core/services/project.service';
import { AuthService } from '../../core/services/auth.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

/**
 * WorkspaceComponent - MNC-grade: OnPush + firstValueFrom + computed canCreateProject per workspace role.
 * Manager sees + New Project, Members/Client/Viewer see view-only banner. Member+Client+Viewer 403 on create is now hidden.
 */
@Component({
  selector: 'app-workspace',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './workspace.component.html',
  styleUrls: ['./workspace.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkspaceComponent {
  private route = inject(ActivatedRoute);
  projectService = inject(ProjectService);
  auth = inject(AuthService);
  private queryClient = inject(QueryClient);

  workspaceId = signal<string>(this.route.snapshot.paramMap.get('wid') || '11111111-1111-1111-1111-111111111111');
  showCreate = signal(false);
  newName = signal('');
  createError = signal<string | null>(null);

  canCreateProject = computed(() => {
    const wid = this.workspaceId();
    return this.auth.isSuperAdmin() || this.auth.isOrgAdmin() || this.auth.isManagerFor(wid) || this.auth.isOrgAdminFor(wid) || this.auth.canCreateProject();
  });
  roleLabel = computed(() => {
    const wid = this.workspaceId();
    const m = this.auth.memberships().find(x => x.workspaceId === wid);
    return m?.roleName ?? (m ? String(m.role) : 'Member');
  });

  projectsQuery = injectQuery(() => ({
    queryKey: ['projects', this.workspaceId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getProjects(this.workspaceId())),
  }));

  createMutation = injectMutation(() => ({
    mutationFn: (vars: { name: string }) => firstValueFrom(this.projectService.createProject(this.workspaceId(), vars.name)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['projects', this.workspaceId()] });
      this.showCreate.set(false);
      this.newName.set('');
      this.createError.set(null);
    },
    onError: (err: any) => this.createError.set(err.error?.error || 'Create failed - need ProjectManager/OrgAdmin'),
  }));

  create() {
    const name = this.newName().trim();
    if (!name) { this.createError.set('Name required'); return; }
    this.createMutation.mutate({ name });
  }
}
