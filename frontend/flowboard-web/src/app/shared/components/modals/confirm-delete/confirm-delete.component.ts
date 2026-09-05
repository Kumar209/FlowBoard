import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * ConfirmDeleteModal - MNC-grade: OnPush + input.required + output. DaisyUI modal with warning.
 */
@Component({
  selector: 'app-confirm-delete',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './confirm-delete.component.html',
  styleUrls: ['./confirm-delete.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfirmDeleteComponent {
  open = input.required<boolean>();
  title = input<string>('Delete?');
  message = input<string>('This cannot be undone.');
  loading = input<boolean>(false);
  confirmed = output<void>();
  cancelled = output<void>();
}
