import { Component, ChangeDetectionStrategy, input, output, signal, computed, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../../core/services/project.service';
import { injectQuery } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-task-create-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-create-modal.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskCreateModalComponent {
  open = input<boolean>(false);
  listName = input<string>('');
  projectId = input<string>('');
  loading = input<boolean>(false);
  error = input<string | null>(null);
  presetTeamId = input<string>('');
  presetSprintId = input<string>('');
  lockTeam = input<boolean>(false);
  lockSprint = input<boolean>(false);

  closed = output<void>();
  submitted = output<{
    title: string;
    description: string;
    priority: string;
    labels: string;
    dueDate: string;
    teamId?: string;
    sprintId?: string;
    issueType: string;
  }>();

  private projectService = inject(ProjectService);

  title = signal('');
  description = signal('');
  priority = signal('Medium');
  labels = signal('');
  dueDate = signal('');
  teamId = signal('');
  sprintId = signal('');
  issueType = signal('Task');

  teamsQuery = injectQuery(() => ({
    queryKey: ['teams', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getTeams(this.projectId())),
    enabled: !!this.projectId(),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000
  }));

  sprintsQuery = injectQuery(() => ({
    queryKey: ['sprints', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.projectService.getSprints(this.projectId())),
    enabled: !!this.projectId(),
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000
  }));

  isValid = computed(() => !!this.title().trim());

  constructor() {
    effect(() => {
      if (this.open()) {
        this.resetForm();
      }
    });

    effect(() => {
      if (!this.open()) return;

      if (this.lockTeam()) {
        this.teamId.set(this.presetTeamId() || '');
      }

      if (this.lockSprint()) {
        this.sprintId.set(this.presetSprintId() || '');
      }
    });
  }

  private resetForm() {
    this.title.set('');
    this.description.set('');
    this.priority.set('Medium');
    this.labels.set('');
    this.dueDate.set('');
    this.issueType.set('Task');
    this.teamId.set(this.presetTeamId() || '');
    this.sprintId.set(this.presetSprintId() || '');
  }

  close() {
    if (this.loading()) return;
    this.closed.emit();
  }

  submit() {
    if (!this.title().trim() || this.loading()) return;

    this.submitted.emit({
      title: this.title().trim(),
      description: this.description().trim(),
      priority: this.priority(),
      labels: this.labels().trim(),
      dueDate: this.dueDate(),
      teamId: this.teamId() || undefined,
      sprintId: this.sprintId() || undefined,
      issueType: this.issueType()
    });
  }
}