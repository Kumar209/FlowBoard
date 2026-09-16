import { Component, ChangeDetectionStrategy, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationDetailModalComponent } from '../notification-detail-modal/notification-detail-modal.component';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-notification-list',
  standalone: true,
  imports: [CommonModule, NotificationDetailModalComponent],
  templateUrl: './notification-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationListComponent {
  private notificationService = inject(NotificationService);
  private queryClient = inject(QueryClient);

  page = signal(1);
  pageSize = signal(10);
  search = signal('');
  unreadOnly = signal(false);
  selected = signal<any>(null);
  detailOpen = signal(false);

  query = injectQuery(() => ({
    queryKey: [
      'notifications',
      'list',
      this.page(),
      this.pageSize(),
      this.unreadOnly(),
      this.search()
    ] as const,
    queryFn: () => firstValueFrom(
      this.notificationService.getNotifications(
        this.page(),
        this.pageSize(),
        this.unreadOnly() || undefined,
        this.search() || undefined
      )
    ),
  }));

  filtered = computed(() => {
    const data: any = this.query.data();
    if (!data?.items) return [];

    const s = this.search().toLowerCase().trim();

    if (!s) return data.items;

    return data.items.filter((n: any) =>
      n.action.toLowerCase().includes(s) ||
      n.payloadJson.toLowerCase().includes(s)
    );
  });

  total = computed(() => (this.query.data() as any)?.total || 0);

  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize()))
  );

  showingFrom = computed(() =>
    this.total() === 0
      ? 0
      : (this.page() - 1) * this.pageSize() + 1
  );

  showingTo = computed(() =>
    Math.min(this.page() * this.pageSize(), this.total())
  );

  visiblePages(): number[] {
    const total = this.totalPages();
    const current = this.page();

    if (total <= 3) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    if (current <= 2) {
      return [1, 2, 3];
    }

    if (current >= total - 1) {
      return [total - 2, total - 1, total];
    }

    return [current - 1, current, current + 1];
  }

  markReadMut = injectMutation(() => ({
    mutationFn: (id: string) =>
      firstValueFrom(this.notificationService.markRead(id)),
    onSuccess: () =>
      this.queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  }));

  markAllMut = injectMutation(() => ({
    mutationFn: () =>
      firstValueFrom(this.notificationService.markAllRead()),
    onSuccess: () =>
      this.queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  }));

  getDisplayAction(action: string): string {
    return (action || '').replace(/^Task/, 'Issue');
  }

  parsePayload(n: any): any {
    try {
      return JSON.parse(n.payloadJson || '{}');
    } catch {
      return {};
    }
  }

  getTaskDisplay(n: any): string {
    if (n.taskTitle && n.taskTitle.trim() !== '') {
      return n.taskTitle;
    }

    const p = this.parsePayload(n);

    return p.TaskTitle ||
      p.Title ||
      p.title ||
      (n.taskId ? n.taskId.slice(0, 8) : '');
  }

  getProjectDisplay(n: any): string {
    if (n.projectName && n.projectName.trim() !== '') {
      return n.projectName;
    }

    const p = this.parsePayload(n);

    return p.ProjectName ||
      p.ProjectKey ||
      p.projectKey ||
      (n.projectId ? n.projectId.slice(0, 8) : '');
  }

  getActorDisplay(n: any): string {
    if (n.actorName && n.actorName.trim() !== '') {
      return n.actorName;
    }

    const p = this.parsePayload(n);

    return p.ActorName ||
      p.actorName ||
      p.FullName ||
      (n.actorUserId ? n.actorUserId.slice(0, 8) : '');
  }

  getActionBadge(n: any): string {
    return n.action;
  }

  openDetail(n: any) {
    this.selected.set(n);
    this.detailOpen.set(true);

    if (!n.isRead) {
      firstValueFrom(
        this.notificationService.markRead(n.id)
      ).then(() => {
        this.queryClient.invalidateQueries({
          queryKey: ['notifications']
        });
      });
    }
  }

  firstPage() {
    if (this.page() > 1) {
      this.page.set(1);
    }
  }

  previousPage() {
    if (this.page() > 1) {
      this.page.update(v => v - 1);
    }
  }

  nextPage() {
    if (this.page() < this.totalPages()) {
      this.page.update(v => v + 1);
    }
  }

  lastPage() {
    if (this.page() < this.totalPages()) {
      this.page.set(this.totalPages());
    }
  }

  onPageSizeChange(value: any) {
    this.pageSize.set(Number(value) || 10);
    this.page.set(1);
  }
}