import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ThemeService } from '../../../core/services/theme.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { injectQuery, QueryClient } from '@tanstack/angular-query-experimental';

/**
 * HeaderComponent - MNC-grade: OnPush + inject() + signal mobileOpen + computed theme.
 * OnPush + signals gives fine-grained updates (only when mobileOpen/theme/currentUser changes), not full app tick.
 */
@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderComponent {
  themeService = inject(ThemeService);
  auth = inject(AuthService);
  router = inject(Router);
  notificationService = inject(NotificationService);
  private queryClient = inject(QueryClient);
  mobileOpen = signal(false);
  notifOpen = signal(false);

  notificationsQuery = injectQuery(() => ({
    queryKey: ['notifications', 'header', 1] as const,
    queryFn: () => firstValueFrom(this.notificationService.getNotifications(1, 5)),
    enabled: this.auth.isAuthenticated(),
    staleTime: 30 * 1000,
  }));
  
  unread = computed(() => {
    const d: any = this.notificationsQuery.data();
    if (!d?.items) return 0;
    return d.items.filter((n: any) => !n.isRead).length;
  });

  toggleMobile() { this.mobileOpen.update(v => !v); }
  closeMobile() { this.mobileOpen.set(false); }
  toggleNotif() { this.notifOpen.update(v => !v); }
  closeNotif() { this.notifOpen.set(false); }

  markRead(id: string) {
    firstValueFrom(this.notificationService.markRead(id)).then(() => {
      this.queryClient.invalidateQueries({ queryKey: ['notifications'] });
    });
  }

  logout() {
    this.closeMobile();
    this.auth.logout().subscribe({
      complete: () => { this.auth.clearSession(); this.router.navigate(['/login']); },
      error: () => { this.auth.clearSession(); this.router.navigate(['/login']); }
    });
  }
}
