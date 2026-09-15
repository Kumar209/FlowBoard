import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { injectQuery } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-project-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './project-layout.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectLayoutComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private projectService = inject(ProjectService);

  workspaceId = signal(this.route.snapshot.paramMap.get('wid') || this.route.parent?.snapshot.paramMap.get('wid') || '');
  projectId = signal(this.route.snapshot.paramMap.get('pid') || '');
  sidebarOpen = signal(false);

  constructor() {
    this.route.paramMap.subscribe(m => {
      const wid = m.get('wid') || this.route.snapshot.paramMap.get('wid') || '';
      const pid = m.get('pid') || '';
      if (wid) this.workspaceId.set(wid);
      if (pid) this.projectId.set(pid);
    });

    this.route.parent?.paramMap.subscribe(m => {
      const wid = m.get('wid');
      if (wid) this.workspaceId.set(wid);
    });
  }

  boardsQuery = injectQuery(() => ({
    queryKey: ['boards', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getBoards(this.projectId())),
    enabled: !!this.projectId(),
  }));

  sprintsQuery = injectQuery(() => ({
    queryKey: ['sprints', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getSprints(this.projectId())),
    enabled: !!this.projectId(),
  }));

  membersQuery = injectQuery(() => ({
    queryKey: ['workspace-members', this.workspaceId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getWorkspaceMembers(this.workspaceId())),
    enabled: !!this.workspaceId(),
  }));

  teamsQuery = injectQuery(() => ({
    queryKey: ['teams', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getTeams(this.projectId())),
    enabled: !!this.projectId(),
  }));

  projectMembersQuery = injectQuery(() => ({
    queryKey: ['project-members', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getProjectMembers(this.projectId(), 1, 1)),
    enabled: !!this.projectId(),
  }));

  boardViews = computed(() => {
    const api = this.boardsQuery.data();
    if (api && api.length > 0) {
      return api.map((b: any) => ({
        id: b.id,
        name: b.name,
        filter: 'all',
        icon: b.type === 'Scrum' ? '🟣' : '🔵',
        type: `${b.type} • ${b.name}`
      }));
    }
    return [];
  });

  selectedBoardView = signal('main');

  onBoardViewChange(viewId: string) {
    this.selectedBoardView.set(viewId);
    const wid = this.workspaceId();
    const pid = this.projectId();
    this.router.navigate([`/w/${wid}/p/${pid}/board`], { queryParams: { view: viewId } });
  }

  projectQuery = injectQuery(() => ({
    queryKey: ['board', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getBoard(this.projectId())),
    enabled: !!this.projectId(),
  }));

  navItems = computed(() => {
    const wid = this.workspaceId();
    const pid = this.projectId();
    const base = `/w/${wid}/p/${pid}`;
    const allTasks = this.projectQuery.data()?.tasks ?? [];
    const backlogCount = allTasks.filter((t: any) => !t.sprintId).length;
    const boardsCount = this.boardsQuery.data()?.length ?? 0;
    const sprintsCount = this.sprintsQuery.data()?.length ?? 0;
    const teamsCount = this.teamsQuery.data()?.length ?? 0;
    const taskCount = allTasks.length;
    const projectMembersCount = (this.projectMembersQuery.data() as any)?.total ?? 0;

    return [
      { label: 'Overview', icon: 'overview', path: `${base}/overview`, badge: '' },
      { label: 'Boards', icon: 'boards', path: `${base}/boards`, badge: `${boardsCount}` },
      { label: 'Backlog', icon: 'backlog', path: `${base}/backlog`, badge: `${backlogCount}` },
      { label: 'Sprints', icon: 'sprints', path: `${base}/sprints`, badge: `${sprintsCount}` },
      { label: 'Issues', icon: 'issues', path: `${base}/issues`, badge: `${taskCount}` },
      { label: 'Teams', icon: 'teams', path: `${base}/team`, badge: teamsCount ? `${teamsCount}` : '' },
      { label: 'Members', icon: 'members', path: `${base}/members`, badge: projectMembersCount ? `${projectMembersCount}` : '' },
      { label: 'Statuses', icon: 'statuses', path: `${base}/statuses`, badge: '' },
      { label: 'Environments', icon: 'environments', path: `${base}/environments`, badge: '' },
      { label: 'Activity', icon: 'activity', path: `${base}/activity`, badge: '' },
      { label: 'AI Usage', icon: 'ai', path: `${base}/ai-usage`, badge: '' },
      { label: 'Docs', icon: 'docs', path: `${base}/docs`, badge: '' },
      { label: 'Settings', icon: 'settings', path: `${base}/settings`, badge: '' },
    ];
  });
}