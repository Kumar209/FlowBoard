import { Component, ChangeDetectionStrategy, input, output, signal, effect, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../../core/services/project.service';
import { injectQuery } from '@tanstack/angular-query-experimental';

/**
 * TaskCreateModal - Create task with Title, Team, Sprint (Backlog if None), Status via listName, etc.
 */
@Component({
  selector: 'app-task-create-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-create-modal.component.html',
  styleUrls: ['./task-create-modal.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskCreateModalComponent {
  open = input.required<boolean>();
  listName = input<string>('To Do');
  projectId = input<string>('');
  loading = input<boolean>(false);
  error = input<string|null>(null);
  // Strict board context: when lock true, team/sprint are auto-filled from board filter and disabled
  presetTeamId = input<string>('');
  presetSprintId = input<string>('');
  lockTeam = input<boolean>(false);
  lockSprint = input<boolean>(false);
  closed = output<void>();
  submitted = output<{title:string; description:string; priority:string; labels:string; dueDate:string; teamId?:string; sprintId?:string; issueType:string}>();

  title = signal('');
  description = signal('');
  priority = signal('Medium');
  labels = signal('');
  dueDate = signal('');
  teamId = signal('');
  sprintId = signal('');
  issueType = signal('Task');

  private ps = inject(ProjectService);

  teamsQuery = injectQuery(() => ({
    queryKey: ['teams', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getTeams(this.projectId())),
    enabled: this.open() && !!this.projectId(),
  }));
  sprintsQuery = injectQuery(() => ({
    queryKey: ['sprints', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getSprints(this.projectId())),
    enabled: this.open() && !!this.projectId(),
  }));

  isValid = computed(() => this.title().trim().length > 0);

  constructor() {
    effect(() => { if(this.open()){
      this.title.set(''); this.description.set(''); this.priority.set('Medium'); this.labels.set(''); this.dueDate.set('');
      // For lock, set from preset but normalize to actual team/sprint id case from loaded lists
      if(this.lockTeam()){
        const preset = this.presetTeamId() || '';
        const teams = this.teamsQuery.data() || [];
        const matched = teams.find((t:any) => t.id.toLowerCase() === preset.toLowerCase());
        this.teamId.set(matched ? matched.id : preset);
      } else this.teamId.set('');
      if(this.lockSprint()){
        const preset = this.presetSprintId() || '';
        const sprints = this.sprintsQuery.data() || [];
        const matched = sprints.find((s:any) => s.id.toLowerCase() === preset.toLowerCase());
        this.sprintId.set(matched ? matched.id : preset);
      } else this.sprintId.set('');
      this.issueType.set('Task');
    } });
    // Also sync when preset changes while open (e.g., board filter loads after modal open) or teams/sprints load
    effect(() => {
      if(this.open() && this.lockTeam()){
        const preset = this.presetTeamId() || '';
        const teams = this.teamsQuery.data() || [];
        // Only update if preset exists and teamId doesn't already match (handle case mismatch)
        if(preset){
          const matched = teams.find((t:any) => t.id.toLowerCase() === preset.toLowerCase());
          const target = matched ? matched.id : preset;
          if(this.teamId() !== target) this.teamId.set(target);
        } else if(this.teamId() !== '') this.teamId.set('');
      }
    });
    effect(() => {
      if(this.open() && this.lockSprint()){
        const preset = this.presetSprintId() || '';
        const sprints = this.sprintsQuery.data() || [];
        if(preset){
          const matched = sprints.find((s:any) => s.id.toLowerCase() === preset.toLowerCase());
          const target = matched ? matched.id : preset;
          if(this.sprintId() !== target) this.sprintId.set(target);
        } else if(this.sprintId() !== '') this.sprintId.set('');
      }
    });
  }

  submit() {
    if(!this.isValid()) return;
    this.submitted.emit({ title: this.title().trim(), description: this.description().trim(), priority: this.priority(), labels: this.labels().trim(), dueDate: this.dueDate(), teamId: this.teamId() || undefined, sprintId: this.sprintId() || undefined, issueType: this.issueType() });
  }
}
