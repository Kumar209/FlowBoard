import { Component, ChangeDetectionStrategy, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-platform-notice',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './platform-notice.component.html',
  styleUrls: ['./platform-notice.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PlatformNoticeComponent {
  private sa = inject(SuperAdminService);
  private auth = inject(AuthService);
  orgId = input.required<string>();

  query = injectQuery(() => ({
    queryKey: ['platform-notices', this.orgId()] as const,
    queryFn: () => firstValueFrom(this.sa.getNotices(this.orgId())),
    enabled: !!this.orgId(),
  }));

  get notices() { return this.query.data() ?? []; }
  isOwnNotice(n: any) {
    const currentId = this.auth.currentUser()?.id;
    return n.targetUserId && currentId && n.targetUserId.toLowerCase() === currentId.toLowerCase();
  }
}
