import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';

@Component({
  selector: 'app-superadmin-settings',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsComponent {
  private sa = inject(SuperAdminService);

  query = injectQuery(() => ({
    queryKey: ['superadmin-settings'] as const,
    queryFn: () => firstValueFrom(this.sa.getSettings()),
  }));

  get d() { return this.query.data(); }
}
