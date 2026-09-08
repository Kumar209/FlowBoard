import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-notification-detail-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-detail-modal.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationDetailModalComponent {
  open = input<boolean>(false);
  notification = input<any>(null);
  closed = output<void>();
}
