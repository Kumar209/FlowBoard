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
    queryKey: ['notifications', 'list', this.page(), this.pageSize(), this.unreadOnly(), this.search()] as const,
    queryFn: () => firstValueFrom(this.notificationService.getNotifications(this.page(), this.pageSize(), this.unreadOnly() || undefined, this.search() || undefined)),
  }));

  filtered = computed(() => {
    const data: any = this.query.data();
    if (!data?.items) return [];
    const s = this.search().toLowerCase().trim();
    if (!s) return data.items;
    return data.items.filter((n: any) => n.action.toLowerCase().includes(s) || n.payloadJson.toLowerCase().includes(s));
  });

  total = computed(() => (this.query.data() as any)?.total || 0);
  totalPages = computed(() => Math.ceil(this.total() / this.pageSize()));

  markReadMut = injectMutation(() => ({
    mutationFn: (id: string) => firstValueFrom(this.notificationService.markRead(id)),
    onSuccess: () => this.queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  }));

  markAllMut = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.notificationService.markAllRead()),
    onSuccess: () => this.queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  }));

  parsePayload(n: any): any {
    try { return JSON.parse(n.payloadJson || '{}'); } catch { return {}; }
  }
  getTaskDisplay(n: any): string {
    const p = this.parsePayload(n);
    return p.TaskTitle || p.Title || p.title || n.taskId.slice(0,8);
  }
  getProjectDisplay(n: any): string {
    const p = this.parsePayload(n);
    return p.ProjectKey || p.projectKey || n.projectId.slice(0,8);
  }
  getActorDisplay(n: any): string {
    const p = this.parsePayload(n);
    return p.ActorName || p.actorName || p.FullName || n.actorUserId.slice(0,8);
  }
  getActionBadge(n: any): string {
    // hide empty handling: payload already has names
    return n.action;
  }

  openDetail(n: any) {
    this.selected.set(n);
    this.detailOpen.set(true);
    if (!n.isRead) {
      firstValueFrom(this.notificationService.markRead(n.id)).then(() => {
        this.queryClient.invalidateQueries({ queryKey: ['notifications'] });
      });
    }
  }
  next() { if (this.page() < this.totalPages()) this.page.update(v => v + 1); }
  prev() { if (this.page() > 1) this.page.update(v => v - 1); }
}
