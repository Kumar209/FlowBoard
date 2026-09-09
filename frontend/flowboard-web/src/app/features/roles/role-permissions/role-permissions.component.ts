import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { OrganizationRoleService, PermissionDto } from '../../../core/services/organization-role.service';
import { ToastService } from '../../../core/services/toast.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-role-permissions',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './role-permissions.component.html',
  styleUrls: ['./role-permissions.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RolePermissionsComponent {
  private route = inject(ActivatedRoute);
  private ws = inject(WorkspaceService);
  private roleService = inject(OrganizationRoleService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);

  roleId = signal(this.route.snapshot.paramMap.get('roleId') || '');

  selected = signal<Set<string>>(new Set());

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.ws.getMyWorkspaces()),
  }));
  orgId = computed(() => {
    const ws = this.workspacesQuery.data() || [];
    return (ws[0] as any)?.organizationId || (ws[0] as any)?.OrganizationId || '';
  });

  rolePermissionsQuery = injectQuery(() => ({
    queryKey: ['role-permissions', this.orgId(), this.roleId()] as const,
    queryFn: async () => {
      const res: any = await firstValueFrom(this.roleService.getRolePermissions(this.orgId(), this.roleId()));
      // initialize selected on first load
      const ids: string[] = res.permissionIds || res.PermissionIds || [];
      // use microtask to avoid ExpressionChangedAfterItHasBeenChecked
      queueMicrotask(() => this.selected.set(new Set(ids)));
      return res;
    },
    enabled: !!this.orgId() && !!this.roleId(),
  }));

  permissionsQuery = injectQuery(() => ({
    queryKey: ['permissions'] as const,
    queryFn: () => firstValueFrom(this.roleService.getPermissions()),
  }));

  grouped = computed(() => {
    const perms = (this.permissionsQuery.data() as PermissionDto[]) || [];
    const map = new Map<string, PermissionDto[]>();
    for (const p of perms) {
      const g = p.group || 'other';
      if (!map.has(g)) map.set(g, []);
      map.get(g)!.push(p);
    }
    // sort groups alphabetically, permissions by key
    return Array.from(map.entries()).sort((a,b) => a[0].localeCompare(b[0])).map(([group, list]) => ({
      group, label: group.charAt(0).toUpperCase() + group.slice(1),
      permissions: list.sort((x,y) => x.key.localeCompare(y.key))
    }));
  });

  roleDto = computed(() => {
    const data: any = this.rolePermissionsQuery.data();
    return data?.role || data?.Role || null;
  });

  isChecked = (id:string) => this.selected().has(id);
  toggle(id:string, checked:boolean){
    const next = new Set(this.selected());
    if(checked) next.add(id); else next.delete(id);
    this.selected.set(next);
  }
  toggleGroup(groupPerms: PermissionDto[], checked:boolean){
    const next = new Set(this.selected());
    for (const p of groupPerms) { if(checked) next.add(p.id); else next.delete(p.id); }
    this.selected.set(next);
  }
  isGroupAllChecked(groupPerms: PermissionDto[]){
    return groupPerms.length>0 && groupPerms.every(p => this.selected().has(p.id));
  }
  isGroupSomeChecked(groupPerms: PermissionDto[]){
    const c = groupPerms.filter(p => this.selected().has(p.id)).length;
    return c>0 && c < groupPerms.length;
  }

  saveMutation = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.roleService.updateRolePermissions(this.orgId(), this.roleId(), Array.from(this.selected()))),
    onSuccess: (res:any) => {
      this.qc.invalidateQueries({queryKey:['role-permissions']});
      this.qc.invalidateQueries({queryKey:['org-roles']});
      const ids: string[] = res.permissionIds || res.PermissionIds || Array.from(this.selected());
      this.selected.set(new Set(ids));
      this.toast.success('Permissions updated');
    },
    onError: (e:any) => this.toast.error(e.error?.error || 'Save failed')
  }));

  doSave(){ this.saveMutation.mutate(); }
}
