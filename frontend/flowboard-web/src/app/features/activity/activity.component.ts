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
  pageSize = 10;

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyWorkspaces()),
  }));

  orgId = computed(() => {
    const ws = this.workspacesQuery.data() || [];
    return (ws[0] as any)?.organizationId || (ws[0] as any)?.OrganizationId || '';
  });

  orgMembersQuery = injectQuery(() => ({
    queryKey: ['org-members', this.orgId()] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getOrganizationMembers(this.orgId())),
    enabled: !!this.orgId(),
  }));

  getActorDisplay = (actorId: string) => {
    const members = (this.orgMembersQuery.data() as any[]) || [];
    const m = members.find((x:any) => x.userId === actorId);
    if (m) return `${m.fullName} (${m.role})`;
    return actorId.slice(0,6);
  };

  orgActivitiesQuery = injectQuery(() => ({
    queryKey: ['org-activities', this.orgId(), this.page()] as const,
    queryFn: async () => {
      const orgId = this.orgId();
      if (!orgId) return { items: [], total: 0 };
      try {
        const res: any = await firstValueFrom(this.workspaceService.getOrganizationActivities(orgId, this.page(), this.pageSize, true));
        const items = (res.items || res.Items || []).map((a:any) => ({
          ...a,
          occurredAt: a.occurredOn || a.OccurredOn || a.occurredAt || a.OccurredAt,
          projectName: a.projectName || a.ProjectName || '',
          workspaceName: a.workspaceName || a.WorkspaceName || '',
          callerEmail: a.callerEmail || a.CallerEmail || '',
          customRoleName: a.customRoleName || a.CustomRoleName || '',
          actorName: a.actorName || a.ActorName || ''
        }));
        return { items, total: res.total ?? res.Total ?? items.length };
      } catch {
        return { items: [], total: 0 };
      }
    },
    enabled: !!this.orgId(),
  }));

  // Keep for template compatibility (shows projects count)
  allProjectsQuery = injectQuery(() => ({
    queryKey: ['org-projects-compat'] as const,
    queryFn: async () => [],
    enabled: false,
  }));
  sampleMembersQuery = injectQuery(() => ({
    queryKey: ['sample-members-deprecated'] as const,
    queryFn: async () => [],
    enabled: false,
  })) as any;

  total = computed(() => this.orgActivitiesQuery.data()?.total || 0);
  totalPages = computed(() => Math.max(1, Math.ceil(this.total()/this.pageSize)));
}
