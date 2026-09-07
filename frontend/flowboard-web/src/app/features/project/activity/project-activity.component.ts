import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
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
  projectId = signal(this.route.parent?.snapshot.paramMap.get('pid') || this.route.snapshot.paramMap.get('pid') || '');
  workspaceId = signal(this.route.parent?.snapshot.paramMap.get('wid') || '');
  page = signal(1);
  pageSize = 20;

  workspaceIdResolved = computed(() => this.workspaceId() || this.route.snapshot.paramMap.get('wid') || '');

  membersQuery = injectQuery(() => ({
    queryKey: ['workspace-members', this.workspaceIdResolved()] as const,
    queryFn: () => firstValueFrom(this.ps.getWorkspaceMembers(this.workspaceIdResolved())),
    enabled: () => !!this.workspaceIdResolved(),
  }));

  activitiesQuery = injectQuery(() => ({
    queryKey: ['activities', this.projectId(), this.page()] as const,
    queryFn: () => firstValueFrom(this.ps.getActivities(this.projectId(), this.page(), this.pageSize)),
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
