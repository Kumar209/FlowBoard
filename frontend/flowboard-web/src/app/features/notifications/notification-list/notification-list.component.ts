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

  openDetail(n: any) { this.selected.set(n); this.detailOpen.set(true); }
  next() { if (this.page() < this.totalPages()) this.page.update(v => v + 1); }
  prev() { if (this.page() > 1) this.page.update(v => v - 1); }
}
