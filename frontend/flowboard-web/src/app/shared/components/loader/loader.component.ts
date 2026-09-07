import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loader',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex flex-col items-center justify-center py-8 gap-3" [class]="size() === 'sm' ? 'py-4' : 'py-8'">
      <span class="loading loading-spinner" [class.loading-sm]="size()==='sm'" [class.loading-lg]="size()==='lg'"></span>
      @if (text()) { <p class="text-xs opacity-60">{{ text() }}</p> }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoaderComponent {
  text = input<string>('Loading...');
  size = input<'sm'|'md'|'lg'>('md');
}
