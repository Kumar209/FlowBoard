import { Routes } from '@angular/router';
import { SuperAdminLayoutComponent } from './superadmin-layout/superadmin-layout.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { OrganizationsComponent } from './organizations/organizations.component';
import { OrgMembersComponent } from './org-members/org-members.component';
import { UsersComponent } from './users/users.component';
import { SubscriptionsComponent } from './subscriptions/subscriptions.component';
import { ActivityComponent } from './activity/activity.component';
import { SystemComponent } from './system/system.component';
import { SupportComponent } from './support/support.component';
import { ComplaintDetailComponent as SuperSupportDetail } from './support/complaint-detail/complaint-detail.component';
import { FlagsComponent } from './flags/flags.component';
import { AiUsageComponent } from './ai-usage/ai-usage.component';
import { SettingsComponent } from './settings/settings.component';

export const superAdminRoutes: Routes = [
  {
    path: '',
    component: SuperAdminLayoutComponent,
    children: [
      { path: '', component: DashboardComponent },
      { path: 'organizations', component: OrganizationsComponent },
      { path: 'organizations/:orgId/members', component: OrgMembersComponent },
      { path: 'users', component: UsersComponent },
      { path: 'subscriptions', component: SubscriptionsComponent },
      { path: 'activity', component: ActivityComponent },
      { path: 'system', component: SystemComponent },
      { path: 'support', component: SupportComponent },
      { path: 'support/:id', component: SuperSupportDetail },
      { path: 'flags', component: FlagsComponent },
      { path: 'ai-usage', component: AiUsageComponent },
      { path: 'settings', component: SettingsComponent },
    ]
  }
];
