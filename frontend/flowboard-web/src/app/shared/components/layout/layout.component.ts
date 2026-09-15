import { Component, inject, signal, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { HeaderComponent } from '../header/header.component';
import { ToastComponent } from '../toast/toast.component';
import { LoaderComponent } from '../loader/loader.component';
import { AuthService } from '../../../core/services/auth.service';
import { LoadingService } from '../../../core/services/loading.service';
import { filter } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

/**
 * LayoutComponent - 6-menu sidebar (removed single Workspace), role-based *ngIf, OnPush + signals + inject.
 * Dashboard/Workspaces/Projects visible to all authenticated; Activity/Members filtered; System only for OrgAdmin/SuperAdmin.
 * Hydrates memberships via me() on init so sidebar hides correctly after refresh.
 */
@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet, HeaderComponent, ToastComponent, LoaderComponent],
  templateUrl: './layout.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LayoutComponent implements OnInit {
  auth = inject(AuthService);
  loading = inject(LoadingService);
  private router = inject(Router);
  private http = inject(HttpClient);

  sidebarOpen = signal(false);
  mainCollapsed = signal(localStorage.getItem('mainSidebarCollapsed') === '1');
  isProjectRoute = signal(this.router.url.includes('/p/'));
  globalNotice = signal<any>(null);

  ngOnInit() {
    if (this.auth.isAuthenticated() && this.auth.memberships().length === 0) {
      this.auth.meDeduped().subscribe({
        next: res => this.auth.hydrateFromMe(res as any),
        error: () => {}
      });
    }

    this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe((e: any) => {
      const isProj = e.urlAfterRedirects.includes('/p/');
      this.isProjectRoute.set(isProj);

      if (!isProj && this.mainCollapsed()) {
        this.mainCollapsed.set(false);
        localStorage.setItem('mainSidebarCollapsed', '0');
      }

      this.sidebarOpen.set(false);
    });

    const poll = () => {
      if (document.visibilityState !== 'visible') return;

      this.http.get<any>(`${environment.apiUrl}/api/platform/maintenance`, {
        withCredentials: false,
        headers: { 'X-Silent': 'true' } as any
      }).subscribe({
        next: (res: any) => {
          if (res?.isActive) this.globalNotice.set(res);
          else this.globalNotice.set(null);
        },
        error: () => {}
      });
    };

    poll();

    let timer: any;
    const schedule = () => {
      clearInterval(timer);
      timer = setInterval(poll, 300000);
    };

    schedule();

    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') poll();
    });
  }

  toggle() {
    this.sidebarOpen.update(v => !v);
  }

  close() {
    this.sidebarOpen.set(false);
  }

  toggleMainCollapse() {
    const v = !this.mainCollapsed();
    this.mainCollapsed.set(v);
    localStorage.setItem('mainSidebarCollapsed', v ? '1' : '0');
  }
}