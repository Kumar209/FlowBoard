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

  prettyPayload = computed(() => {
    const n = this.notification();
    if (!n) return '{}';
    const p: any = this.payload();
    const obj: any = {};
    if (n.taskTitle || p.TaskTitle || p.Title) obj.taskTitle = n.taskTitle || p.TaskTitle || p.Title || '';
    if (n.projectName) obj.project = n.projectName;
    else if (p.ProjectName || p.ProjectKey) obj.project = p.ProjectName || p.ProjectKey || '';
    if (p.FromListName || p.fromListName) obj.fromColumn = p.FromListName || p.fromListName;
    if (p.ToListName || p.toListName || p.ListName) obj.toColumn = p.ToListName || p.toListName || p.ListName || '';
    if (p.BoardName || p.boardName) obj.board = p.BoardName || p.boardName;
    if (p.SprintName || p.sprintName) obj.sprint = p.SprintName || p.sprintName;
    if (p.CommentContent || p.content) obj.comment = p.CommentContent || p.content || '';
    if (n.actorName || p.ActorName) obj.actor = (n.actorName || p.ActorName || '') + (p.ActorRole ? ` (${p.ActorRole})` : '');
    Object.keys(obj).forEach(k => { if (!obj[k] || obj[k].toString().trim() === '') delete obj[k]; });
    return JSON.stringify(obj, null, 2);
  });

  humanMessage = computed(() => {
    const n = this.notification();
    if (!n) return '';
    const p: any = this.payload();
    const actor = n.actorName || p.ActorName || p.actorName || p.FullName || '';
    const role = p.ActorRole || p.actorRole ? ` (${p.ActorRole || p.actorRole})` : '';
    let title = n.taskTitle || p.TaskTitle || p.Title || p.title || p.name || '';
    if (!title || title.trim() === '') title = '';
    const fromName = p.FromListName || p.fromListName || '';
    const toName = p.ToListName || p.toListName || p.ListName || p.listName || '';
    const boardName = p.BoardName || p.boardName || '';
    const sprintName = p.SprintName || p.sprintName || '';
    const boardPart = boardName ? ` in board "${boardName}"` : '';
    const sprintPart = sprintName ? ` (Sprint: ${sprintName})` : '';
    if (n.action === 'TaskMoved') {
      if (title && fromName && toName) return `${actor}${role} moved "${title}" from ${fromName} → ${toName}${boardPart}${sprintPart}`;
      if (fromName && toName) return `${actor}${role} moved task from ${fromName} → ${toName}${boardPart}${sprintPart}`;
      if (title) return `${actor || 'Someone'}${role} moved "${title}"${boardPart}${sprintPart}`;
      return `${actor || 'Someone'}${role} ${n.action}${boardPart}${sprintPart}`;
    }
    if (n.action === 'TaskCreated') {
      if (title) return `${actor}${role} created "${title}"${boardPart}${sprintPart}${toName ? ' in ' + toName : ''}`;
      return `${actor || 'Someone'}${role} created a task${boardPart}${sprintPart}`;
    }
    if (n.action === 'TaskCommented') {
      if (title) return `${actor}${role} commented on "${title}"${boardPart}${sprintPart}`;
      return `${actor || 'Someone'}${role} added a comment${boardPart}${sprintPart}`;
    }
    return title ? `${actor}${role} ${n.action} "${title}"${boardPart}${sprintPart}` : `${actor || 'Someone'}${role} ${n.action}${boardPart}${sprintPart}`;
  });
}
