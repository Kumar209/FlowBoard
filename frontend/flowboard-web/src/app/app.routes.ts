import { Routes } from '@angular/router';
import { authGuard, orgAdminGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent) },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./shared/components/layout/layout.component').then(m => m.LayoutComponent),
    children: [
      { path: '', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'w', loadComponent: () => import('./features/workspaces/workspaces.component').then(m => m.WorkspacesComponent) },
      { path: 'w/:wid', loadComponent: () => import('./features/workspace/workspace.component').then(m => m.WorkspaceComponent) },
      { path: 'projects', loadComponent: () => import('./features/projects/projects.component').then(m => m.ProjectsComponent) },
      {
        path: 'w/:wid/p/:pid',
        loadComponent: () => import('./features/project/project-layout/project-layout.component').then(m => m.ProjectLayoutComponent),
        children: [
          { path: '', redirectTo: 'overview', pathMatch: 'full' },
          { path: 'overview', loadComponent: () => import('./features/project/overview/overview.component').then(m => m.OverviewComponent) },
          { path: 'board', loadComponent: () => import('./features/board/board/board.component').then(m => m.BoardComponent) },
          { path: 'boards', loadComponent: () => import('./features/project/boards/boards.component').then(m => m.BoardsComponent) },
          { path: 'backlog', loadComponent: () => import('./features/project/backlog/backlog.component').then(m => m.BacklogComponent) },
          { path: 'sprints', loadComponent: () => import('./features/project/sprints/sprints.component').then(m => m.SprintsComponent) },
          { path: 'issues', loadComponent: () => import('./features/project/issues/issues.component').then(m => m.IssuesComponent) },
          { path: 'team', loadComponent: () => import('./features/project/team/team.component').then(m => m.TeamComponent) },
          { path: 'team/:teamId', loadComponent: () => import('./features/project/team-detail/team-detail.component').then(m => m.TeamDetailComponent) },
          { path: 'activity', loadComponent: () => import('./features/project/activity/project-activity.component').then(m => m.ProjectActivityComponent) },
          { path: 'environments', loadComponent: () => import('./features/project/environments/environments.component').then(m => m.EnvironmentsComponent) },
          { path: 'docs', loadComponent: () => import('./features/project/docs/docs.component').then(m => m.DocsComponent) },
          { path: 'settings', loadComponent: () => import('./features/project/settings/settings.component').then(m => m.SettingsComponent) },
        ]
      },
      { path: 'activity', loadComponent: () => import('./features/activity/activity.component').then(m => m.ActivityComponent) },
      { path: 'members', loadComponent: () => import('./features/members/members.component').then(m => m.MembersComponent) },
      { path: 'system', canActivate: [orgAdminGuard], loadComponent: () => import('./features/system/system.component').then(m => m.SystemComponent) },
    ]
  },
  { path: '**', redirectTo: '' }
];
