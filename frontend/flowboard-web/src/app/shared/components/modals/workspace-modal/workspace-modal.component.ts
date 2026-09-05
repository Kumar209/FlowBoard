import { Component, ChangeDetectionStrategy, input, output, signal, effect, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * WorkspaceModal - MNC-grade: OnPush + input.required + signals + computed validation. Handles Create + Update.
 * DaisyUI modal, slug editable with pattern a-z0-9-.
 */
@Component({
  selector: 'app-workspace-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './workspace-modal.component.html',
  styleUrls: ['./workspace-modal.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkspaceModalComponent {
  open = input.required<boolean>();
  mode = input<'create'|'update'>('create');
  initialName = input<string>('');
  initialSlug = input<string>('');
  organizations = input<{id:string; name:string; slug:string}[]>([]);
  initialOrgId = input<string>('');
  loading = input<boolean>(false);
  error = input<string|null>(null);

  closed = output<void>();
  submitted = output<{name:string; slug:string; organizationId:string}>();

  name = signal('');
  slug = signal('');
  orgId = signal('');

  isUpdate = computed(() => this.mode()==='update');

  constructor() {
    effect(() => {
      if (this.open()) {
        this.name.set(this.initialName());
        this.slug.set(this.initialSlug());
        this.orgId.set(this.initialOrgId() || this.organizations()[0]?.id || '');
      }
    });
  }

  submit() {
    const n = this.name().trim();
    if (!n) return;
    if (this.slug() && !/^[a-z0-9-]+$/.test(this.slug())) return;
    this.submitted.emit({ name: n, slug: this.slug().trim().toLowerCase(), organizationId: this.orgId() });
  }
}
