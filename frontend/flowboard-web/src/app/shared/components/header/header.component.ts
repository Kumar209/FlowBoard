import { Component, inject, signal, computed, ChangeDetectionStrategy, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ThemeService } from '../../../core/services/theme.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SuperAdminService } from '../../../core/services/superadmin.service';
import { NotificationDetailModalComponent } from '../../../features/notifications/notification-detail-modal/notification-detail-modal.component';
import { injectQuery, QueryClient } from '@tanstack/angular-query-experimental';

/**
 * HeaderComponent - OnPush + inject() + signal mobileOpen + computed theme.
 * OnPush + signals gives fine-grained updates (only when mobileOpen/theme/currentUser changes), not full app tick.
 */
@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, NotificationDetailModalComponent],
  templateUrl: './header.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderComponent {
  themeService = inject(ThemeService);
  auth = inject(AuthService);
  router = inject(Router);
  notificationService = inject(NotificationService);
  private superAdminService = inject(SuperAdminService);
  private queryClient = inject(QueryClient);

  mobileMenuRequested = output<void>();

  platformQuery = injectQuery(() => ({
    queryKey: ['platform-general'] as const,
    queryFn: () => firstValueFrom(this.superAdminService.getPlatformGeneral()),
    staleTime: 5 * 60 * 1000,
  }));

  mobileOpen = signal(false);
  notifOpen = signal(false);
  selectedNotif = signal<any>(null);
  notifDetailOpen = signal(false);

  notificationsQuery = injectQuery(() => ({
    queryKey: ['notifications', 'header', 1] as const,
    queryFn: () => firstValueFrom(this.notificationService.getNotifications(1, 5)),
    enabled: this.auth.isAuthenticated() && !this.auth.isSuperAdmin(),
    staleTime: 30 * 1000,
  }));

  complaintsQuery = injectQuery(() => ({
    queryKey: ['superadmin-complaints', 'header'] as const,
    queryFn: () => firstValueFrom(this.superAdminService.getSuperAdminComplaints()),
    enabled: this.auth.isAuthenticated() && this.auth.isSuperAdmin(),
    staleTime: 30 * 1000,
  }));

  getComplaintsList(): any[] {
    const d: any = this.complaintsQuery.data();
    if (!d) return [];
    if (Array.isArray(d)) return d;
    return d.items ?? [];
  }
  unread = computed(() => {
    if (this.auth.isSuperAdmin()) {
      const list: any = this.getComplaintsList();
      return list.filter((c: any) => c.status === 'Open').length;
    }
    const d: any = this.notificationsQuery.data();
    if (!d?.items) return 0;
    return d.items.filter((n: any) => !n.isRead).length;
  });

  superAdminUnreadLabel = computed(() => {
    const n = this.unread();
    if (n === 0) return '';
    return n > 9 ? '9+' : String(n);
  });

  requestMobileMenu() {
    this.mobileOpen.update(v => !v);
    this.mobileMenuRequested.emit();
  }

  toggleMobile() {
    this.mobileOpen.update(v => !v);
    this.mobileMenuRequested.emit();
  }

  closeMobile() {
    this.mobileOpen.set(false);
  }

  toggleNotif() {
    this.notifOpen.update(v => !v);
  }

  closeNotif() {
    this.notifOpen.set(false);
  }

  formatNotif(n: any): string {
    try {
      const p = JSON.parse(n.payloadJson || '{}');
      const title = n.taskTitle || p.TaskTitle || p.Title || p.title || '';
      const from = p.FromListName || p.fromListName || p.fromList || '';
      const to = p.ToListName || p.toListName || p.toList || '';
      const board = p.BoardName || p.boardName || '';
      const actor = n.actorName || p.ActorName || '';
      const boardPart = board ? ` in ${board}` : '';
      const displayAction = (n.action || '').replace(/^Task/, 'Issue');

      if (n.action === 'TaskMoved' && title) return `${actor ? actor + ' ' : ''}moved "${title}" ${from ? 'from ' + from : ''} → ${to}${boardPart}`;
      if (n.action === 'TaskCreated' && title) return `${actor ? actor + ' ' : ''}created an issue "${title}"${boardPart}`;
      if (n.action === 'TaskCommented' && title) {
        const preview = p.CommentContent || p.commentPreview || '';
        if (preview) return `${actor ? actor + ' ' : ''}commented on "${title}": "${preview.slice(0, 30)}"`;
        return `${actor ? actor + ' ' : ''}commented on "${title}"`;
      }
      if (title) return title;
      return displayAction;
    } catch {
      return (n.action || '').replace(/^Task/, 'Issue');
    }
  }

  openNotifDetail(n: any) {
    this.selectedNotif.set(n);
    this.notifDetailOpen.set(true);
    this.notifOpen.set(false);

    if (!n.isRead) {
      firstValueFrom(this.notificationService.markRead(n.id)).then(() => {
        this.queryClient.invalidateQueries({ queryKey: ['notifications'] });
      });
    }
  }

  markRead(id: string) {
    firstValueFrom(this.notificationService.markRead(id)).then(() => {
      this.queryClient.invalidateQueries({ queryKey: ['notifications'] });
    });
  }

  logout() {
    this.closeMobile();
    this.auth.logout().subscribe({
      complete: () => {
        this.auth.clearSession();
        this.router.navigate(['/login']);
      },
      error: () => {
        this.auth.clearSession();
        this.router.navigate(['/login']);
      }
    });
  }
}