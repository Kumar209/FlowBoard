import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-feature-disabled-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './feature-disabled-modal.component.html',
  styleUrls: ['./feature-disabled-modal.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FeatureDisabledModalComponent {
  open = input.required<boolean>();
  featureName = input<string>('This feature');
  disabledBy = input<string>('SuperAdmin');
  closed = output<void>();
}
