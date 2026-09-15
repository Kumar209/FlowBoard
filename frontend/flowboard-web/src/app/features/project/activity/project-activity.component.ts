import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { injectQuery } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-project-activity',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './project-activity.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectActivityComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  private wsService = inject(WorkspaceService);

  projectId = signal(
    this.route.parent?.snapshot.paramMap.get('pid') ||
    this.route.snapshot.paramMap.get('pid') ||
    ''
  );

  workspaceId = signal(
    this.route.parent?.snapshot.paramMap.get('wid') ||
    this.route.snapshot.paramMap.get('wid') ||
    ''
  );

  page = signal(1);
  pageSize = signal(10);

  workspaceIdResolved = computed(
    () =>
      this.workspaceId() ||
      this.route.snapshot.paramMap.get('wid') ||
      ''
  );

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () =>
      firstValueFrom(
        this.wsService.getMyWorkspaces()
      )
  }));

  workspaceName = computed(() => {
    const wid = this.workspaceIdResolved();
    const list: any[] =
      (this.workspacesQuery.data() as any) || [];

    const workspace = list.find(
      (x: any) => (x.id || x.Id) === wid
    );

    return workspace
      ? (workspace.name || workspace.Name)
      : (wid ? wid.slice(0, 6) : 'Workspace');
  });

  membersQuery = injectQuery(() => ({
    queryKey: [
      'workspace-members',
      this.workspaceIdResolved()
    ] as const,
    queryFn: () =>
      firstValueFrom(
        this.ps.getWorkspaceMembers(
          this.workspaceIdResolved()
        )
      ),
    enabled: !!this.workspaceIdResolved()
  }));

  activitiesQuery = injectQuery(() => ({
    queryKey: [
      'activities',
      this.projectId(),
      this.page(),
      this.pageSize()
    ] as const,
    queryFn: async () => {
      const res: any = await firstValueFrom(
        this.ps.getActivities(
          this.projectId(),
          this.page(),
          this.pageSize()
        )
      );

      const items = (
        res.items ||
        res.Items ||
        []
      ).map((a: any) => ({
        ...a,
        workspaceId:
          a.workspaceId ||
          a.WorkspaceId ||
          null,
        occurredAt:
          a.occurredAt ||
          a.OccurredAt
      }));

      return {
        items,
        total:
          res.total ??
          res.Total ??
          items.length
      };
    },
    enabled: !!this.projectId()
  }));

  total = computed(
    () => this.activitiesQuery.data()?.total || 0
  );

  totalPages = computed(() =>
    Math.max(
      1,
      Math.ceil(
        this.total() / this.pageSize()
      )
    )
  );

  visiblePages = computed(() => {
    const total = this.totalPages();
    const current = this.page();

    if (total <= 3) {
      return Array.from(
        { length: total },
        (_, i) => i + 1
      );
    }

    if (current <= 2) {
      return [1, 2, 3];
    }

    if (current >= total - 1) {
      return [total - 2, total - 1, total];
    }

    return [current - 1, current, current + 1];
  });

  showingFrom = computed(() => {
    if (this.total() === 0) return 0;

    return (
      (this.page() - 1) *
        this.pageSize() +
      1
    );
  });

  showingTo = computed(() =>
    Math.min(
      this.page() * this.pageSize(),
      this.total()
    )
  );

  getActorDisplay = (actorId: string) => {
    const members =
      this.membersQuery.data() || [];

    const member = members.find(
      (x: any) => x.userId === actorId
    );

    if (member) {
      return `${member.fullName} (${member.role})`;
    }

    return actorId?.slice(0, 6) || 'Unknown';
  };

  actorName = (actorId: string) =>
    this.getActorDisplay(actorId)
      .split(' (')[0];

  actorRole = (actorId: string) =>
    this.getActorDisplay(actorId)
      .split('(')[1]
      ?.slice(0, -1) || '';

  actionLabel = (action: string) => {
    const labels: Record<string, string> = {
      TaskCreated: 'Task created',
      TaskMoved: 'Task moved',
      TaskUpdated: 'Task updated',
      BoardCreated: 'Board created',
      BoardUpdated: 'Board updated',
      SprintCreated: 'Sprint created',
      SprintUpdated: 'Sprint updated',
      SprintDeleted: 'Sprint deleted',
      ProjectCreated: 'Project created',
      ProjectUpdated: 'Project updated',
      TeamCreated: 'Team created',
      TeamUpdated: 'Team updated',
      TeamDeleted: 'Team deleted'
    };

    return labels[action] || action;
  };

  actionClass = (action: string) => {
    if (
      action === 'TaskCreated' ||
      action === 'ProjectCreated' ||
      action === 'TeamCreated' ||
      action === 'SprintCreated'
    ) {
      return 'bg-success/10 text-success border border-success/20';
    }

    if (
      action === 'TaskMoved' ||
      action === 'SprintUpdated'
    ) {
      return 'bg-warning/10 text-warning border border-warning/20';
    }

    if (
      action === 'TaskUpdated' ||
      action === 'ProjectUpdated' ||
      action === 'TeamUpdated' ||
      action === 'BoardUpdated'
    ) {
      return 'bg-info/10 text-info border border-info/20';
    }

    if (
      action === 'TeamDeleted' ||
      action === 'SprintDeleted'
    ) {
      return 'bg-error/10 text-error border border-error/20';
    }

    return 'bg-base-200 text-base-content/70 border border-base-300';
  };

  firstPage() {
    if (this.page() > 1) {
      this.page.set(1);
    }
  }

  previousPage() {
    if (this.page() > 1) {
      this.page.update(p => p - 1);
    }
  }

  nextPage() {
    if (this.page() < this.totalPages()) {
      this.page.update(p => p + 1);
    }
  }

  lastPage() {
    this.page.set(this.totalPages());
  }

  setPage(page: number) {
    const target = Math.max(
      1,
      Math.min(page, this.totalPages())
    );

    this.page.set(target);
  }

  setPageSize(size: number) {
    const value = Number(size);

    if (!value || value === this.pageSize()) {
      return;
    }

    this.pageSize.set(value);
    this.page.set(1);
  }

  onPageSizeChange(value: string) {
    this.setPageSize(Number(value));
  }
}