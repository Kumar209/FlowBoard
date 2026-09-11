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
  styleUrls: ['./project-activity.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectActivityComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  private wsService = inject(WorkspaceService);
  projectId = signal(this.route.parent?.snapshot.paramMap.get('pid') || this.route.snapshot.paramMap.get('pid') || '');
  workspaceId = signal(this.route.parent?.snapshot.paramMap.get('wid') || '');
  page = signal(1);
  pageSize = 10;

  workspaceIdResolved = computed(() => this.workspaceId() || this.route.snapshot.paramMap.get('wid') || '');

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.wsService.getMyWorkspaces()),
  }));

  workspaceName = computed(() => {
    const wid = this.workspaceIdResolved();
    const list: any[] = (this.workspacesQuery.data() as any) || [];
    const w = list.find((x:any) => (x.id || x.Id) === wid);
    return w ? (w.name || w.Name) : (wid ? wid.slice(0,6) : 'Workspace');
  });

  membersQuery = injectQuery(() => ({
    queryKey: ['workspace-members', this.workspaceIdResolved()] as const,
    queryFn: () => firstValueFrom(this.ps.getWorkspaceMembers(this.workspaceIdResolved())),
    enabled: () => !!this.workspaceIdResolved(),
  }));

  activitiesQuery = injectQuery(() => ({
    queryKey: ['activities', this.projectId(), this.page()] as const,
    queryFn: async () => {
      const res: any = await firstValueFrom(this.ps.getActivities(this.projectId(), this.page(), this.pageSize));
      const items = (res.items || res.Items || []).map((a:any) => ({
        ...a,
        workspaceId: a.workspaceId || a.WorkspaceId || null,
        occurredAt: a.occurredAt || a.OccurredAt,
      }));
      return { items, total: res.total ?? res.Total ?? items.length };
    },
    enabled: !!this.projectId(),
  }));

  getActorDisplay = (actorId: string) => {
    const members = this.membersQuery.data() || [];
    const m = members.find((x:any) => x.userId === actorId);
    if (m) return `${m.fullName} (${m.role})`;
    return actorId.slice(0,6);
  };

  total = computed(() => this.activitiesQuery.data()?.total || 0);
  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));
}
