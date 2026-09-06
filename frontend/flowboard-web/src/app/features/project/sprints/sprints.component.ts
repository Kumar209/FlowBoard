import { Component, ChangeDetectionStrategy, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { ToastService } from '../../../core/services/toast.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-sprints',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './sprints.component.html',
  styleUrls: ['./sprints.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SprintsComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);
  projectId = signal(this.route.parent?.snapshot.paramMap.get('pid') || '');
  search = signal('');
  page = signal(1);
  pageSize = 8;
  // Boards for BoardId when creating sprint (Board → Sprint)
  boardsQuery = injectQuery(() => ({
    queryKey: ['boards', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getBoards(this.projectId())),
    enabled: !!this.projectId(),
  }));
  sprintsQuery = injectQuery(() => ({
    queryKey: ['sprints', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getSprints(this.projectId())),
    enabled: !!this.projectId(),
  }));
  boardQuery = injectQuery(() => ({
    queryKey: ['board', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getBoard(this.projectId())),
    enabled: !!this.projectId(),
  }));
  // Fallback to in-memory if API empty (for demo)
  fallbackSprints = signal([
    { id:'1', name:'Sprint 1 — Auth & Board', start:'2026-09-01', end:'2026-09-14', tasks: 12, done: 4 },
    { id:'2', name:'Sprint 2 — Realtime & Files', start:'2026-09-15', end:'2026-09-28', tasks: 8, done: 1 },
  ]);
  sprints = signal<any[]>([]); // kept for backward compat, not used now
  showCreate = signal(false);
  editingSprint = signal<any>(null);
  newName = signal('');
  newStart = signal('');
  newEnd = signal('');
  deleteTarget = signal<any>(null);
  sprintIssueCounts = computed(() => {
    const tasks = this.boardQuery.data()?.tasks || [];
    const lists = this.boardQuery.data()?.lists || [];
    const map = new Map<string, {total:number; done:number}>();
    for (const t of tasks as any[]) {
      if (!t.sprintId) continue;
      const entry = map.get(t.sprintId) || {total:0, done:0};
      entry.total++;
      const isDone = (t as any).status === 'Done' || lists.find((l:any)=>l.id===t.listId)?.name === 'Done';
      if (isDone) entry.done++;
      map.set(t.sprintId, entry);
    }
    return map;
  });
  scrumBoardId = computed(() => {
    const boards = this.boardsQuery.data() || [];
    const scrum = boards.find((b:any) => b.type === 'Scrum');
    return scrum?.id || boards[0]?.id || '';
  });
  displaySprints = computed(() => {
    const api = this.sprintsQuery.data();
    const counts = this.sprintIssueCounts();
    if (api && api.length > 0) return api.map((s:any) => {
      const c = counts.get(s.id) || {total:0, done:0};
      return { id:s.id, name:s.name, start: (s.startDate||s.start||'').slice(0,10), end:(s.endDate||s.end||'').slice(0,10), tasks:c.total, done:c.done, status: s.status || 'Planned' };
    });
    return [];
  });
  filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    const list = this.displaySprints();
    if(!q) return list;
    return list.filter((s:any) => s.name.toLowerCase().includes(q));
  });
  totalPages = computed(() => Math.max(1, Math.ceil(this.filtered().length / this.pageSize)));
  paginated = computed(() => {
    const start = (this.page()-1)*this.pageSize;
    return this.filtered().slice(start, start+this.pageSize);
  });

  createMutation = injectMutation(() => ({
    mutationFn: () => {
      return firstValueFrom(this.ps.createSprint(this.projectId(), null, this.newName().trim(), this.newStart() || new Date().toISOString().slice(0,10), this.newEnd() || new Date().toISOString().slice(0,10)));
    },
    onSuccess: () => { this.qc.invalidateQueries({ queryKey: ['sprints', this.projectId()] }); this.qc.invalidateQueries({ queryKey: ['board'] }); this.showCreate.set(false); this.editingSprint.set(null); this.newName.set(''); this.newStart.set(''); this.newEnd.set(''); this.toast.success('Sprint created'); },
    onError: (e:any) => this.toast.error(e?.error?.error || e?.message || 'Create sprint failed')
  }));
  updateMutation = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.ps.updateSprint(this.editingSprint()!.id, this.newName().trim(), this.newStart() || new Date().toISOString().slice(0,10), this.newEnd() || new Date().toISOString().slice(0,10))),
    onSuccess: () => { this.qc.invalidateQueries({ queryKey: ['sprints', this.projectId()] }); this.showCreate.set(false); this.editingSprint.set(null); this.newName.set(''); this.newStart.set(''); this.newEnd.set(''); this.toast.success('Sprint updated'); },
    onError: (e:any) => this.toast.error(e?.error?.error || e?.message || 'Update failed')
  }));
  create(){
    if(this.editingSprint()) this.updateMutation.mutate();
    else this.createMutation.mutate();
  }
  openCreate(){ this.editingSprint.set(null); this.newName.set(''); this.newStart.set(''); this.newEnd.set(''); this.showCreate.set(true); }
  openEdit(s:any){ this.editingSprint.set(s); this.newName.set(s.name); this.newStart.set(s.start); this.newEnd.set(s.end); this.showCreate.set(true); }
  confirmDelete(s:any){ this.deleteTarget.set(s); }
  deleteMutation = injectMutation(() => ({
    mutationFn: (id:string) => firstValueFrom(this.ps.deleteSprint(id)),
    onSuccess: () => { this.qc.invalidateQueries({ queryKey: ['sprints', this.projectId()] }); this.qc.invalidateQueries({ queryKey: ['board'] }); this.deleteTarget.set(null); this.toast.success('Sprint deleted'); },
    onError: () => {
      // Fallback local delete
      const t=this.deleteTarget(); if(!t) return;
      this.fallbackSprints.update(s=>s.filter(x=>x.id!==t.id));
      this.sprints.update(s=>s.filter((x:any)=>x.id!==t.id));
      this.deleteTarget.set(null);
    }
  }));
  deleteConfirm(){
    const t=this.deleteTarget(); if(!t) return;
    // Try API if id looks like GUID (has -), else fallback
    if (t.id.includes('-') && t.id.length>20) this.deleteMutation.mutate(t.id);
    else {
      this.fallbackSprints.update(s=>s.filter(x=>x.id!==t.id));
      this.deleteTarget.set(null);
    }
  }
}
