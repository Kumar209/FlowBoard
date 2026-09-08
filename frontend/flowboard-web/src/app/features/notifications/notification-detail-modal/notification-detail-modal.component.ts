import { Component, ChangeDetectionStrategy, input, output, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

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
  private router = inject(Router);

  payload = computed(() => {
    try { return JSON.parse(this.notification()?.payloadJson || '{}'); } catch { return {}; }
  });

  prettyPayload = computed(() => {
    const n = this.notification();
    if (!n) return '{}';
    const p: any = this.payload();
    const obj: any = {};
    if (n.taskTitle || p.TaskTitle || p.Title) obj.issue = n.taskTitle || p.TaskTitle || p.Title || '';
    if (n.projectName) obj.project = n.projectName;
    else if (p.ProjectName || p.ProjectKey) obj.project = p.ProjectName || p.ProjectKey || '';
    if (p.FromListName || p.fromListName) obj.fromColumn = p.FromListName || p.fromListName;
    if (p.ToListName || p.toListName || p.ListName) obj.toColumn = p.ToListName || p.toListName || p.ListName || '';
    if (p.BoardName || p.boardName) obj.board = p.BoardName || p.boardName;
    if (p.SprintName || p.sprintName) obj.sprint = p.SprintName || p.sprintName;
    if (p.CommentContent || p.content || p.commentPreview) obj.comment = p.CommentContent || p.content || p.commentPreview || '';
    if (n.actorName || p.ActorName) obj.actor = (n.actorName || p.ActorName || '') + (p.ActorRole ? ` (${p.ActorRole})` : '');
    if (p.deepLink || p.DeepLink) obj.link = p.deepLink || p.DeepLink || '';
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
    const fromName = p.FromListName || p.fromListName || p.fromList || '';
    const toName = p.ToListName || p.toListName || p.ListName || p.listName || '';
    const boardName = p.BoardName || p.boardName || '';
    const sprintName = p.SprintName || p.sprintName || '';
    const boardPart = boardName ? ` in board "${boardName}"` : '';
    const sprintPart = sprintName ? ` (Sprint: ${sprintName})` : '';
    const displayAction = (n.action || '').replace(/^Task/, 'Issue');
    if (n.action === 'TaskMoved') {
      if (title && fromName && toName) return `${actor}${role} moved "${title}" from ${fromName} → ${toName}${boardPart}${sprintPart}`;
      if (fromName && toName) return `${actor}${role} moved issue from ${fromName} → ${toName}${boardPart}${sprintPart}`;
      if (title) return `${actor || 'Someone'}${role} moved "${title}"${boardPart}${sprintPart}`;
      return `${actor || 'Someone'}${role} ${displayAction}${boardPart}${sprintPart}`;
    }
    if (n.action === 'TaskCreated') {
      if (title) return `${actor}${role} created an issue "${title}"${boardPart}${sprintPart}`;
      return `${actor || 'Someone'}${role} created an issue${boardPart}${sprintPart}`;
    }
    if (n.action === 'TaskCommented') {
      const preview = p.CommentContent || p.commentPreview || p.content || '';
      if (title && preview) return `${actor}${role} commented on "${title}": "${preview.slice(0,40)}"`;
      if (title) return `${actor}${role} commented on "${title}"`;
      return `${actor || 'Someone'}${role} ${displayAction}`;
    }
    return title ? `${actor}${role} ${displayAction} "${title}"${boardPart}${sprintPart}` : `${actor || 'Someone'}${role} ${displayAction}${boardPart}${sprintPart}`;
  });

  displayAction = computed(() => (this.notification()?.action || '').replace(/^Task/, 'Issue'));

  goToIssue() {
    const n = this.notification();
    if (!n) return;
    const p: any = this.payload();
    const link = p.deepLink || p.DeepLink || '';
    if (link) {
      this.closed.emit();
      this.router.navigateByUrl(link);
    } else if (n.projectId && n.taskId) {
      this.closed.emit();
      // Fallback: dispatch openTask event for board
      const evt = new CustomEvent('openTask', { detail: { id: n.taskId } });
      window.dispatchEvent(evt);
    }
  }
}
