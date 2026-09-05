import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ThemeService } from '../../../core/services/theme.service';
import { AuthService } from '../../../core/services/auth.service';

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
  mobileOpen = signal(false);

  toggleMobile() { this.mobileOpen.update(v => !v); }
  closeMobile() { this.mobileOpen.set(false); }

  logout() {
    this.closeMobile();
    this.auth.logout().subscribe({
      complete: () => { this.auth.clearSession(); this.router.navigate(['/login']); },
      error: () => { this.auth.clearSession(); this.router.navigate(['/login']); }
    });
  }
}
