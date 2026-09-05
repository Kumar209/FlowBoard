import { Component, inject, ChangeDetectionStrategy, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { WorkspaceService } from '../../core/services/workspace.service';
import { AuthService } from '../../core/services/auth.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

/**
 * WorkspacesComponent - MNC-grade: OnPush + computed canCreateWorkspace (OrgAdmin only show + New Workspace).
 * Manager/Member/Client/Viewer see assigned workspaces only (API filtered) + view-only.
 */
@Component({
  selector: 'app-workspaces',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './workspaces.component.html',
  styleUrls: ['./workspaces.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkspacesComponent {
  private workspaceService = inject(WorkspaceService);
  auth = inject(AuthService);
  private queryClient = inject(QueryClient);

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyWorkspaces()),
  }));

  orgsQuery = injectQuery(() => ({
    queryKey: ['organizations'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyOrganizations()),
  }));

  canCreateWorkspace = computed(() => this.auth.canCreateWorkspace());

  showCreate = signal(false);
  newName = signal('');
  selectedOrgId = signal<string>('');
  createError = signal<string | null>(null);

  createMutation = injectMutation(() => ({
    mutationFn: (vars: { organizationId: string; name: string }) =>
      firstValueFrom(this.workspaceService.createWorkspace(vars.organizationId, vars.name)),
    onSuccess: () => {
      this.queryClient.invalidateQueries({ queryKey: ['workspaces'] });
      this.showCreate.set(false);
      this.newName.set('');
      this.createError.set(null);
    },
    onError: (err: any) => this.createError.set(err.error?.error || err.error?.message || 'Create failed - OrgAdmin only'),
  }));

  toggleCreate() {
    this.showCreate.update(v => !v);
    if (this.showCreate() && this.orgsQuery.data()?.length) {
      this.selectedOrgId.set(this.orgsQuery.data()![0].id);
    }
  }

  create() {
    const name = this.newName().trim();
    if (!name) { this.createError.set('Name required'); return; }
    let orgId = this.selectedOrgId();
    if (!orgId) orgId = this.orgsQuery.data()?.[0]?.id || '';
    if (!orgId) { this.createError.set('No organization — create org first'); return; }
    this.createMutation.mutate({ organizationId: orgId, name });
  }
}
