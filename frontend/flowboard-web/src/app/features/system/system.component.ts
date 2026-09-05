import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';

/**
 * SystemComponent - MNC-grade: OnPush + role-gated (OrgAdmin/SuperAdmin only via orgAdminGuard).
 * Shows health checks for YARP/Gateway + services.
 */
@Component({
  selector: 'app-system',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './system.component.html',
  styleUrls: ['./system.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SystemComponent {
  auth = inject(AuthService);
}
