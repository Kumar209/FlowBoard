import { Routes } from '@angular/router';
import { authGuard, orgAdminGuard } from './core/guards/auth.guard';
import { superAdminGuard } from './core/guards/superadmin.guard';

export const routes: Routes = [
  { path: 'maintenance', loadComponent: () => import('./features/maintenance/maintenance.component').then(m => m.MaintenanceComponent) },
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
        loadChildren: () => import('./features/project/project.routes').then(m => m.projectRoutes),
        data: { preload: true }
      },
      { path: 'notifications', loadComponent: () => import('./features/notifications/notification-list/notification-list.component').then(m => m.NotificationListComponent) },
      { path: 'activity', loadComponent: () => import('./features/activity/activity.component').then(m => m.ActivityComponent) },
      { path: 'members', loadComponent: () => import('./features/members/members.component').then(m => m.MembersComponent) },
      { path: 'roles', canActivate: [orgAdminGuard], loadComponent: () => import('./features/roles/roles.component').then(m => m.RolesComponent) },
      { path: 'roles/:roleId/permissions', canActivate: [orgAdminGuard], loadComponent: () => import('./features/roles/role-permissions/role-permissions.component').then(m => m.RolePermissionsComponent) },
      { path: 'system', canActivate: [orgAdminGuard], loadComponent: () => import('./features/system/system.component').then(m => m.SystemComponent) },
      { path: 'ai-usage', canActivate: [orgAdminGuard], loadComponent: () => import('./features/ai-usage/ai-usage.component').then(m => m.AiUsageComponent) },
      { path: 'support', canActivate: [orgAdminGuard], loadComponent: () => import('./features/support/support.component').then(m => m.SupportComponent) },
      { path: 'support/:id', canActivate: [orgAdminGuard], loadComponent: () => import('./features/support/complaint-detail/complaint-detail.component').then(m => m.ComplaintDetailComponent) },
    ]
  },
  {
    path: 'superadmin',
    canActivate: [authGuard, superAdminGuard],
    loadChildren: () => import('./features/superadmin/superadmin.routes').then(m => m.superAdminRoutes),
    data: { preload: false }
  },
  { path: '**', redirectTo: '' }
];
