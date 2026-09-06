import { Component, inject, signal, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { HeaderComponent } from '../header/header.component';
import { ToastComponent } from '../toast/toast.component';
import { AuthService } from '../../../core/services/auth.service';
import { filter } from 'rxjs';

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
  private router = inject(Router);
  sidebarOpen = signal(false);
  mainCollapsed = signal(localStorage.getItem('mainSidebarCollapsed') === '1');
  isProjectRoute = signal(this.router.url.includes('/p/'));

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
    this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe((e:any) => {
      const isProj = e.urlAfterRedirects.includes('/p/');
      this.isProjectRoute.set(isProj);
      // Auto-expand main sidebar when leaving project (Image 7 fix - don't keep collapsed on Workspaces)
      if (!isProj && this.mainCollapsed()) {
        this.mainCollapsed.set(false);
        localStorage.setItem('mainSidebarCollapsed', '0');
      }
    });
  }

  toggle() { this.sidebarOpen.update(v => !v); }
  close() { this.sidebarOpen.set(false); }
  toggleMainCollapse() {
    const v = !this.mainCollapsed();
    this.mainCollapsed.set(v);
    localStorage.setItem('mainSidebarCollapsed', v ? '1' : '0');
  }
}
