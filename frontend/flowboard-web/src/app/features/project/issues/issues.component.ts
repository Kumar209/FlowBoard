import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
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
  typeFilter = signal('');
  search = signal('');
  page = signal(1);
  pageSize = 8;
  detailOpen = signal(false);
  selectedTask = signal<any>(null);
  createOpen = signal(false);
  createListId = signal('');
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
    mutationFn: (vars: any) => firstValueFrom(this.ps.updateTask(vars.id, vars.title, vars.description, vars.priority, vars.listId, vars.labelsJson, vars.assigneeId, vars.dueDate, vars.issueType, vars.epic, vars.storyPoints, vars.startDate, vars.environment, vars.parentIssueId, vars.sprintId, vars.watchersJson, vars.linkedIssuesJson, vars.timeEstimated, vars.timeSpent, vars.timeRemaining, vars.teamId)),
    onSuccess: () => { this.qc.invalidateQueries({queryKey: ['board']}); this.detailOpen.set(false); this.toast.success('Issue updated'); },
    onError: (e:any) => this.toast.error(e.error?.error || 'Update failed'),
  }));
  createMutation = injectMutation(() => ({
    mutationFn: (vars: any) => firstValueFrom(this.ps.createTask(this.projectId(), vars.listId, vars.title, vars.description, vars.priority, vars.labelsJson, undefined, vars.dueDate, vars.issueType, vars.epic, vars.storyPoints, vars.startDate, vars.environment, vars.parentIssueId, vars.sprintId, vars.teamId)),
    onSuccess: () => { this.qc.invalidateQueries({queryKey: ['board']}); this.toast.success('Issue created'); },
    onError: (e:any) => this.toast.error(e.error?.error || 'Create failed'),
  }));
  deleteMutation = injectMutation(() => ({
    mutationFn: (id:string) => firstValueFrom(this.ps.deleteTask(id)),
    onSuccess: () => { this.qc.invalidateQueries({queryKey:['board']}); this.deleteTarget.set(null); this.toast.success('Issue deleted'); },
    onError: (e:any) => this.toast.error(e.error?.error || 'Delete failed')
  }));
  openDetail(t:any){ this.selectedTask.set(t); this.detailOpen.set(true); }
  confirmDelete(t:any){ this.deleteTarget.set(t); }
  getTeamName(teamId?:string){ if(!teamId) return '—'; return this.teamsQuery.data()?.find((x:any)=>x.id===teamId)?.name || '—'; }
  getSprintName(sprintId?:string){ if(!sprintId) return 'Backlog'; return this.sprintsQuery.data()?.find((x:any)=>x.id===sprintId)?.name || 'Backlog'; }
  getAssigneeName(assigneeId?:string){ if(!assigneeId) return 'Unassigned'; const m = this.membersQuery.data()?.find((x:any)=>x.userId===assigneeId); return m ? m.fullName : assigneeId.slice(0,6); }
  async openCreate(){
    const lists = this.boardQuery.data()?.lists || [];
    if(lists.length>0) {
      this.createListId.set(lists[0].id);
      this.createOpen.set(true);
    } else {
      // Auto-create default To Do column for empty project so issue can be created (goes to Backlog)
      try {
        this.toast.success('Creating default To Do column...');
        const list:any = await firstValueFrom(this.ps.createList(this.projectId(), 'To Do'));
        await this.qc.invalidateQueries({queryKey:['board', this.projectId()]});
        // wait a bit for refetch
        setTimeout(async () => {
          const refreshed:any = await firstValueFrom(this.ps.getBoard(this.projectId()));
          const newList = refreshed.lists?.[0];
          if(newList){ this.createListId.set(newList.id); this.createOpen.set(true); }
          else this.toast.error('Failed to create default column — create a column in Boards first');
        }, 800);
      } catch (e:any) {
        this.toast.error(e.error?.error || 'Create a column in Boards first');
      }
    }
  }
  onCreateSubmit(e:any){
    const labelsJson = e.labels ? JSON.stringify(e.labels.split(',').map((s:string)=>s.trim()).filter(Boolean)) : undefined;
    this.createMutation.mutate({ listId: this.createListId(), title: e.title, description: e.description, priority: e.priority, labelsJson, dueDate: e.dueDate, issueType: e.issueType, teamId: e.teamId, sprintId: e.sprintId });
    this.createOpen.set(false);
  }
  onSave(e:any){ const t=this.selectedTask(); if(!t) return; this.updateMutation.mutate({ id:t.id, title:e.title, description:e.description, priority:e.priority, listId:e.listId, labelsJson:e.labelsJson, assigneeId:e.assigneeId, dueDate:e.dueDate, issueType: e.issueType, epic: e.epic, storyPoints: e.storyPoints, startDate: e.startDate, environment: e.environment, parentIssueId: e.parentIssueId, sprintId: e.sprintId, watchersJson: e.watchersJson, linkedIssuesJson: e.linkedIssuesJson, timeEstimated: e.timeEstimated, timeSpent: e.timeSpent, timeRemaining: e.timeRemaining, teamId: e.teamId }); }
}
