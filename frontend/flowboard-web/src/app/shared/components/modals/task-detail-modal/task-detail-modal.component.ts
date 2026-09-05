import { Component, ChangeDetectionStrategy, input, output, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * TaskDetailModal - Jira-style: Subtasks, Comments, Details (Assignee, Priority, Labels, Due, Reporter)
 * MNC-grade: OnPush + input.required + signals + computed.
 */
@Component({
  selector: 'app-task-detail-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-detail-modal.component.html',
  styleUrls: ['./task-detail-modal.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskDetailModalComponent {
  open = input.required<boolean>();
  task = input<any>(null);
  lists = input<any[]>([]);
  loading = input<boolean>(false);
  closed = output<void>();
  saved = output<{title:string; description:string; priority:string; listId:string}>();
  commentAdded = output<string>();

  // Editable fields
  title = signal('');
  description = signal('');
  priority = signal('Medium');
  listId = signal('');
  newComment = signal('');
  newSubtask = signal('');

  // Initialize from task when open
  isDirty = computed(() => {
    const t = this.task();
    if(!t) return false;
    return this.title() !== t.title || this.description() !== (t.description||'') || this.priority() !== t.priority || this.listId() !== t.listId;
  });

  constructor() {
    effect(() => {
      if (this.open() && this.task()) {
        const t = this.task();
        this.title.set(t.title || '');
        this.description.set(t.description || '');
        this.priority.set(t.priority || 'Medium');
        this.listId.set(t.listId || '');
      }
    });
  }
  // Call when input task changes
  ngOnInit(){}
  sync() {
    const t = this.task();
    if(t){ this.title.set(t.title||''); this.description.set(t.description||''); this.priority.set(t.priority||'Medium'); this.listId.set(t.listId||''); }
  }
}
