import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WorkspaceService } from '../../core/services/workspace.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-members',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './members.component.html',
  styleUrls: ['./members.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MembersComponent {
  auth = inject(AuthService);
  private ws = inject(WorkspaceService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);

  search = signal('');
  page = signal(1);
  pageSize = 8;
  showInvite = signal(false);
  inviteFullName = signal('');
  inviteEmail = signal('');
  invitePassword = signal('');
  inviteRole = signal('Member');
  inviteWorkspaceId = signal('');
  editTarget = signal<any>(null);
  editName = signal('');
  editEmail = signal('');
  editRole = signal('Member');

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.ws.getMyWorkspaces()),
  }));

  orgId = computed(() => {
    const ws = this.workspacesQuery.data() || [];
    // Use first workspace's organizationId as org (all workspaces in same org for this tenant)
    return (ws[0] as any)?.organizationId || (ws[0] as any)?.OrganizationId || '';
  });

  orgMembersQuery = injectQuery(() => ({
    queryKey: ['org-members', this.orgId()] as const,
    queryFn: () => firstValueFrom(this.ws.getOrganizationMembers(this.orgId())),
    enabled: () => !!this.orgId(),
  }));

  // Fallback: aggregate workspace members if org endpoint fails
  workspaceMembersAggQuery = injectQuery(() => ({
    queryKey: ['agg-members'] as const,
    queryFn: async () => {
      const wss = this.workspacesQuery.data() || [];
      const all: any[] = [];
      for (const w of wss) {
        try {
          const ms = await firstValueFrom(this.ws.getWorkspaceMembers(w.id));
          all.push(...ms.map((m:any) => ({...m, workspaceName: w.name})));
        } catch {}
      }
      const map = new Map();
      for (const m of all) if (!map.has(m.userId)) map.set(m.userId, m);
      return Array.from(map.values());
    },
    enabled: () => !!this.workspacesQuery.data()?.length && !this.orgId(),
  }));

  members = computed(() => {
    const org = this.orgMembersQuery.data() as any[];
    if (org && org.length) return org;
    return (this.workspaceMembersAggQuery.data() as any[]) || [];
  });

  filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    const list = this.members() || [];
    if (!q) return list;
    return list.filter((m:any) => m.fullName?.toLowerCase().includes(q) || m.email?.toLowerCase().includes(q) || (m.role||'').toLowerCase().includes(q));
  });
  totalPages = computed(() => Math.max(1, Math.ceil(this.filtered().length / this.pageSize)));
  paginated = computed(() => {
    const start = (this.page()-1)*this.pageSize;
    return this.filtered().slice(start, start+this.pageSize);
  });

  canInvite = computed(() => this.auth.isOrgAdmin() || this.auth.isSuperAdmin());

  inviteMutation = injectMutation(() => ({
    mutationFn: () => {
      const orgId = this.orgId();
      if (!orgId) throw new Error('No organization');
      return firstValueFrom(this.ws.createOrganizationMember(orgId, this.inviteFullName().trim(), this.inviteEmail().trim(), this.invitePassword().trim(), this.inviteRole(), this.inviteWorkspaceId() || undefined));
    },
    onSuccess: () => {
      this.qc.invalidateQueries({queryKey:['org-members']});
      this.qc.invalidateQueries({queryKey:['agg-members']});
      this.showInvite.set(false);
      this.inviteFullName.set(''); this.inviteEmail.set(''); this.invitePassword.set('');
      this.toast.success('Employee created');
    },
    onError: (e:any) => this.toast.error(e.error?.error || 'Create failed')
  }));

  removeMutation = injectMutation(() => ({
    mutationFn: (userId:string) => firstValueFrom(this.ws.deleteOrganizationMember(this.orgId(), userId)),
    onSuccess: () => {
      this.qc.invalidateQueries({queryKey:['org-members']});
      this.qc.invalidateQueries({queryKey:['agg-members']});
      this.toast.success('Member removed');
    },
    onError: (e:any) => this.toast.error(e.error?.error || 'Remove failed')
  }));
  updateMutation = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.ws.updateOrganizationMember(this.orgId(), this.editTarget()!.userId, this.editName().trim() || undefined, this.editEmail().trim() || undefined, this.editRole())),
    onSuccess: () => {
      this.qc.invalidateQueries({queryKey:['org-members']});
      this.qc.invalidateQueries({queryKey:['agg-members']});
      this.editTarget.set(null);
      this.toast.success('Member updated');
    },
    onError: (e:any) => this.toast.error(e.error?.error || 'Update failed')
  }));

  openInvite(){ this.inviteFullName.set(''); this.inviteEmail.set(''); this.invitePassword.set(''); this.inviteRole.set('Member'); this.inviteWorkspaceId.set(this.workspacesQuery.data()?.[0]?.id || ''); this.showInvite.set(true); }
  doInvite(){ if(!this.inviteFullName().trim() || !this.inviteEmail().trim() || !this.invitePassword().trim()) return; this.inviteMutation.mutate(); }
  confirmRemove(m:any){ this.removeMutation.mutate(m.userId); }
  openEdit(m:any){ this.editTarget.set(m); this.editName.set(m.fullName); this.editEmail.set(m.email); this.editRole.set(m.role); }
  saveEdit(){
    const t = this.editTarget(); if(!t) return;
    this.updateMutation.mutate();
  }
}
