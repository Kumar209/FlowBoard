import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ProjectService } from '../../core/services/project.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { injectQuery } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-activity',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './activity.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ActivityComponent {
  auth = inject(AuthService);
  private projectService = inject(ProjectService);
  private workspaceService = inject(WorkspaceService);

  selectedWorkspaceId = signal<string>('');
  page = signal(1);
  pageSize = signal(10);

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getMyWorkspaces())
  }));

  orgId = computed(() => {
    const ws = this.workspacesQuery.data() || [];
    return (ws[0] as any)?.organizationId || (ws[0] as any)?.OrganizationId || '';
  });

  orgMembersQuery = injectQuery(() => ({
    queryKey: ['org-members', this.orgId()] as const,
    queryFn: () => firstValueFrom(this.workspaceService.getOrganizationMembers(this.orgId())),
    enabled: !!this.orgId()
  }));

  getActorDisplay = (actorId: string) => {
    const members = (this.orgMembersQuery.data() as any[]) || [];
    const m = members.find((x: any) => x.userId === actorId);

    if (m) {
      return `${m.fullName} (${m.role})`;
    }

    return actorId?.slice(0, 6) || 'Unknown';
  };

  orgActivitiesQuery = injectQuery(() => ({
    queryKey: ['org-activities', this.orgId(), this.page(), this.pageSize()] as const,
    queryFn: async () => {
      const orgId = this.orgId();

      if (!orgId) {
        return { items: [], total: 0 };
      }

      try {
        const res: any = await firstValueFrom(
          this.workspaceService.getOrganizationActivities(
            orgId,
            this.page(),
            this.pageSize(),
            true
          )
        );

        const items = (res.items || res.Items || []).map((a: any) => ({
          ...a,
          occurredAt: a.occurredOn || a.OccurredOn || a.occurredAt || a.OccurredAt,
          projectName: a.projectName || a.ProjectName || '',
          workspaceName: a.workspaceName || a.WorkspaceName || '',
          callerEmail: a.callerEmail || a.CallerEmail || '',
          customRoleName: a.customRoleName || a.CustomRoleName || '',
          actorName: a.actorName || a.ActorName || ''
        }));

        return {
          items,
          total: res.total ?? res.Total ?? items.length
        };
      } catch {
        return {
          items: [],
          total: 0
        };
      }
    },
    enabled: !!this.orgId()
  }));

  allProjectsQuery = injectQuery(() => ({
    queryKey: ['org-projects-compat'] as const,
    queryFn: async () => [],
    enabled: false
  }));

  sampleMembersQuery = injectQuery(() => ({
    queryKey: ['sample-members-deprecated'] as const,
    queryFn: async () => [],
    enabled: false
  })) as any;

  total = computed(() => this.orgActivitiesQuery.data()?.total || 0);

  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize()))
  );

  showingFrom = computed(() => {
    if (this.total() === 0) return 0;
    return (this.page() - 1) * this.pageSize() + 1;
  });

  showingTo = computed(() => {
    if (this.total() === 0) return 0;
    return Math.min(this.page() * this.pageSize(), this.total());
  });

  onPageSizeChange(value: string) {
    this.pageSize.set(Number(value));
    this.page.set(1);
  }

  firstPage() {
    this.page.set(1);
  }

  previousPage() {
    if (this.page() > 1) {
      this.page.set(this.page() - 1);
    }
  }

  nextPage() {
    if (this.page() < this.totalPages()) {
      this.page.set(this.page() + 1);
    }
  }

  lastPage() {
    this.page.set(this.totalPages());
  }

  actionClass(action: string): string {
    switch (action) {
      case 'TaskCreated':
        return 'badge-success';
      case 'TaskMoved':
        return 'badge-warning';
      case 'MemberAdded':
        return 'badge-info';
      case 'TaskUpdated':
      case 'ProjectUpdated':
      case 'WorkspaceUpdated':
        return 'badge-primary';
      case 'TaskDeleted':
      case 'ProjectDeleted':
      case 'WorkspaceDeleted':
      case 'MemberRemoved':
        return 'badge-error';
      default:
        return 'badge-ghost';
    }
  }

  visiblePages = computed(() => {
  const total = this.totalPages();
  const current = this.page();

  if (total <= 3) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  if (current <= 2) {
    return [1, 2, 3];
  }

  if (current >= total - 1) {
    return [total - 2, total - 1, total];
  }

  return [current - 1, current, current + 1];
});

  actionLabel(action: string): string {
    if (!action) return 'Activity';

    return action
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/_/g, ' ');
  }

  actorName(a: any): string {
    if (a.actorName) return a.actorName;
    if (a.callerName) return a.callerName;

    return this.getActorDisplay(a.actorUserId || a.actorId || '').split(' (')[0];
  }

  actorRole(a: any): string {
    if (a.customRoleName) return a.customRoleName;
    if (a.callerEmail) return a.callerEmail;

    return this.getActorDisplay(a.actorUserId || a.actorId || '')
      .split('(')[1]
      ?.slice(0, -1) || '';
  }

  activityTarget(a: any): string {
    return a.projectName || a.workspaceName || 'Organization';
  }

  activityScope(a: any): string {
    if (a.projectName) return a.workspaceName || 'Workspace';
    if (a.workspaceName) return 'Workspace';
    return 'Organization';
  }
}