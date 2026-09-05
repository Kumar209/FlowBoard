import { Component, ChangeDetectionStrategy, input, output, signal, effect, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * ProjectModal - MNC-grade: OnPush + signals + validation. Create/Update project (name + description + optional slug).
 */
@Component({
  selector: 'app-project-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './project-modal.component.html',
  styleUrls: ['./project-modal.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectModalComponent {
  open = input.required<boolean>();
  mode = input<'create'|'update'>('create');
  initialName = input<string>('');
  initialDescription = input<string>('');
  workspaces = input<{id:string; name:string}[]>([]);
  initialWorkspaceId = input<string>('');
  loading = input<boolean>(false);
  error = input<string|null>(null);

  closed = output<void>();
  submitted = output<{name:string; description:string; workspaceId:string}>();

  name = signal('');
  description = signal('');
  workspaceId = signal('');
  wsSearch = signal('');
  dropdownOpen = signal(false);

  isUpdate = computed(() => this.mode()==='update');
  filteredWorkspaces = computed(() => {
    const s = this.wsSearch().toLowerCase();
    const ws = this.workspaces();
    return s ? ws.filter(w => w.name.toLowerCase().includes(s)) : ws;
  });
  selectedWorkspaceName = computed(() => this.workspaces().find(w => w.id === this.workspaceId())?.name || 'Select workspace');

  constructor() {
    effect(() => {
      if (this.open()) {
        this.name.set(this.initialName());
        this.description.set(this.initialDescription());
        this.workspaceId.set(this.initialWorkspaceId() || this.workspaces()[0]?.id || '');
      }
    });
  }

  submit() {
    const n = this.name().trim();
    if (!n) return;
    this.submitted.emit({ name: n, description: this.description().trim(), workspaceId: this.workspaceId() });
  }
}
