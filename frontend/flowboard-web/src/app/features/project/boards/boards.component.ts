import { Component, ChangeDetectionStrategy, signal, inject, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { ToastService } from '../../../core/services/toast.service';
import { PermissionService } from '../../../core/services/permission.service';
import { AuthService } from '../../../core/services/auth.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-boards',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './boards.component.html',
  styleUrls: ['./boards.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BoardsComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);
  private perm = inject(PermissionService);
  private auth = inject(AuthService);
  workspaceId = signal(this.route.parent?.snapshot.paramMap.get('wid') || this.route.snapshot.paramMap.get('wid') || '');
  projectId = signal(this.route.parent?.snapshot.paramMap.get('pid') || '');
  canCreate = signal(false);
  canUpdate = signal(false);
  canDelete = signal(false);
  constructor() {
    effect(async () => {
      const wid = this.workspaceId();
      if (!wid) return;
      // Prefer me cache (B) — single source, no extra HTTP if me already has perms
      const hasCreate = this.auth.hasPermission(wid, 'board:create');
      const hasUpdate = this.auth.hasPermission(wid, 'board:update');
      const hasDelete = this.auth.hasPermission(wid, 'board:delete');
      if (hasCreate || hasUpdate || hasDelete) {
        this.canCreate.set(hasCreate || this.auth.isOrgAdmin() || this.auth.isSuperAdmin());
        this.canUpdate.set(hasUpdate || this.auth.isOrgAdmin() || this.auth.isSuperAdmin());
        this.canDelete.set(hasDelete || this.auth.isOrgAdmin() || this.auth.isSuperAdmin());
        return;
      }
      try {
        const c = await this.perm.hasPermission(wid, 'board:create');
        const u = await this.perm.hasPermission(wid, 'board:update');
        const d = await this.perm.hasPermission(wid, 'board:delete');
        this.canCreate.set(c || this.auth.isOrgAdmin() || this.auth.isSuperAdmin());
        this.canUpdate.set(u || this.auth.isOrgAdmin() || this.auth.isSuperAdmin());
        this.canDelete.set(d || this.auth.isOrgAdmin() || this.auth.isSuperAdmin());
      } catch { this.canCreate.set(this.auth.isOrgAdmin() || this.auth.isSuperAdmin()); }
    }, { allowSignalWrites: true });
  }
  boardsQuery = injectQuery(() => ({
    queryKey: ['boards', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getBoards(this.projectId())),
    enabled: !!this.projectId(),
  }));
  teamsQuery = injectQuery(() => ({
    queryKey: ['teams', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getTeams(this.projectId())),
    enabled: !!this.projectId(),
  }));
  displayBoards = computed(() => {
    const api = this.boardsQuery.data();
    if (api && api.length>0) return api.map((b:any)=> {
      let filterInfo = '';
      try { const f = b.filterJson ? JSON.parse(b.filterJson) : null; if (f?.teamIds?.length) filterInfo = ` • Team filter ${f.teamIds.length}`; } catch {}
      return {id:b.id, name:b.name, desc:`${b.type} • ${b.description||''}${filterInfo}`, tasks:0, type:b.type, color: b.type==='Scrum'?'🟣':'🔵', filterJson: b.filterJson};
    });
    return [];
  });
  showCreate = signal(false);
  editingBoard = signal<any>(null);
  deleteTarget = signal<any>(null);
  newName = signal('');
  newType = signal('Kanban');
  selectedTeam = signal('');
  createMutation = injectMutation(() => ({
    mutationFn: () => {
      const filter = this.selectedTeam() ? JSON.stringify({teamIds:[this.selectedTeam()]}) : null;
      return firstValueFrom(this.ps.createBoard(this.projectId(), this.newName().trim(), this.newType(), undefined, filter));
    },
    onSuccess: () => { this.qc.invalidateQueries({queryKey:['boards', this.projectId()]}); this.qc.invalidateQueries({queryKey:['board', this.projectId()]}); this.showCreate.set(false); this.newName.set(''); this.newType.set('Kanban'); this.selectedTeam.set(''); this.toast.success('Board created'); },
    onError: (e:any) => this.toast.error(e.error?.error || e.message || 'Create board failed')
  }));
  updateMutation = injectMutation(() => ({
    mutationFn: () => {
      const filter = this.selectedTeam() ? JSON.stringify({teamIds:[this.selectedTeam()]}) : null;
      return firstValueFrom(this.ps.updateBoard(this.editingBoard()!.id, this.newName().trim(), this.newType(), filter));
    },
    onSuccess: () => { this.qc.invalidateQueries({queryKey:['boards', this.projectId()]}); this.editingBoard.set(null); this.showCreate.set(false); this.newName.set(''); this.selectedTeam.set(''); this.toast.success('Board updated'); },
    onError: (e:any)=> this.toast.error(e.error?.error||'Update failed'),
  }));
  deleteMutation = injectMutation(() => ({
    mutationFn: (id:string) => firstValueFrom(this.ps.deleteBoard(id)),
    onSuccess: () => { this.qc.invalidateQueries({queryKey:['boards', this.projectId()]}); this.deleteTarget.set(null); this.toast.success('Board deleted'); },
    onError: (e:any)=> { this.toast.error(e.error?.error || e.message || 'Delete failed'); this.deleteTarget.set(null); },
  }));
  create(){
    if(this.editingBoard()) this.updateMutation.mutate();
    else this.createMutation.mutate();
  }
  openCreate(){
    this.editingBoard.set(null);
    this.newName.set('');
    this.newType.set('Kanban');
    this.selectedTeam.set('');
    this.showCreate.set(true);
  }
  openEdit(b:any){
    this.editingBoard.set(b); this.newName.set(b.name); this.newType.set(b.type);
    try {
      const f = b.filterJson ? JSON.parse(b.filterJson) : null;
      const rawId = f?.teamIds?.[0] || '';
      // Normalize to actual team id case from teams list (DB may store lowercase)
      const teams = this.teamsQuery.data() || [];
      const matched = teams.find((t:any) => t.id.toLowerCase() === rawId.toLowerCase());
      this.selectedTeam.set(matched ? matched.id : rawId);
    } catch { this.selectedTeam.set(''); }
    this.showCreate.set(true);
  }
  confirmDelete(b:any){ this.deleteTarget.set(b); }
}
