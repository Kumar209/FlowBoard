import { Component, OnInit, ChangeDetectionStrategy, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { ToastService } from '../../core/services/toast.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';

/**
 * DashboardComponent - MNC-grade: OnPush + inject() + Signals + RouterLink (not dead buttons).
 * Header moved to LayoutComponent (drawer), so this page is just content under Layout.
 * workspaceId Signal defaults to demo 111... and updates from me() workspaces[0].id for View projects link.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit {
  auth = inject(AuthService);
  private wsService = inject(WorkspaceService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);
  workspaceId = signal<string>('11111111-1111-1111-1111-111111111111');
  editOrgOpen = signal(false);
  orgName = signal('');
  orgDesc = signal('');

  orgsQuery = injectQuery(() => ({
    queryKey: ['organizations'] as const,
    queryFn: () => firstValueFrom(this.wsService.getMyOrganizations()),
  }));
  org = computed(() => this.orgsQuery.data()?.[0] as any);

  isOrgAdmin = computed(() => this.auth.isOrgAdmin() || this.auth.isSuperAdmin());

  updateOrgMutation = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.wsService.updateOrganization(this.org()!.id, this.orgName().trim(), this.orgDesc().trim() || undefined)),
    onSuccess: () => { this.qc.invalidateQueries({queryKey:['organizations']}); this.editOrgOpen.set(false); this.toast.success('Organization updated'); },
    onError: (e:any) => this.toast.error(e.error?.error || 'Update failed')
  }));

  openEditOrg(){
    const o = this.org();
    if(!o) return;
    this.orgName.set(o.name);
    this.orgDesc.set(o.description || '');
    this.editOrgOpen.set(true);
  }

  ngOnInit() {
    this.auth.me().subscribe({
      next: (res: any) => {
        this.auth.hydrateFromMe(res);
        const ws = res?.workspaces?.[0];
        if (ws?.id) this.workspaceId.set(ws.id);
        else if (ws?.workspaceId) this.workspaceId.set(ws.workspaceId);
      },
      error: () => {}
    });
  }
}
