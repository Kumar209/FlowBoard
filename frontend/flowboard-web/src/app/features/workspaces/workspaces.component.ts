import { Component, inject, ChangeDetectionStrategy, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { WorkspaceService } from '../../core/services/workspace.service';
import { AuthService } from '../../core/services/auth.service';
import { injectQuery } from '@tanstack/angular-query-experimental';

/**
 * WorkspacesComponent - MNC-grade: OnPush + computed canCreateWorkspace (OrgAdmin only hide).
 * Manager/Member/Client/Viewer see assigned workspaces only (API filtered) + view-only, no Create button.
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

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyWorkspaces()),
  }));

  canCreateWorkspace = computed(() => this.auth.canCreateWorkspace());
}
