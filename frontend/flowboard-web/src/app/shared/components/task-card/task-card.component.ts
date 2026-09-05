import { Component, input, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * TaskCardComponent - MNC-grade: signal inputs (input.required) + computed() + OnPush.
 * Why not @Input() title = ''? That is anemic, not type-safe, getter recomputes on every change detection (no memoization),
 * and allows parent to pass undefined without compile error. MNC uses input.required<string>() (compile-time required),
 * input<string>('Medium') with default + transform, and computed() memoized derived state (priorityColor/labels parsed once per change).
 * ChangeDetectionStrategy.OnPush + signals gives fine-grained updates (only when input signal changes), not full zone tick.
 * Boilerplate increase is intentional for prod-grade.
 */
@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-card.component.html',
  styleUrls: ['./task-card.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskCardComponent {
  // Signal inputs - strictly typed, required where needed, transform via input()
  title = input.required<string>();
  priority = input<string>('Medium');
  labelsJson = input<string | undefined>(undefined);
  assigneeId = input<string | undefined>(undefined);

  // Computed - memoized, only re-runs when priority() signal changes (not every CD)
  priorityColor = computed(() => {
    switch (this.priority()) {
      case 'Urgent': return 'badge-error';
      case 'High': return 'badge-warning';
      case 'Low': return 'badge-ghost';
      default: return 'badge-info';
    }
  });

  // Computed labels - JSON.parse once per labelsJson() change, safe fallback []
  labels = computed<string[]>(() => {
    const raw = this.labelsJson();
    if (!raw) return [];
    try { const parsed = JSON.parse(raw); return Array.isArray(parsed) ? parsed : []; } catch { return []; }
  });
}
