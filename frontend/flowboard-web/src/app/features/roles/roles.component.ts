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
  selector: 'app-roles',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './roles.component.html',
  styleUrls: ['./roles.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RolesComponent {
  private ws = inject(WorkspaceService);
  private roleService = inject(OrganizationRoleService);
  auth = inject(AuthService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);

  search = signal('');
  showAdd = signal(false);
  addName = signal('');
  addDescription = signal('');
  editTarget = signal<any>(null);
  editName = signal('');
  editDescription = signal('');
  deleteTarget = signal<any>(null);

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.ws.getMyWorkspaces()),
  }));

  orgId = computed(() => {
    const ws = this.workspacesQuery.data() || [];
    return (ws[0] as any)?.organizationId || (ws[0] as any)?.OrganizationId || '';
  });

  rolesQuery = injectQuery(() => ({
    queryKey: ['org-roles', this.orgId()] as const,
    queryFn: () => firstValueFrom(this.roleService.getRoles(this.orgId())),
    enabled: !!this.orgId(),
  }));

  canManage = computed(() => this.auth.isOrgAdmin() || this.auth.isSuperAdmin());

  filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    const list = (this.rolesQuery.data() as any[]) || [];
    if (!q) return list;
    return list.filter((r: any) => r.name.toLowerCase().includes(q) || (r.description||'').toLowerCase().includes(q));
  });

  createMutation = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.roleService.createRole(this.orgId(), this.addName().trim(), this.addDescription().trim() || undefined)),
    onSuccess: () => {
      this.qc.invalidateQueries({ queryKey: ['org-roles'] });
      this.showAdd.set(false);
      this.addName.set(''); this.addDescription.set('');
      this.toast.success('Role created');
    },
    onError: (e:any) => this.toast.error(e.error?.error || 'Create failed')
  }));

  updateMutation = injectMutation(() => ({
    mutationFn: () => {
      const t = this.editTarget(); if (!t) throw new Error('No target');
      return firstValueFrom(this.roleService.updateRole(this.orgId(), t.id, this.editName().trim(), this.editDescription().trim() || undefined));
    },
    onSuccess: () => {
      this.qc.invalidateQueries({ queryKey: ['org-roles'] });
      this.editTarget.set(null);
      this.toast.success('Role updated');
    },
    onError: (e:any) => this.toast.error(e.error?.error || 'Update failed')
  }));

  deleteMutation = injectMutation(() => ({
    mutationFn: (roleId:string) => firstValueFrom(this.roleService.deleteRole(this.orgId(), roleId)),
    onSuccess: () => {
      this.qc.invalidateQueries({ queryKey: ['org-roles'] });
      this.deleteTarget.set(null);
      this.toast.success('Role deleted');
    },
    onError: (e:any) => this.toast.error(e.error?.error || e.error || 'Delete failed')
  }));

  openAdd(){ this.addName.set(''); this.addDescription.set(''); this.showAdd.set(true); }
  doCreate(){ if(!this.addName().trim()) return; this.createMutation.mutate(); }

  openEdit(r:any){
    this.editTarget.set(r);
    this.editName.set(r.name);
    this.editDescription.set(r.description || '');
  }
  doUpdate(){ if(!this.editName().trim()) return; this.updateMutation.mutate(); }

  openDelete(r:any){ this.deleteTarget.set(r); }
  confirmDelete(){ const t=this.deleteTarget(); if(!t) return; this.deleteMutation.mutate(t.id); }
}
