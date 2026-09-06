import { Component, ChangeDetectionStrategy, input, output, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * ColumnModal - Create/Edit Board Column with name + position.
 * Validates position uniqueness against existingColumns.
 */
@Component({
  selector: 'app-column-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './column-modal.component.html',
  styleUrls: ['./column-modal.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ColumnModalComponent {
  open = input.required<boolean>();
  mode = input<'create'|'update'>('create');
  initialName = input<string>('');
  initialPosition = input<number>(0);
  existingColumns = input<{id:string; name:string; position:number}[]>([]);
  currentColumnId = input<string | null>(null);
  loading = input<boolean>(false);
  error = input<string|null>(null);
  closed = output<void>();
  submitted = output<{name:string; position:number}>();

  name = signal('');
  position = signal<number>(0);
  isUpdate = computed(() => this.mode() === 'update');

  positionError = computed(() => {
    const p = this.position();
    if (p < 0) return 'Position must be >= 0';
    const cols = this.existingColumns();
    const curId = this.currentColumnId();
    const conflict = cols.find(c => c.position === p && c.id !== curId);
    if (conflict) return `Position ${p} already used by "${conflict.name}" — choose another`;
    return null;
  });

  isValid = computed(() => this.name().trim().length > 0 && this.name().trim().length <= 100 && !this.positionError());

  constructor() {
    effect(() => {
      if (this.open()) {
        this.name.set(this.initialName() || '');
        this.position.set(this.initialPosition());
      }
    });
  }

  submit() {
    if (!this.isValid()) return;
    this.submitted.emit({ name: this.name().trim(), position: this.position() });
  }
}
