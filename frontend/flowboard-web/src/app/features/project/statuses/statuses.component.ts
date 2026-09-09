import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ProjectService } from '../../../core/services/project.service';
import { ToastService } from '../../../core/services/toast.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-statuses',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './statuses.component.html',
  styleUrls: ['./statuses.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatusesComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);

  projectId = signal(this.route.parent?.snapshot.paramMap.get('pid') || this.route.snapshot.paramMap.get('pid') || '');
  search = signal('');
  showCreate = signal(false);
  newName = signal('');
  editing = signal<any>(null);
  editName = signal('');
  deleteTarget = signal<any>(null);

  constructor() {
    this.route.parent?.paramMap.subscribe(m => {
      const pid = m.get('pid');
      if (pid) this.projectId.set(pid);
    });
  }

  statusesQuery = injectQuery(() => ({
    queryKey: ['statuses', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getStatuses(this.projectId())),
    enabled: !!this.projectId(),
  }));

  filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    const list = (this.statusesQuery.data() as any[]) || [];
    if (!q) return list;
    return list.filter((s:any) => s.name.toLowerCase().includes(q));
  });

  createMutation = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.ps.createStatus(this.projectId(), this.newName().trim())),
    onSuccess: () => {
      this.qc.invalidateQueries({ queryKey: ['statuses', this.projectId()] });
      this.showCreate.set(false); this.newName.set(''); this.toast.success('Status created');
    },
    onError: (e:any) => this.toast.error(e.error?.error || 'Create failed')
  }));

  updateMutation = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.ps.updateStatus(this.editing()!.id, this.editName().trim())),
    onSuccess: () => {
      this.qc.invalidateQueries({ queryKey: ['statuses', this.projectId()] });
      this.editing.set(null); this.toast.success('Status updated');
    },
    onError: (e:any) => this.toast.error(e.error?.error || 'Update failed')
  }));

  deleteMutation = injectMutation(() => ({
    mutationFn: (id:string) => firstValueFrom(this.ps.deleteStatus(id)),
    onSuccess: () => {
      this.qc.invalidateQueries({ queryKey: ['statuses', this.projectId()] });
      this.deleteTarget.set(null); this.toast.success('Status deleted');
    },
    onError: (e:any) => this.toast.error(e.error?.error || 'Delete failed')
  }));

  openCreate(){ this.newName.set(''); this.showCreate.set(true); }
  doCreate(){ if(!this.newName().trim()) return; this.createMutation.mutate(); }
  openEdit(s:any){ this.editing.set(s); this.editName.set(s.name); }
  doUpdate(){ if(!this.editName().trim()) return; this.updateMutation.mutate(); }
  openDelete(s:any){ this.deleteTarget.set(s); }
  confirmDelete(){ const t=this.deleteTarget(); if(t) this.deleteMutation.mutate(t.id); }
}
