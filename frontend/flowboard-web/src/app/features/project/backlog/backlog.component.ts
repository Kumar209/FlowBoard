import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { TaskDetailModalComponent } from '../../../shared/components/modals/task-detail-modal/task-detail-modal.component';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-backlog',
  standalone: true,
  imports: [CommonModule, TaskDetailModalComponent],
  templateUrl: './backlog.component.html',
  styleUrls: ['./backlog.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BacklogComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  private qc = inject(QueryClient);
  projectId = signal(this.route.parent?.snapshot.paramMap.get('pid') || '');
  workspaceId = signal(this.route.parent?.snapshot.paramMap.get('wid') || this.route.snapshot.paramMap.get('wid') || '');
  search = signal('');
  detailOpen = signal(false);
  selectedTask = signal<any>(null);
  boardQuery = injectQuery(() => ({
    queryKey: ['board', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getBoard(this.projectId())),
    enabled: !!this.projectId(),
  }));
  // Backlog = VIEW WHERE sprintId IS NULL (same Task table, not separate)
  filtered = computed(() => {
    let tasks = (this.boardQuery.data()?.tasks || []).filter((t:any) => !t.sprintId);
    const s = this.search().toLowerCase();
    if (s) tasks = tasks.filter(t => t.title.toLowerCase().includes(s) || (t.description||'').toLowerCase().includes(s));
    return tasks.sort((a,b)=> a.position - b.position);
  });
  allCount = computed(() => this.boardQuery.data()?.tasks?.length || 0);
  updateMutation = injectMutation(() => ({
    mutationFn: (vars: { id:string; title:string; description:string; priority:string; listId:string; labelsJson?:string; assigneeId?:string; dueDate?:string; issueType?:string; epic?:string; storyPoints?:number; startDate?:string; environment?:string; parentIssueId?:string; sprintId?:string; watchersJson?:string; linkedIssuesJson?:string; timeEstimated?:number; timeSpent?:number; timeRemaining?:number; teamId?:string }) =>
      firstValueFrom(this.ps.updateTask(vars.id, vars.title, vars.description, vars.priority, vars.listId, vars.labelsJson, vars.assigneeId, vars.dueDate, vars.issueType, vars.epic, vars.storyPoints, vars.startDate, vars.environment, vars.parentIssueId, vars.sprintId, vars.watchersJson, vars.linkedIssuesJson, vars.timeEstimated, vars.timeSpent, vars.timeRemaining, vars.teamId)),
    onSuccess: () => { this.qc.invalidateQueries({ queryKey: ['board'] }); this.detailOpen.set(false); },
  }));
  openDetail(task:any){ this.selectedTask.set(task); this.detailOpen.set(true); }
  onDetailSave(e:any){
    const t=this.selectedTask(); if(!t) return;
    this.updateMutation.mutate({ id:t.id, title:e.title, description:e.description, priority:e.priority, listId:e.listId, labelsJson: e.labelsJson, assigneeId: e.assigneeId, dueDate: e.dueDate, issueType: e.issueType, epic: e.epic, storyPoints: e.storyPoints, startDate: e.startDate, environment: e.environment, parentIssueId: e.parentIssueId, sprintId: e.sprintId, watchersJson: e.watchersJson, linkedIssuesJson: e.linkedIssuesJson, timeEstimated: e.timeEstimated, timeSpent: e.timeSpent, timeRemaining: e.timeRemaining, teamId: e.teamId });
  }
}
