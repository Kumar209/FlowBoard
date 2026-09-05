import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WorkspaceService } from '../../core/services/workspace.service';
import { AuthService } from '../../core/services/auth.service';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';

/**
 * MembersComponent - MNC-grade: OnPush + TanStack queryKey as const + firstValueFrom.
 * Shows members filtered to assigned workspaces (Manager sees his workspace members only, not org-wide).
 * Client/Viewer sees minimal assigned project members.
 */
@Component({
  selector: 'app-members',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './members.component.html',
  styleUrls: ['./members.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MembersComponent {
  auth = inject(AuthService);
  private ws = inject(WorkspaceService);

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.ws.getMyWorkspaces()),
  }));

  hint = signal('Manager sees workspace members only — not whole org. Client/Viewer minimal.');
}
