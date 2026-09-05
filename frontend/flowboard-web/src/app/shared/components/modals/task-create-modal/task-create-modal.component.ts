import { Component, ChangeDetectionStrategy, input, output, signal, effect, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * TaskCreateModal - MNC-grade: OnPush + signals + validation. Create task with enough details.
 * Fields: title*, description, priority, labels, assignee, dueDate.
 */
@Component({
  selector: 'app-task-create-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-create-modal.component.html',
  styleUrls: ['./task-create-modal.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskCreateModalComponent {
  open = input.required<boolean>();
  listName = input<string>('To Do');
  loading = input<boolean>(false);
  error = input<string|null>(null);
  closed = output<void>();
  submitted = output<{title:string; description:string; priority:string; labels:string; dueDate:string}>();

  title = signal('');
  description = signal('');
  priority = signal('Medium');
  labels = signal('');
  dueDate = signal('');

  isValid = computed(() => this.title().trim().length > 0);

  // Reset on open
  constructor() { effect(() => { if(this.open()){ this.title.set(''); this.description.set(''); this.priority.set('Medium'); this.labels.set(''); this.dueDate.set(''); } }); }

  submit() {
    if(!this.isValid()) return;
    this.submitted.emit({ title: this.title().trim(), description: this.description().trim(), priority: this.priority(), labels: this.labels().trim(), dueDate: this.dueDate() });
  }
}
