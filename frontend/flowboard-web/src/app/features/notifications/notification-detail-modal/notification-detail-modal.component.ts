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
    const actor = p.ActorName || p.actorName || p.FullName || '';
    const role = p.ActorRole || p.actorRole ? ` (${p.ActorRole || p.actorRole})` : '';
    let title = p.TaskTitle || p.Title || p.title || p.name || '';
    // hide empty title -> fallback to Task id short or action
    if (!title || title.trim() === '') title = '';
    const fromName = p.FromListName || p.fromListName || '';
    const toName = p.ToListName || p.toListName || p.ListName || p.listName || '';
    if (n.action === 'TaskMoved') {
      if (title && fromName && toName) return `${actor}${role} moved "${title}" from ${fromName} → ${toName}`;
      if (fromName && toName) return `${actor}${role} moved task from ${fromName} → ${toName}`;
      return `${actor || 'Someone'}${role} ${n.action}`;
    }
    if (n.action === 'TaskCreated') {
      if (title) return `${actor}${role} created "${title}"${toName ? ' in ' + toName : ''}`;
      return `${actor || 'Someone'}${role} created a task`;
    }
    if (n.action === 'TaskCommented') {
      if (title) return `${actor}${role} commented on "${title}"`;
      return `${actor || 'Someone'}${role} added a comment`;
    }
    return title ? `${actor}${role} ${n.action} "${title}"` : `${actor || 'Someone'}${role} ${n.action}`;
  });
}
