import { Routes } from '@angular/router';
import { ProjectLayoutComponent } from './project-layout/project-layout.component';
import { OverviewComponent } from './overview/overview.component';
import { BoardComponent } from '../../features/board/board.component';
import { BoardsComponent } from './boards/boards.component';
import { BacklogComponent } from './backlog/backlog.component';
import { SprintsComponent } from './sprints/sprints.component';
import { IssuesComponent } from './issues/issues.component';
import { TeamComponent } from './team/team.component';
import { TeamDetailComponent } from './team-detail/team-detail.component';
import { ProjectMembersComponent } from './members/project-members.component';
import { ProjectActivityComponent } from './activity/project-activity.component';
import { StatusesComponent } from './statuses/statuses.component';
import { EnvironmentsComponent } from './environments/environments.component';
import { DocsComponent } from './docs/docs.component';
import { SettingsComponent } from './settings/settings.component';
import { ProjectAiUsageComponent } from './ai-usage/ai-usage.component';

export const projectRoutes: Routes = [
  {
    path: '',
    component: ProjectLayoutComponent,
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
      { path: 'overview', component: OverviewComponent },
      { path: 'board', component: BoardComponent },
      { path: 'boards', component: BoardsComponent },
      { path: 'backlog', component: BacklogComponent },
      { path: 'sprints', component: SprintsComponent },
      { path: 'issues', component: IssuesComponent },
      { path: 'team', component: TeamComponent },
      { path: 'team/:teamId', component: TeamDetailComponent },
      { path: 'members', component: ProjectMembersComponent },
      { path: 'activity', component: ProjectActivityComponent },
      { path: 'statuses', component: StatusesComponent },
      { path: 'environments', component: EnvironmentsComponent },
      { path: 'docs', component: DocsComponent },
      { path: 'settings', component: SettingsComponent },
      { path: 'ai-usage', component: ProjectAiUsageComponent },
    ]
  }
];
