import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-delete',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './confirm-delete.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfirmDeleteComponent {
  open = input<boolean>(false);
  title = input<string>('Delete?');
  message = input<string>('This cannot be undone.');
  loading = input<boolean>(false);

  confirmed = output<void>();
  cancelled = output<void>();
}