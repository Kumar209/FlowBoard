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
  page = signal(1);
  pageSize = signal(10);
  detailOpen = signal(false);
  selectedTask = signal<any>(null);
  boardQuery = injectQuery(() => ({
    queryKey: ['board', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getBoard(this.projectId())),
    enabled: !!this.projectId(),
  }));
  backlogQuery = injectQuery(() => ({
    queryKey: ['backlog', this.projectId(), this.search(), this.page(), this.pageSize()] as const,
    queryFn: () => firstValueFrom(this.ps.getTasks(this.projectId(), { sprintId: 'null', search: this.search() || undefined, page: this.page(), pageSize: this.pageSize() })),
    enabled: !!this.projectId(),
  }));
  get items() {
    const d: any = this.backlogQuery.data();
    return d?.items ?? [];
  }
  get total() { return (this.backlogQuery.data() as any)?.total ?? 0; }
  get totalPages() { return Math.max(1, Math.ceil(this.total / this.pageSize())); }
  get showingFrom() { return this.total === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1; }
  get showingTo() { return Math.min(this.page() * this.pageSize(), this.total); }
  visiblePages(): number[] {
    const total = this.totalPages; const cur = this.page(); const range = 2;
    let start = Math.max(1, cur - range), end = Math.min(total, cur + range);
    if (cur <= 3) end = Math.min(total, 5);
    if (cur >= total - 2) start = Math.max(1, total - 4);
    const pages: number[] = []; for (let i = start; i <= end; i++) pages.push(i); return pages;
  }
  previousPage() { if (this.page() > 1) this.page.set(this.page() - 1); }
  nextPage() { if (this.page() < this.totalPages) this.page.set(this.page() + 1); }
  firstPage() { this.page.set(1); }
  lastPage() { this.page.set(this.totalPages); }
  onPageSizeChange(v: any) { this.pageSize.set(Number(v) || 10); this.page.set(1); }
  // Keep for list name fallback
  allCount = computed(() => this.boardQuery.data()?.tasks?.length || 0);
  updateMutation = injectMutation(() => ({
    mutationFn: (vars: { id:string; title:string; description:string; priority:string; listId:string; labelsJson?:string; assigneeId?:string; dueDate?:string; issueType?:string; epic?:string; storyPoints?:number; startDate?:string; environment?:string; parentIssueId?:string; sprintId?:string; watchersJson?:string; linkedIssuesJson?:string; timeEstimated?:number; timeSpent?:number; timeRemaining?:number; teamId?:string; statusId?:string; acceptanceCriteriaJson?:string }) =>
      firstValueFrom(this.ps.updateTask(vars.id, vars.title, vars.description, vars.priority, vars.listId, vars.labelsJson, vars.assigneeId, vars.dueDate, vars.issueType, vars.epic, vars.storyPoints, vars.startDate, vars.environment, vars.parentIssueId, vars.sprintId, vars.watchersJson, vars.linkedIssuesJson, vars.timeEstimated, vars.timeSpent, vars.timeRemaining, vars.teamId, vars.statusId, vars.acceptanceCriteriaJson)),
    onSuccess: () => { this.qc.invalidateQueries({ queryKey: ['board'] }); this.qc.invalidateQueries({ queryKey: ['backlog'] }); this.detailOpen.set(false); },
  }));
  openDetail(task:any){ this.selectedTask.set(task); this.detailOpen.set(true); }
  onDetailSave(e:any){
    const t=this.selectedTask(); if(!t) return;
    this.updateMutation.mutate({ id:t.id, title:e.title, description:e.description, priority:e.priority, listId:e.listId, labelsJson: e.labelsJson, assigneeId: e.assigneeId, dueDate: e.dueDate, issueType: e.issueType, epic: e.epic, storyPoints: e.storyPoints, startDate: e.startDate, environment: e.environment, parentIssueId: e.parentIssueId, sprintId: e.sprintId, watchersJson: e.watchersJson, linkedIssuesJson: e.linkedIssuesJson, timeEstimated: e.timeEstimated, timeSpent: e.timeSpent, timeRemaining: e.timeRemaining, teamId: e.teamId, statusId: e.statusId, acceptanceCriteriaJson: e.acceptanceCriteriaJson });
  }
}
