import { Component, inject, signal, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { HeaderComponent } from '../header/header.component';
import { ToastComponent } from '../toast/toast.component';
import { AuthService } from '../../../core/services/auth.service';

/**
 * LayoutComponent - MNC-grade: 6-menu sidebar (removed single Workspace), role-based *ngIf, OnPush + signals + inject.
 * Dashboard/Workspaces/Projects visible to all authenticated; Activity/Members filtered; System only for OrgAdmin/SuperAdmin.
 * Hydrates memberships via me() on init so sidebar hides correctly after refresh.
 */
@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet, HeaderComponent, ToastComponent],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LayoutComponent implements OnInit {
  auth = inject(AuthService);
  sidebarOpen = signal(false);

  ngOnInit() {
    // In-memory fix: no sessionStorage, so after F5 token is null. Try silent refresh via HttpOnly cookie, then me.
    if (!this.auth.isAuthenticated()) {
      this.auth.refresh().subscribe({
        next: res => {
          this.auth.accessToken.set(res.accessToken);
          this.auth.me().subscribe({ next: m => this.auth.hydrateFromMe(m as any), error: () => {} });
        },
        error: () => {}
      });
    } else if (this.auth.memberships().length === 0) {
      this.auth.me().subscribe({
        next: res => this.auth.hydrateFromMe(res as any),
        error: () => {}
      });
    }
  }

  toggle() { this.sidebarOpen.update(v => !v); }
  close() { this.sidebarOpen.set(false); }
}
