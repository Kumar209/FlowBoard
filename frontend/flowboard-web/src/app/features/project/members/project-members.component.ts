import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmDeleteComponent } from '../../../shared/components/modals/confirm-delete/confirm-delete.component';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-project-members',
  standalone: true,
  imports: [CommonModule, ConfirmDeleteComponent],
  templateUrl: './project-members.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectMembersComponent {
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

  constructor() {
    this.route.paramMap.subscribe(m => {
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

  search = signal('');
  addSearch = signal('');
  page = signal(1);
  pageSize = signal(10);
  addPage = signal(1);
  addPageSize = signal(10);

  membersQuery = injectQuery(() => ({
    queryKey: [
      'project-members',
      this.projectId(),
      this.search(),
      this.page(),
      this.pageSize()
    ] as const,
    queryFn: async () => {
      const res: any = await firstValueFrom(
        this.ps.getProjectMembers(
          this.projectId(),
          this.page(),
          this.pageSize(),
          this.search() || undefined
        )
      );

      return res;
    },
    enabled: !!this.projectId()
  }));

  workspaceMembersQuery = injectQuery(() => ({
    queryKey: [
      'workspace-members',
      this.workspaceId(),
      this.addSearch(),
      this.addPage(),
      this.addPageSize()
    ] as const,
    queryFn: async () => {
      const res: any = await firstValueFrom(
        this.ps.getWorkspaceMembersPaged(
          this.workspaceId(),
          this.addPage(),
          this.addPageSize(),
          this.addSearch() || undefined
        )
      );

      if (res.items) {
        return {
          items: res.items as any[],
          total: res.total as number
        };
      }

      const arr = res as any[];

      return {
        items: arr,
        total: arr.length
      };
    },
    enabled: !!this.workspaceId()
  }));

  workspaceItems = computed(
    () => this.workspaceMembersQuery.data()?.items || []
  );

  workspaceTotal = computed(
    () => this.workspaceMembersQuery.data()?.total || 0
  );

  paginatedMembers = computed(
    () => this.membersQuery.data()?.items || []
  );

  totalMembers = computed(
    () => this.membersQuery.data()?.total || 0
  );

  totalPages = computed(() =>
    Math.max(
      1,
      Math.ceil(
        this.totalMembers() / this.pageSize()
      )
    )
  );

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

    const end = Math.min(
      total,
      start + 2
    );

    start = Math.max(
      1,
      end - 2
    );

    return Array.from(
      { length: end - start + 1 },
      (_, i) => start + i
    );
  });

  showingFrom = computed(() => {
    const total = this.totalMembers();

    if (!total) return 0;

    return (
      (this.page() - 1) *
        this.pageSize() +
      1
    );
  });

  showingTo = computed(() =>
    Math.min(
      this.page() * this.pageSize(),
      this.totalMembers()
    )
  );

  filteredAvailable = computed(() => {
    const list = this.workspaceItems();

    const existing = new Set(
      this.paginatedMembers().map(
        (m: any) => m.userId
      )
    );

    return list.filter(
      (m: any) => !existing.has(m.userId)
    );
  });

  totalAddPages = computed(() =>
    Math.max(
      1,
      Math.ceil(
        this.workspaceTotal() /
          this.addPageSize()
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

    const end = Math.min(
      total,
      start + 2
    );

    start = Math.max(
      1,
      end - 2
    );

    return Array.from(
      { length: end - start + 1 },
      (_, i) => start + i
    );
  });

  addShowingFrom = computed(() => {
    const total = this.workspaceTotal();

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
      this.workspaceTotal()
    )
  );

  addMutation = injectMutation(() => ({
    mutationFn: (userId: string) =>
      firstValueFrom(
        this.ps.addProjectMember(
          this.projectId(),
          userId
        )
      ),
    onSuccess: () => {
      this.qc.invalidateQueries({
        queryKey: ['project-members', this.projectId()]
      });

      this.qc.invalidateQueries({
        queryKey: ['workspace-members', this.workspaceId()]
      });

      this.toast.success(
        'Member added to project'
      );

      this.addSearch.set('');
      this.addPage.set(1);
    },
    onError: (e: any) =>
      this.toast.error(
        e.error?.error || 'Add failed'
      )
  }));

  removeMutation = injectMutation(() => ({
    mutationFn: (userId: string) =>
      firstValueFrom(
        this.ps.removeProjectMember(
          this.projectId(),
          userId
        )
      ),
    onSuccess: () => {
      this.qc.invalidateQueries({
        queryKey: ['project-members', this.projectId()]
      });

      this.qc.invalidateQueries({
        queryKey: ['workspace-members', this.workspaceId()]
      });

      this.toast.success(
        'Member removed'
      );
    },
    onError: (e: any) =>
      this.toast.error(
        e.error?.error || 'Remove failed'
      )
  }));

  deleteConfirmUserId =
    signal<string | null>(null);

  add(userId: string) {
    this.addMutation.mutate(userId);
  }

  remove(userId: string) {
    this.deleteConfirmUserId.set(userId);
  }

  confirmRemove() {
    const id = this.deleteConfirmUserId();

    if (!id) return;

    this.removeMutation.mutate(id);
    this.deleteConfirmUserId.set(null);
  }

  cancelRemove() {
    this.deleteConfirmUserId.set(null);
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
    this.page.set(1);
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

  onSearchChange(value: string) {
    this.search.set(value);
    this.page.set(1);
  }

  onAddSearchChange(value: string) {
    this.addSearch.set(value);
    this.addPage.set(1);
  }
}