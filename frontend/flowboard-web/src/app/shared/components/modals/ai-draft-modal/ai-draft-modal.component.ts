import { Component, ChangeDetectionStrategy, input, output, signal, computed, inject, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { AiService, AiDraftResponse } from '../../../../core/services/ai.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-ai-draft-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ai-draft-modal.component.html',
  styleUrls: ['./ai-draft-modal.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AiDraftModalComponent {
  open = input<boolean>(false);
  projectId = input<string>('');
  closed = output<void>();
  created = output<{ title: string; description: string; checklist: string[]; labels: string[]; priority: string; issueType: string; storyPoints?: number }>();

  private ai = inject(AiService);
  private toast = inject(ToastService);

  prompt = signal('');
  model = signal('gemini-2.5-flash');
  isGenerating = signal(false);
  draft = signal<AiDraftResponse | null>(null);
  error = signal<string | null>(null);

  // Editable preview fields (bind after draft loaded)
  title = signal('');
  description = signal('');
  checklist = signal(''); // comma or newline separated for edit
  labels = signal('');
  priority = signal('Medium');
  issueType = signal('Task');
  storyPoints = signal<number | null>(null);

  promptValid = computed(() => {
    const p = this.prompt().trim();
    return p.length >= 10 && p.length <= 500;
  });
  promptCount = computed(() => this.prompt().trim().length);
  canGenerate = computed(() => this.promptValid() && !this.isGenerating());

  isPreview = computed(() => !!this.draft());

  constructor() {
    effect(() => {
      if (this.open()) {
        // reset when opened fresh if not already preview
        if (!this.draft()) {
          this.prompt.set('');
          this.model.set('gemini-2.5-flash');
          this.error.set(null);
        }
      }
    }, { allowSignalWrites: true });

    effect(() => {
      const d = this.draft();
      if (d) {
        this.title.set(d.title || '');
        this.description.set(d.description || '');
        this.checklist.set((d.checklist || []).join('\n'));
        this.labels.set((d.labels || []).join(', '));
        this.priority.set(this.normalizePriority(d.priority));
        this.issueType.set(d.issueType || 'Task');
        this.storyPoints.set(d.storyPoints ?? null);
      }
    }, { allowSignalWrites: true });
  }

  private normalizePriority(p: string): string {
    if (!p) return 'Medium';
    const low = p.toLowerCase();
    if (['low', '0'].includes(low)) return 'Low';
    if (['medium', '1'].includes(low)) return 'Medium';
    if (['high', '2'].includes(low)) return 'High';
    if (['urgent', '3'].includes(low)) return 'Urgent';
    return p.charAt(0).toUpperCase() + p.slice(1).toLowerCase();
  }

  async generate() {
    if (!this.canGenerate()) return;
    this.isGenerating.set(true);
    this.error.set(null);
    try {
      const res = await firstValueFrom(this.ai.draft(this.prompt().trim(), this.model(), this.projectId() || undefined));
      this.draft.set(res);
      this.toast.success(`Draft via ${res.provider} • ${res.model}`);
    } catch (e: any) {
      const msg = e.error?.error || e.message || 'Generate failed';
      this.error.set(msg);
      this.toast.error(msg);
    } finally {
      this.isGenerating.set(false);
    }
  }

  backToPrompt() {
    this.draft.set(null);
    this.error.set(null);
  }

  close() {
    this.draft.set(null);
    this.prompt.set('');
    this.error.set(null);
    this.closed.emit();
  }

  submitCreate() {
    const t = this.title().trim();
    if (!t) { this.toast.error('Title required'); return; }
    const checklistArr = this.checklist().split('\n').map(s => s.trim()).filter(Boolean);
    const labelsArr = this.labels().split(',').map(s => s.trim()).filter(Boolean);
    this.created.emit({
      title: t,
      description: this.description().trim(),
      checklist: checklistArr,
      labels: labelsArr,
      priority: this.priority(),
      issueType: this.issueType(),
      storyPoints: this.storyPoints() ?? undefined
    });
    this.draft.set(null);
  }
}
