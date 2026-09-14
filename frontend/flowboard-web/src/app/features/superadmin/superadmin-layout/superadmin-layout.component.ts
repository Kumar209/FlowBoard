import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { HeaderComponent } from '../../../shared/components/header/header.component';
import { ToastComponent } from '../../../shared/components/toast/toast.component';
import { AuthService } from '../../../core/services/auth.service';
import { filter } from 'rxjs';

@Component({
  selector: 'app-superadmin-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet, HeaderComponent, ToastComponent],
  templateUrl: './superadmin-layout.component.html',
  styleUrls: ['./superadmin-layout.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SuperAdminLayoutComponent implements OnInit {
  auth = inject(AuthService);
  private router = inject(Router);
  sidebarOpen = signal(false);
  mainCollapsed = signal(localStorage.getItem('saSidebarCollapsed') === '1');

  ngOnInit() {
    if (this.auth.isAuthenticated() && this.auth.memberships().length === 0) {
      this.auth.me().subscribe({ next: res => this.auth.hydrateFromMe(res as any), error: () => {} });
    }
    this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe(() => {});
  }

  toggle() { this.sidebarOpen.update(v => !v); }
  close() { this.sidebarOpen.set(false); }
  toggleMainCollapse() {
    const v = !this.mainCollapsed();
    this.mainCollapsed.set(v);
    localStorage.setItem('saSidebarCollapsed', v ? '1' : '0');
  }
  logout() {
    this.auth.logout().subscribe({ next: () => { this.auth.clearSession(); this.router.navigate(['/login']); }, error: () => { this.auth.clearSession(); this.router.navigate(['/login']); } });
  }
}
