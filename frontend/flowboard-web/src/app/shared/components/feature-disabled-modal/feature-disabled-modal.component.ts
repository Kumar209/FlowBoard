import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ROLE_LABEL_MAP, OrgRoleValues } from '../../constants/roles';

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
  disabledBy = input<string>(ROLE_LABEL_MAP[String(OrgRoleValues.SuperAdmin)]);
  closed = output<void>();
}