import { Component, OnInit, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

/**
 * DashboardComponent - MNC-grade: OnPush + inject() + Signals + RouterLink (not dead buttons).
 * Header moved to LayoutComponent (drawer), so this page is just content under Layout.
 * workspaceId Signal defaults to demo 111... and updates from me() workspaces[0].id for View projects link.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit {
  auth = inject(AuthService);
  workspaceId = signal<string>('11111111-1111-1111-1111-111111111111');

  ngOnInit() {
    this.auth.me().subscribe({
      next: (res: any) => {
        this.auth.hydrateFromMe(res);
        const ws = res?.workspaces?.[0];
        if (ws?.id) this.workspaceId.set(ws.id);
        else if (ws?.workspaceId) this.workspaceId.set(ws.workspaceId);
      },
      error: () => {}
    });
  }
}
