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
      { path: 'w/:wid/p/:pid/board', loadComponent: () => import('./features/board/board/board.component').then(m => m.BoardComponent) },
      { path: 'activity', loadComponent: () => import('./features/activity/activity.component').then(m => m.ActivityComponent) },
      { path: 'members', loadComponent: () => import('./features/members/members.component').then(m => m.MembersComponent) },
      { path: 'system', canActivate: [orgAdminGuard], loadComponent: () => import('./features/system/system.component').then(m => m.SystemComponent) },
    ]
  },
  { path: '**', redirectTo: '' }
];
