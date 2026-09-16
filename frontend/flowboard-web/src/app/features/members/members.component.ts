import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { WorkspaceService } from '../../core/services/workspace.service';
import { OrganizationRoleService } from '../../core/services/organization-role.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { ROLE_LABEL_MAP, OrgRoleValues } from '../../shared/constants/roles';

@Component({
  selector: 'app-members',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './members.component.html',
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
  pageSize = signal(10);

  showInvite = signal(false);
  inviteFullName = signal('');
  inviteEmail = signal('');
  invitePassword = signal('');
  showInvitePassword = signal(false);

  OrgRoleValues = OrgRoleValues;
  inviteRole = signal<number>(OrgRoleValues.Member);
  inviteWorkspaceIds = signal<string[]>([]);
  inviteWorkspaceRoles = signal<Record<string, string>>({});

  editTarget = signal<any>(null);
  editName = signal('');
  editEmail = signal('');
  editRole = signal<number>(OrgRoleValues.Member);
  editWorkspaceIds = signal<string[]>([]);
  editWorkspaceRoles = signal<Record<string, string>>({});

  deleteConfirmMember = signal<any | null>(null);

  workspacesQuery = injectQuery(() => ({
    queryKey: ['workspaces'] as const,
    queryFn: () => firstValueFrom(this.ws.getMyWorkspaces()),
  }));

  orgId = computed(() => {
    const ws = this.workspacesQuery.data() || [];
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

  workspaceMembersAggQuery = injectQuery(() => ({
    queryKey: ['agg-members'] as const,
    queryFn: async () => {
      const wss = this.workspacesQuery.data() || [];
      const all: any[] = [];

      for (const w of wss) {
        try {
          const ms = await firstValueFrom(this.ws.getWorkspaceMembers(w.id));
          all.push(...ms.map((m: any) => ({ ...m, workspaceName: w.name })));
        } catch {}
      }

      const map = new Map();

      for (const m of all) {
        if (!map.has(m.userId)) map.set(m.userId, m);
      }

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

    return list.filter((m: any) =>
      m.fullName?.toLowerCase().includes(q) ||
      m.email?.toLowerCase().includes(q) ||
      (m.role || '').toLowerCase().includes(q)
    );
  });

  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filtered().length / this.pageSize()))
  );

  paginated = computed(() => {
    const start = (this.page() - 1) * this.pageSize();
    return this.filtered().slice(start, start + this.pageSize());
  });

  showingFrom = computed(() => {
    const total = this.filtered().length;
    return total === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1;
  });

  showingTo = computed(() =>
    Math.min(this.page() * this.pageSize(), this.filtered().length)
  );

  visiblePages(): number[] {
    const total = this.totalPages();
    const current = this.page();

    if (total <= 3) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    if (current <= 2) return [1, 2, 3];

    if (current >= total - 1) return [total - 2, total - 1, total];

    return [current - 1, current, current + 1];
  }

  setPage(value: number) {
    const next = Math.min(Math.max(value, 1), this.totalPages());
    this.page.set(next);
  }

  firstPage() {
    this.page.set(1);
  }

  previousPage() {
    if (this.page() > 1) this.page.set(this.page() - 1);
  }

  nextPage() {
    if (this.page() < this.totalPages()) this.page.set(this.page() + 1);
  }

  lastPage() {
    this.page.set(this.totalPages());
  }

  setPageSize(value: number) {
    this.pageSize.set(Number(value) || 10);
    this.page.set(1);
  }

  onSearch(value: string) {
    this.search.set(value);
    this.page.set(1);
  }

  canInvite = computed(() =>
    this.auth.isOrgAdmin() || this.auth.isSuperAdmin()
  );

  inviteMutation = injectMutation(() => ({
    mutationFn: () => {
      const orgId = this.orgId();

      if (!orgId) throw new Error('No organization');

      const orgRole: number = this.inviteRole();
      let roles: any[] = [];

      if (orgRole !== OrgRoleValues.OrgAdmin) {
        const wids = this.inviteWorkspaceIds();
        const customRoles = (this.customRolesQuery.data() as any[]) || [];

        roles = wids.map(id => {
          const selectedId = this.inviteWorkspaceRoles()[id];
          const cr =
            customRoles.find((c: any) => c.id === selectedId) ||
            customRoles.find((c: any) => c.name === selectedId);

          const name =
            cr?.name ||
            selectedId ||
            ROLE_LABEL_MAP[String(OrgRoleValues.Member)];

          return {
            workspaceId: id,
            role: name,
            customRoleId: cr?.id || selectedId || undefined
          };
        });
      }

      return firstValueFrom(
        this.ws.createOrganizationMember(
          orgId,
          this.inviteFullName().trim(),
          this.inviteEmail().trim(),
          this.invitePassword().trim(),
          roles as any,
          orgRole
        )
      );
    },
    onSuccess: () => {
      this.qc.invalidateQueries({ queryKey: ['org-members'] });
      this.qc.invalidateQueries({ queryKey: ['agg-members'] });

      this.showInvite.set(false);
      this.inviteFullName.set('');
      this.inviteEmail.set('');
      this.invitePassword.set('');
      this.inviteWorkspaceIds.set([]);

      this.toast.success('Employee created');
    },
    onError: (e: any) =>
      this.toast.error(e.error?.error || 'Create failed')
  }));

  removeMutation = injectMutation(() => ({
    mutationFn: (userId: string) =>
      firstValueFrom(
        this.ws.deleteOrganizationMember(this.orgId(), userId)
      ),
    onSuccess: () => {
      this.qc.invalidateQueries({ queryKey: ['org-members'] });
      this.qc.invalidateQueries({ queryKey: ['agg-members'] });
      this.toast.success('Member removed');
    },
    onError: (e: any) =>
      this.toast.error(e.error?.error || 'Remove failed')
  }));

  updateMutation = injectMutation(() => ({
    mutationFn: () => {
      const orgRole: number = this.editRole();
      let roles: any[] = [];

      if (orgRole !== OrgRoleValues.OrgAdmin) {
        const wids = this.editWorkspaceIds();
        const customRoles = (this.customRolesQuery.data() as any[]) || [];

        roles = wids.map(id => {
          const selectedId = this.editWorkspaceRoles()[id];
          const cr =
            customRoles.find((c: any) => c.id === selectedId) ||
            customRoles.find((c: any) => c.name === selectedId);

          const name =
            cr?.name ||
            selectedId ||
            ROLE_LABEL_MAP[String(OrgRoleValues.Member)];

          return {
            workspaceId: id,
            role: name,
            customRoleId: cr?.id || selectedId || undefined
          };
        });
      }

      return firstValueFrom(
        this.ws.updateOrganizationMember(
          this.orgId(),
          this.editTarget()!.userId,
          this.editName().trim() || undefined,
          this.editEmail().trim() || undefined,
          roles as any,
          orgRole
        )
      );
    },
    onSuccess: () => {
      this.qc.invalidateQueries({ queryKey: ['org-members'] });
      this.qc.invalidateQueries({ queryKey: ['agg-members'] });
      this.editTarget.set(null);
      this.toast.success('Member updated');
    },
    onError: (e: any) =>
      this.toast.error(e.error?.error || 'Update failed')
  }));

  openInvite() {
    const wss = this.workspacesQuery.data() || [];

    this.inviteFullName.set('');
    this.inviteEmail.set('');
    this.invitePassword.set('');
    this.inviteRole.set(OrgRoleValues.Member);
    this.showInvitePassword.set(false);

    const customRoles = (this.customRolesQuery.data() as any[]) || [];

    if (customRoles.length === 0) {
      this.inviteWorkspaceIds.set([]);
      this.inviteWorkspaceRoles.set({});
      this.showInvite.set(true);
      return;
    }

    const dev =
      wss.find((w: any) =>
        w.name.toLowerCase().includes('development')
      ) || wss[0];

    const ids = dev
      ? [dev.id]
      : wss.slice(0, 1).map((w: any) => w.id);

    const defaultId = customRoles[0]?.id || '';
    const map: Record<string, string> = {};

    ids.forEach(id => {
      map[id] = defaultId;
    });

    this.inviteWorkspaceIds.set(ids);
    this.inviteWorkspaceRoles.set(map);
    this.showInvite.set(true);
  }

  doInvite() {
    if (
      !this.inviteFullName().trim() ||
      !this.inviteEmail().trim() ||
      !this.invitePassword().trim()
    ) return;

    this.inviteMutation.mutate();
  }

  confirmRemove(m: any) {
    this.deleteConfirmMember.set(m);
  }

  cancelDelete() {
    this.deleteConfirmMember.set(null);
  }

  doDelete() {
    const m = this.deleteConfirmMember();

    if (!m) return;

    this.removeMutation.mutate(m.userId);
    this.deleteConfirmMember.set(null);
  }

  onInviteWorkspaceChecked(
    workspaceId: string,
    checked: boolean
  ) {
    if (checked) {
      const customRoles = (this.customRolesQuery.data() as any[]) || [];
      const defaultId = customRoles[0]?.id || '';

      this.inviteWorkspaceIds.set([
        ...this.inviteWorkspaceIds(),
        workspaceId
      ]);

      this.inviteWorkspaceRoles.set({
        ...this.inviteWorkspaceRoles(),
        [workspaceId]: defaultId
      });
    } else {
      this.inviteWorkspaceIds.set(
        this.inviteWorkspaceIds().filter(id => id !== workspaceId)
      );

      const copy = { ...this.inviteWorkspaceRoles() };
      delete copy[workspaceId];
      this.inviteWorkspaceRoles.set(copy);
    }
  }

  onInviteRoleChange(workspaceId: string, role: string) {
    this.inviteWorkspaceRoles.set({
      ...this.inviteWorkspaceRoles(),
      [workspaceId]: role
    });
  }

  onEditWorkspaceChecked(
    workspaceId: string,
    checked: boolean
  ) {
    if (checked) {
      const customRoles = (this.customRolesQuery.data() as any[]) || [];
      const defaultId = customRoles[0]?.id || '';

      this.editWorkspaceIds.set([
        ...this.editWorkspaceIds(),
        workspaceId
      ]);

      this.editWorkspaceRoles.set({
        ...this.editWorkspaceRoles(),
        [workspaceId]: defaultId
      });
    } else {
      this.editWorkspaceIds.set(
        this.editWorkspaceIds().filter(id => id !== workspaceId)
      );

      const copy = { ...this.editWorkspaceRoles() };
      delete copy[workspaceId];
      this.editWorkspaceRoles.set(copy);
    }
  }

  onEditRoleChange(workspaceId: string, role: string) {
    this.editWorkspaceRoles.set({
      ...this.editWorkspaceRoles(),
      [workspaceId]: role
    });
  }

  isEditRoleDeleted(wsId: string): boolean {
    const roles = (this.customRolesQuery.data() as any[]) || [];
    const sel = this.editWorkspaceRoles()[wsId];

    return !!sel && !roles.some((c: any) => c.id === sel);
  }

  isInviteRoleDeleted(wsId: string): boolean {
    const roles = (this.customRolesQuery.data() as any[]) || [];
    const sel = this.inviteWorkspaceRoles()[wsId];

    return !!sel && !roles.some((c: any) => c.id === sel);
  }

  openEdit(m: any) {
    this.editTarget.set(m);
    this.editName.set(m.fullName);
    this.editEmail.set(m.email);

    const roleMap: Record<string, number> = {
      Member: 1,
      OrgAdmin: 2,
      Client: 3,
      SuperAdmin: 0
    };

    const rawInt =
      (m as any).roleInt ??
      (m.role
        ? roleMap[m.role] ?? OrgRoleValues.Member
        : OrgRoleValues.Member);

    const mappedInt = [
      OrgRoleValues.Member,
      OrgRoleValues.OrgAdmin,
      OrgRoleValues.Client,
      OrgRoleValues.SuperAdmin
    ].includes(rawInt)
      ? rawInt
      : OrgRoleValues.Member;

    this.editRole.set(mappedInt);

    const wids = (m as any).workspaceIds as string[] | undefined;

    const allWids =
      wids && wids.length
        ? wids
        : (m as any).workspaceId
          ? [(m as any).workspaceId]
          : [];

    const ids = allWids.length
      ? allWids
      : (this.workspacesQuery.data()?.slice(0, 1).map((w: any) => w.id) || []);

    const customRoles = (this.customRolesQuery.data() as any[]) || [];

    this.editWorkspaceIds.set(
      customRoles.length ? ids : []
    );

    const map: Record<string, string> = {};

    const wsRoleIdMap =
      (m as any).workspaceRoleIdMap as Record<string, string> | undefined;

    const wsRoleMap =
      (m as any).workspaceRoleMap as Record<string, string> | undefined;

    ids.forEach(id => {
      let perWsId =
        wsRoleIdMap?.[id] ||
        wsRoleIdMap?.[id.toLowerCase()] ||
        '';

      if (!perWsId) {
        const perWsName =
          wsRoleMap?.[id] ||
          wsRoleMap?.[id.toLowerCase()] ||
          '';

        if (perWsName) {
          const found = customRoles.find(
            (c: any) =>
              c.name === perWsName ||
              c.name.toLowerCase() === perWsName.toLowerCase()
          );

          perWsId = found?.id || '';
        }
      }

      map[id] = perWsId;
    });

    if (!customRoles.length) {
      this.editWorkspaceIds.set([]);
    }

    this.editWorkspaceRoles.set(map);
  }

  saveEdit() {
    const target = this.editTarget();

    if (!target) return;

    this.updateMutation.mutate();
  }
}