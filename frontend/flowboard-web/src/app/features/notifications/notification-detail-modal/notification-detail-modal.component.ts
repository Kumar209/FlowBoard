import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';
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

  payload = computed(() => {
    try { return JSON.parse(this.notification()?.payloadJson || '{}'); } catch { return {}; }
  });

  humanMessage = computed(() => {
    const n = this.notification();
    if (!n) return '';
    const p: any = this.payload();
    const actor = p.ActorName || p.actorName || n.actorUserId?.slice(0,8) || '';
    const role = p.ActorRole || p.actorRole ? ` (${p.ActorRole || p.actorRole})` : '';
    const title = p.TaskTitle || p.Title || p.title || '';
    const fromName = p.FromListName || p.fromListName || '';
    const toName = p.ToListName || p.toListName || p.ListName || p.listName || '';
    if (n.action === 'TaskMoved' && fromName && toName) return `${actor}${role} moved "${title}" from ${fromName} → ${toName}`;
    if (n.action === 'TaskCreated' && title) return `${actor}${role} created "${title}"${toName ? ' in ' + toName : ''}`;
    if (n.action === 'TaskCommented' && title) return `${actor}${role} commented on "${title}"`;
    return `${actor}${role} ${n.action} "${title}"`;
  });
}
