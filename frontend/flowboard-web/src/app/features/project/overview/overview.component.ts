import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { injectQuery } from '@tanstack/angular-query-experimental';

/**
 * Overview - Project stats, recent activity, quick links. Jira-style overview.
 */
@Component({
  selector: 'app-project-overview',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './overview.component.html',
  styleUrls: ['./overview.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OverviewComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  projectId = signal(this.route.parent?.snapshot.paramMap.get('pid') || this.route.snapshot.paramMap.get('pid') || '');
  workspaceId = signal(this.route.parent?.snapshot.paramMap.get('wid') || this.route.snapshot.paramMap.get('wid') || '');
  boardQuery = injectQuery(() => ({
    queryKey: ['board', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getBoard(this.projectId())),
    enabled: !!this.projectId(),
  }));
  boardsQuery = injectQuery(() => ({
    queryKey: ['boards', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getBoards(this.projectId())),
    enabled: !!this.projectId(),
  }));
  sprintsQuery = injectQuery(() => ({
    queryKey: ['sprints', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getSprints(this.projectId())),
    enabled: !!this.projectId(),
  }));
  membersQuery = injectQuery(() => ({
    queryKey: ['workspace-members', this.workspaceId()] as const,
    queryFn: () => firstValueFrom(this.ps.getWorkspaceMembers(this.workspaceId())),
    enabled: !!this.workspaceId(),
  }));
  activitiesQuery = injectQuery(() => ({
    queryKey: ['activities', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getActivities(this.projectId(), 1, 5)),
    enabled: !!this.projectId(),
  }));
  todoCount(b:any){ return b?.tasks?.filter((t:any)=> b.lists[0] && t.listId===b.lists[0].id).length || 0; }
  inReviewCount(b:any){ const id=b?.lists?.find((l:any)=>l.name==='In Review')?.id; return b?.tasks?.filter((t:any)=>t.listId===id).length || 0; }
  doneCount(b:any){ const id=b?.lists?.find((l:any)=>l.name==='Done')?.id; return b?.tasks?.filter((t:any)=>t.listId===id).length || 0; }
}
