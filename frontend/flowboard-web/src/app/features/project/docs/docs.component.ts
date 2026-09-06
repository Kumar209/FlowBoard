import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-docs',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './docs.component.html',
  styleUrls: ['./docs.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DocsComponent {}
