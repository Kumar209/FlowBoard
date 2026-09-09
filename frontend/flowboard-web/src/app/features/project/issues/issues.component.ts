import { Component, ChangeDetectionStrategy, inject, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { ToastService } from '../../../core/services/toast.service';
import { TaskDetailModalComponent } from '../../../shared/components/modals/task-detail-modal/task-detail-modal.component';
import { TaskCreateModalComponent } from '../../../shared/components/modals/task-create-modal/task-create-modal.component';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-issues',
  standalone: true,
  imports: [CommonModule, TaskDetailModalComponent, TaskCreateModalComponent],
  templateUrl: './issues.component.html',
  styleUrls: ['./issues.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IssuesComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);
  projectId = signal(this.route.parent?.snapshot.paramMap.get('pid') || '');
  workspaceId = signal(this.route.parent?.snapshot.paramMap.get('wid') || '');

  constructor() {
    queueMicrotask(() => {
      this.route.queryParamMap.subscribe(m => {
        const taskId = m.get('task');
        if (taskId) this.openTaskFromQuery(taskId);
      });
      this.route.parent?.queryParamMap?.subscribe(m => {
        const taskId = m.get('task');
        if (taskId) this.openTaskFromQuery(taskId);
      });
    });
    const initialTask = this.route.snapshot.queryParamMap.get('task') || this.route.parent?.snapshot.queryParamMap.get('task');
    if (initialTask) queueMicrotask(() => this.openTaskFromQuery(initialTask));
  }

  private async openTaskFromQuery(taskId: string) {
    if (!taskId) return;
    const found = this.boardQuery.data()?.tasks?.find((x: any) => x.id === taskId);
    if (found) { this.openDetail(found); return; }
    try {
      const detail: any = await firstValueFrom(this.ps.getTaskDetail(taskId));
      if (detail?.task) this.openDetail(detail.task);
      else if (detail) this.openDetail(detail);
    } catch {}
  }
  typeFilter = signal('');
  search = signal('');
  page = signal(1);
  pageSize = 8;
  detailOpen = signal(false);
  detailReadOnly = signal(false);
  selectedTask = signal<any>(null);
  createOpen = signal(false);
  createListId = signal('');
  createStatusId = signal('');
  deleteTarget = signal<any>(null);
  boardQuery = injectQuery(() => ({
    queryKey: ['board', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getBoard(this.projectId())),
    enabled: !!this.projectId(),
  }));
  teamsQuery = injectQuery(() => ({
    queryKey: ['teams', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getTeams(this.projectId())),
    enabled: !!this.projectId(),
  }));
  sprintsQuery = injectQuery(() => ({
    queryKey: ['sprints', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getSprints(this.projectId())),
    enabled: !!this.projectId(),
  }));
  membersQuery = injectQuery(() => ({
    queryKey: ['workspace-members', this.workspaceId()] as const,
    queryFn: () => firstValueFrom(this.ps.getWorkspaceMembers(this.workspaceId())),
    enabled: !!this.workspaceId(),
  }));
  statusesQuery = injectQuery(() => ({
    queryKey: ['statuses', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getStatuses(this.projectId())),
    enabled: !!this.projectId(),
  }));
  filtered = computed(() => {
    let tasks = this.boardQuery.data()?.tasks || [];
    const q = this.search().toLowerCase().trim();
    if (q) tasks = tasks.filter(x => x.title.toLowerCase().includes(q) || (x.description||'').toLowerCase().includes(q) || x.id.toLowerCase().includes(q));
    const t = this.typeFilter().toLowerCase();
    if (t) tasks = tasks.filter(x => (x.labelsJson||'').toLowerCase().includes(t) || x.priority.toLowerCase()===t || (x.issueType||'').toLowerCase()===t);
    return tasks.sort((a,b)=> a.position - b.position);
  });
  totalPages = computed(() => Math.max(1, Math.ceil(this.filtered().length / this.pageSize)));
  paginated = computed(() => {
    const start = (this.page()-1)*this.pageSize;
    return this.filtered().slice(start, start+this.pageSize);
  });
  updateMutation = injectMutation(() => ({
    mutationFn: (vars: any) => firstValueFrom(this.ps.updateTask(vars.id, vars.title, vars.description, vars.priority, vars.listId, vars.labelsJson, vars.assigneeId, vars.dueDate, vars.issueType, vars.epic, vars.storyPoints, vars.startDate, vars.environment, vars.parentIssueId, vars.sprintId, vars.watchersJson, vars.linkedIssuesJson, vars.timeEstimated, vars.timeSpent, vars.timeRemaining, vars.teamId, vars.statusId)),
    onSuccess: () => { this.qc.invalidateQueries({queryKey: ['board']}); this.detailOpen.set(false); this.toast.success('Issue updated'); },
    onError: (e:any) => this.toast.error(e.error?.error || 'Update failed'),
  }));
  createMutation = injectMutation(() => ({
    mutationFn: (vars: any) => firstValueFrom(this.ps.createTask(this.projectId(), vars.listId || null, vars.title, vars.description, vars.priority, vars.labelsJson, undefined, vars.dueDate, vars.issueType, vars.epic, vars.storyPoints, vars.startDate, vars.environment, vars.parentIssueId, vars.sprintId, vars.teamId, vars.statusId)),
    onSuccess: () => { this.qc.invalidateQueries({queryKey: ['board']}); this.toast.success('Issue created in Backlog'); },
    onError: (e:any) => this.toast.error(e.error?.error || 'Create failed'),
  }));
  deleteMutation = injectMutation(() => ({
    mutationFn: (id:string) => firstValueFrom(this.ps.deleteTask(id)),
    onSuccess: () => { this.qc.invalidateQueries({queryKey:['board']}); this.deleteTarget.set(null); this.toast.success('Issue deleted'); },
    onError: (e:any) => this.toast.error(e.error?.error || 'Delete failed')
  }));
  openDetail(t:any, readOnly=false){ this.selectedTask.set(t); this.detailReadOnly.set(readOnly); this.detailOpen.set(true); }
  @HostListener('window:openTask', ['$event'])
  onOpenTask(event:any){
    const task = event.detail;
    if(!task?.id){ this.toast.error('Issue not found'); return; }
    const found = this.boardQuery.data()?.tasks?.find((x:any)=>x.id===task.id);
    this.openDetail(found || task);
    if(!found) this.toast.error('Linked issue not in current project view — opened anyway');
  }
  confirmDelete(t:any){ this.deleteTarget.set(t); }
  getTeamName(teamId?:string){ if(!teamId) return '—'; return this.teamsQuery.data()?.find((x:any)=>x.id===teamId)?.name || '—'; }
  getSprintName(sprintId?:string){ if(!sprintId) return 'Backlog'; return this.sprintsQuery.data()?.find((x:any)=>x.id===sprintId)?.name || 'Backlog'; }
  getAssigneeName(assigneeId?:string){ if(!assigneeId) return 'Unassigned'; const m = this.membersQuery.data()?.find((x:any)=>x.userId===assigneeId); return m ? m.fullName : assigneeId.slice(0,6); }
  async openCreate(){
    const statuses = this.statusesQuery.data() || [];
    if(statuses.length===0) {
      this.toast.error('Create a Status first — go to Project → Statuses → + New Status (e.g., To Do). Issues go to Backlog.');
      return;
    }
    // Issues are independent of boards/columns — go to Backlog with Status
    this.createListId.set(''); // no column
    this.createStatusId.set(statuses[0].id);
    this.createOpen.set(true);
  }
  onCreateSubmit(e:any){
    const labelsJson = e.labels ? JSON.stringify(e.labels.split(',').map((s:string)=>s.trim()).filter(Boolean)) : undefined;
    this.createMutation.mutate({ listId: null, statusId: this.createStatusId(), title: e.title, description: e.description, priority: e.priority, labelsJson, dueDate: e.dueDate, issueType: e.issueType, teamId: e.teamId, sprintId: e.sprintId });
    this.createOpen.set(false);
  }
  onSave(e:any){ const t=this.selectedTask(); if(!t) return; this.updateMutation.mutate({ id:t.id, title:e.title, description:e.description, priority:e.priority, listId:e.listId, labelsJson:e.labelsJson, assigneeId:e.assigneeId, dueDate:e.dueDate, issueType: e.issueType, epic: e.epic, storyPoints: e.storyPoints, startDate: e.startDate, environment: e.environment, parentIssueId: e.parentIssueId, sprintId: e.sprintId, watchersJson: e.watchersJson, linkedIssuesJson: e.linkedIssuesJson, timeEstimated: e.timeEstimated, timeSpent: e.timeSpent, timeRemaining: e.timeRemaining, teamId: e.teamId, statusId: e.statusId }); }
}
