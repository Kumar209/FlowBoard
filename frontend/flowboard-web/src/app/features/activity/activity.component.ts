import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';

/**
 * ActivityComponent - MNC-grade: OnPush + signals + computed role label.
 * Shows audit timeline placeholder (Task 2.5 will wire GET /api/projects/{pid}/activities paged).
 * Manager/Member sees workspace-scoped, Client/Viewer sees own project only (filtered, not all).
 */
@Component({
  selector: 'app-activity',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './activity.component.html',
  styleUrls: ['./activity.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ActivityComponent {
  auth = inject(AuthService);
  hint = signal('Timeline will show after Task 2.5 GET /activities — Client/Viewer sees own project only');
}
