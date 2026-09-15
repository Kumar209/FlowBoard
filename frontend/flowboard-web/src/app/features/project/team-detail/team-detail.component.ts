import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmDeleteComponent } from '../../../shared/components/modals/confirm-delete/confirm-delete.component';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { ROLE_LABEL_MAP, OrgRoleValues } from '../../../shared/constants/roles';

@Component({
  selector: 'app-team-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ConfirmDeleteComponent],
  templateUrl: './team-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TeamDetailComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);

  projectId = signal(
    this.route.snapshot.paramMap.get('pid') ||
    this.route.parent?.snapshot.paramMap.get('pid') ||
    ''
  );

  workspaceId = signal(
    this.route.snapshot.paramMap.get('wid') ||
    this.route.parent?.snapshot.paramMap.get('wid') ||
    ''
  );

  teamId = signal(this.route.snapshot.paramMap.get('teamId') || '');

  constructor() {
    this.route.paramMap.subscribe(m => {
      const tid = m.get('teamId');
      if (tid) this.teamId.set(tid);

      const pid = m.get('pid');
      if (pid) this.projectId.set(pid);

      const wid = m.get('wid');
      if (wid) this.workspaceId.set(wid);
    });

    this.route.parent?.paramMap.subscribe(m => {
      const pid = m.get('pid');
      if (pid) this.projectId.set(pid);

      const wid = m.get('wid');
      if (wid) this.workspaceId.set(wid);
    });
  }

  teamQuery = injectQuery(() => ({
    queryKey: ['teams', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getTeams(this.projectId())),
    enabled: !!this.projectId()
  }));

  team = computed(() =>
    (this.teamQuery.data() || []).find(
      (t: any) => t.id === this.teamId()
    )
  );

  membersQuery = injectQuery(() => ({
    queryKey: ['team-members', this.teamId()] as const,
    queryFn: () => firstValueFrom(this.ps.getTeamMembers(this.teamId())),
    enabled: !!this.teamId()
  }));

  search = signal('');
  page = signal(1);
  pageSize = signal(10);

  addSearch = signal('');
  selectedUserId = signal('');
  addPage = signal(1);
  addPageSize = signal(10);

  projectMembersQuery = injectQuery(() => ({
    queryKey: [
      'project-members',
      this.projectId(),
      this.addSearch(),
      this.addPage(),
      this.addPageSize()
    ] as const,
    queryFn: async () => {
      const res: any = await firstValueFrom(
        this.ps.getProjectMembers(
          this.projectId(),
          this.addPage(),
          this.addPageSize(),
          this.addSearch() || undefined
        )
      );

      return {
        items: (res.items || res.Items || []) as any[],
        total: (res.total || res.Total || 0) as number
      };
    },
    enabled: !!this.projectId()
  }));

  projectMembersItems = computed(
    () => this.projectMembersQuery.data()?.items || []
  );

  projectMembersTotal = computed(
    () => this.projectMembersQuery.data()?.total || 0
  );

  enrichedMembers = computed(() => {
    const teamMembers = this.membersQuery.data() || [];
    const projectMembers = this.projectMembersItems() as any[];

    const map = new Map<string, any>(
      projectMembers.map((m: any) => [m.userId, m])
    );

    return teamMembers.map((tm: any) => {
      const projectMember: any = map.get(tm.userId);

      return {
        ...tm,
        fullName:
          projectMember?.fullName ||
          tm.userId.slice(0, 8),
        email:
          projectMember?.email || '',
        role:
          projectMember?.role ||
          ROLE_LABEL_MAP[String(OrgRoleValues.Member)],
        avatarUrl:
          projectMember?.avatarUrl
      };
    });
  });

  filteredMembers = computed(() => {
    const q = this.search().toLowerCase().trim();
    const list = this.enrichedMembers();

    if (!q) return list;

    return list.filter(
      (m: any) =>
        m.fullName.toLowerCase().includes(q) ||
        m.email.toLowerCase().includes(q) ||
        m.role.toLowerCase().includes(q)
    );
  });

  totalPages = computed(() =>
    Math.max(
      1,
      Math.ceil(
        this.filteredMembers().length / this.pageSize()
      )
    )
  );

  paginatedMembers = computed(() => {
    const start =
      (this.page() - 1) * this.pageSize();

    return this.filteredMembers().slice(
      start,
      start + this.pageSize()
    );
  });

  visiblePages = computed(() => {
    const total = this.totalPages();
    const current = this.page();

    if (total <= 3) {
      return Array.from(
        { length: total },
        (_, i) => i + 1
      );
    }

    let start = Math.max(
      1,
      Math.min(current - 1, total - 2)
    );

    const end = Math.min(total, start + 2);
    start = Math.max(1, end - 2);

    return Array.from(
      { length: end - start + 1 },
      (_, i) => start + i
    );
  });

  showingFrom = computed(() => {
    const total = this.filteredMembers().length;

    if (!total) return 0;

    return (this.page() - 1) * this.pageSize() + 1;
  });

  showingTo = computed(() =>
    Math.min(
      this.page() * this.pageSize(),
      this.filteredMembers().length
    )
  );

  totalAddPages = computed(() =>
    Math.max(
      1,
      Math.ceil(
        this.projectMembersTotal() / this.addPageSize()
      )
    )
  );

  visibleAddPages = computed(() => {
    const total = this.totalAddPages();
    const current = this.addPage();

    if (total <= 3) {
      return Array.from(
        { length: total },
        (_, i) => i + 1
      );
    }

    let start = Math.max(
      1,
      Math.min(current - 1, total - 2)
    );

    const end = Math.min(total, start + 2);
    start = Math.max(1, end - 2);

    return Array.from(
      { length: end - start + 1 },
      (_, i) => start + i
    );
  });

  filteredWorkspaceMembers = computed(() => {
    const list = this.projectMembersItems();
    const teamMembers = this.membersQuery.data() || [];

    const existing = new Set(
      teamMembers.map((m: any) => m.userId)
    );

    return list.filter(
      (m: any) => !existing.has(m.userId)
    );
  });

  paginatedAvailableMembers = computed(() =>
    this.filteredWorkspaceMembers()
  );

  addShowingFrom = computed(() => {
    const total = this.projectMembersTotal();

    if (!total) return 0;

    return (
      (this.addPage() - 1) *
        this.addPageSize() +
      1
    );
  });

  addShowingTo = computed(() =>
    Math.min(
      this.addPage() * this.addPageSize(),
      this.projectMembersTotal()
    )
  );

  addMutation = injectMutation(() => ({
    mutationFn: () =>
      firstValueFrom(
        this.ps.addTeamMember(
          this.teamId(),
          this.selectedUserId()
        )
      ),
    onSuccess: () => {
      this.qc.invalidateQueries({
        queryKey: ['team-members', this.teamId()]
      });

      this.qc.invalidateQueries({
        queryKey: ['teams', this.projectId()]
      });

      this.qc.invalidateQueries({
        queryKey: ['project-members', this.projectId()]
      });

      this.toast.success('Member added');

      this.selectedUserId.set('');
      this.addSearch.set('');
    },
    onError: (e: any) =>
      this.toast.error(
        e.error?.error || 'Add failed'
      )
  }));

  removeMutation = injectMutation(() => ({
    mutationFn: (userId: string) =>
      firstValueFrom(
        this.ps.removeTeamMember(
          this.teamId(),
          userId
        )
      ),
    onSuccess: () => {
      this.qc.invalidateQueries({
        queryKey: ['team-members', this.teamId()]
      });

      this.qc.invalidateQueries({
        queryKey: ['teams', this.projectId()]
      });

      this.toast.success('Member removed');
    },
    onError: (e: any) =>
      this.toast.error(
        e.error?.error || 'Remove failed'
      )
  }));

  deleteConfirmMember = signal<any | null>(null);

  remove(member: any) {
    this.deleteConfirmMember.set(member);
  }

  cancelRemove() {
    this.deleteConfirmMember.set(null);
  }

  confirmRemove() {
    const member = this.deleteConfirmMember();

    if (!member) return;

    this.removeMutation.mutate(member.userId);
    this.deleteConfirmMember.set(null);
  }

  setPage(page: number) {
    const target = Math.max(
      1,
      Math.min(page, this.totalPages())
    );

    this.page.set(target);
  }

  firstPage() {
    this.setPage(1);
  }

  previousPage() {
    this.setPage(this.page() - 1);
  }

  nextPage() {
    this.setPage(this.page() + 1);
  }

  lastPage() {
    this.setPage(this.totalPages());
  }

  onPageSizeChange(value: string | number) {
    const size = Number(value);

    if (!size) return;

    this.pageSize.set(size);

    if (this.page() > this.totalPages()) {
      this.page.set(this.totalPages());
    }
  }

  setAddPage(page: number) {
    const target = Math.max(
      1,
      Math.min(page, this.totalAddPages())
    );

    this.addPage.set(target);
  }

  firstAddPage() {
    this.setAddPage(1);
  }

  previousAddPage() {
    this.setAddPage(this.addPage() - 1);
  }

  nextAddPage() {
    this.setAddPage(this.addPage() + 1);
  }

  lastAddPage() {
    this.setAddPage(this.totalAddPages());
  }

  onAddPageSizeChange(value: string | number) {
    const size = Number(value);

    if (!size) return;

    this.addPageSize.set(size);
    this.addPage.set(1);
  }

  resetMemberPage() {
    this.page.set(1);
  }

  resetAddPage() {
    this.addPage.set(1);
  }
}