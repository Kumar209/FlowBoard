import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { WorkspaceService } from '../../core/services/workspace.service';
import { OrganizationRoleService } from '../../core/services/organization-role.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-members',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './members.component.html',
  styleUrls: ['./members.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MembersComponent {
  auth = inject(AuthService);
  private ws = inject(WorkspaceService);
  private roleService = inject(OrganizationRoleService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);

  search = signal('');
  page = signal(1);
  pageSize = 8;
  showInvite = signal(false);
  inviteFullName = signal('');
  inviteEmail = signal('');
  invitePassword = signal('');
  showInvitePassword = signal(false);
  inviteRole = signal('Member');
  inviteWorkspaceIds = signal<string[]>([]);
  inviteWorkspaceRoles = signal<Record<string,string>>({});
  editTarget = signal<any>(null);
  editName = signal('');
  editEmail = signal('');
  editRole = signal('Member');
  editWorkspaceIds = signal<string[]>([]);
  editWorkspaceRoles = signal<Record<string,string>>({});

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
    enabled: !!this.orgId(),
  }));

  customRolesQuery = injectQuery(() => ({
    queryKey: ['org-roles', this.orgId()] as const,
    queryFn: () => firstValueFrom(this.roleService.getRoles(this.orgId())),
    enabled: !!this.orgId(),
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
      const wids = this.inviteWorkspaceIds();
      const customRoles = (this.customRolesQuery.data() as any[]) || [];
      const roles = wids.map(id => {
        const selectedName = this.inviteWorkspaceRoles()[id];
        const cr = customRoles.find((c:any) => c.name === selectedName);
        return { workspaceId: id, role: selectedName || cr?.name || 'Member', customRoleId: cr?.id || undefined };
      });
      const orgRole = this.inviteRole().trim() || 'Member';
      return firstValueFrom(this.ws.createOrganizationMember(orgId, this.inviteFullName().trim(), this.inviteEmail().trim(), this.invitePassword().trim(), roles as any, orgRole));
    },
    onSuccess: () => {
      this.qc.invalidateQueries({queryKey:['org-members']});
      this.qc.invalidateQueries({queryKey:['agg-members']});
      this.showInvite.set(false);
      this.inviteFullName.set(''); this.inviteEmail.set(''); this.invitePassword.set(''); this.inviteWorkspaceIds.set([]);
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
    mutationFn: () => {
      const wids = this.editWorkspaceIds();
      const customRoles = (this.customRolesQuery.data() as any[]) || [];
      const roles = wids.map(id => {
        const selectedName = this.editWorkspaceRoles()[id];
        const cr = customRoles.find((c:any) => c.name === selectedName);
        return { workspaceId: id, role: selectedName || cr?.name || 'Member', customRoleId: cr?.id || undefined };
      });
      const orgRole = this.editRole().trim() || undefined;
      return firstValueFrom(this.ws.updateOrganizationMember(this.orgId(), this.editTarget()!.userId, this.editName().trim() || undefined, this.editEmail().trim() || undefined, roles as any, orgRole));
    },
    onSuccess: () => {
      this.qc.invalidateQueries({queryKey:['org-members']});
      this.qc.invalidateQueries({queryKey:['agg-members']});
      this.editTarget.set(null);
      this.toast.success('Member updated');
    },
    onError: (e:any) => this.toast.error(e.error?.error || 'Update failed')
  }));

  openInvite(){
    const wss = this.workspacesQuery.data() || [];
    this.inviteFullName.set(''); this.inviteEmail.set(''); this.invitePassword.set(''); this.inviteRole.set('Member');
    const customRoles = (this.customRolesQuery.data() as any[]) || [];
    // If no custom roles, disable workspace checklist (org-level only)
    if (customRoles.length === 0) {
      this.inviteWorkspaceIds.set([]);
      this.inviteWorkspaceRoles.set({});
      this.showInvite.set(true);
      return;
    }
    const dev = wss.find((w:any) => w.name.toLowerCase().includes('development')) || wss[0];
    const ids = dev ? [dev.id] : wss.slice(0,1).map((w:any)=>w.id);
    const defaultRole = customRoles[0]?.name || 'Member';
    this.inviteWorkspaceIds.set(ids);
    const map: Record<string,string> = {};
    ids.forEach(id => map[id] = defaultRole);
    this.inviteWorkspaceRoles.set(map);
    this.showInvite.set(true);
  }
  doInvite(){ if(!this.inviteFullName().trim() || !this.inviteEmail().trim() || !this.invitePassword().trim()) return; this.inviteMutation.mutate(); }
  confirmRemove(m:any){ this.removeMutation.mutate(m.userId); }
  onInviteWorkspaceChecked(workspaceId: string, checked: boolean){
    if(checked){
      const customRoles = (this.customRolesQuery.data() as any[]) || [];
      const defaultRole = customRoles[0]?.name || '';
      if (!defaultRole) { // no custom roles -> keep empty, will be org-level only
        this.inviteWorkspaceIds.set([...this.inviteWorkspaceIds(), workspaceId]);
        this.inviteWorkspaceRoles.set({...this.inviteWorkspaceRoles(), [workspaceId]: ''});
        return;
      }
      this.inviteWorkspaceIds.set([...this.inviteWorkspaceIds(), workspaceId]);
      this.inviteWorkspaceRoles.set({...this.inviteWorkspaceRoles(), [workspaceId]: defaultRole});
    } else {
      this.inviteWorkspaceIds.set(this.inviteWorkspaceIds().filter(id=>id!==workspaceId));
      const copy = {...this.inviteWorkspaceRoles()}; delete copy[workspaceId]; this.inviteWorkspaceRoles.set(copy);
    }
  }
  onInviteRoleChange(workspaceId: string, role: string){
    this.inviteWorkspaceRoles.set({...this.inviteWorkspaceRoles(), [workspaceId]: role});
  }
  onEditWorkspaceChecked(workspaceId: string, checked: boolean){
    if(checked){
      const customRoles = (this.customRolesQuery.data() as any[]) || [];
      const defaultRole = customRoles[0]?.name || '';
      if (!defaultRole) {
        this.editWorkspaceIds.set([...this.editWorkspaceIds(), workspaceId]);
        this.editWorkspaceRoles.set({...this.editWorkspaceRoles(), [workspaceId]: ''});
        return;
      }
      this.editWorkspaceIds.set([...this.editWorkspaceIds(), workspaceId]);
      this.editWorkspaceRoles.set({...this.editWorkspaceRoles(), [workspaceId]: defaultRole});
    } else {
      this.editWorkspaceIds.set(this.editWorkspaceIds().filter(id=>id!==workspaceId));
      const copy = {...this.editWorkspaceRoles()}; delete copy[workspaceId]; this.editWorkspaceRoles.set(copy);
    }
  }
  onEditRoleChange(workspaceId: string, role: string){
    this.editWorkspaceRoles.set({...this.editWorkspaceRoles(), [workspaceId]: role});
  }
  openEdit(m:any){
    this.editTarget.set(m);
    this.editName.set(m.fullName);
    this.editEmail.set(m.email);
    // org-level editRole must be one of Member/OrgAdmin/Client - map incoming to 3-role
    const mappedOrg = ['Member','OrgAdmin','Client','SuperAdmin'].includes(m.role) ? m.role : 'Member';
    this.editRole.set(mappedOrg);
    const wids = (m as any).workspaceIds as string[] | undefined;
    const allWids = wids && wids.length ? wids : ((m as any).workspaceId ? [(m as any).workspaceId] : []);
    const ids = allWids.length ? allWids : (this.workspacesQuery.data()?.slice(0,1).map((w:any)=>w.id) || []);
    const customRoles = (this.customRolesQuery.data() as any[]) || [];
    this.editWorkspaceIds.set(customRoles.length ? ids : []);
    const map: Record<string,string> = {};
    // Use per-workspace custom role from API if available, else fallback to custom default
    const wsRoleMap = (m as any).workspaceRoleMap as Record<string,string> | undefined;
    const defaultCr = customRoles[0]?.name || '';
    ids.forEach(id => {
      const perWsRole = wsRoleMap?.[id] || wsRoleMap?.[id.toLowerCase()] || '';
      map[id] = perWsRole || defaultCr || m.role;
    });
    if (!customRoles.length) this.editWorkspaceIds.set([]);
    this.editWorkspaceRoles.set(map);
  }
  saveEdit(){
    const t = this.editTarget(); if(!t) return;
    this.updateMutation.mutate();
  }
}
