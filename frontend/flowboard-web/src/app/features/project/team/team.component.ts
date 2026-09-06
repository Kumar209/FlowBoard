import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-project-team',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './team.component.html',
  styleUrls: ['./team.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TeamComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private ps = inject(ProjectService);
  private qc = inject(QueryClient);
  projectId = signal(this.route.parent?.snapshot.paramMap.get('pid') || this.route.snapshot.paramMap.get('pid') || '');
  workspaceId = signal(this.route.parent?.snapshot.paramMap.get('wid') || this.route.snapshot.paramMap.get('wid') || '');
  search = signal('');
  newTeamName = signal('');
  newTeamDesc = signal('');
  showCreate = signal(false);
  editingTeam = signal<any>(null);
  deleteTarget = signal<any>(null);
  selectedTeamId = signal<string | null>(null);
  addMemberSearch = signal('');
  selectedUserId = signal('');

  teamsQuery = injectQuery(() => ({
    queryKey: ['teams', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getTeams(this.projectId())),
    enabled: !!this.projectId(),
  }));

  membersQuery = injectQuery(() => ({
    queryKey: ['team-members', this.selectedTeamId()] as const,
    queryFn: () => firstValueFrom(this.ps.getTeamMembers(this.selectedTeamId()!)),
    enabled: !!this.selectedTeamId(),
  }));

  workspaceMembersQuery = injectQuery(() => ({
    queryKey: ['workspace-members', this.workspaceId()] as const,
    queryFn: () => firstValueFrom(this.ps.getWorkspaceMembers(this.workspaceId())),
    enabled: !!this.workspaceId(),
  }));

  filteredTeams = computed(() => {
    const s=this.search().toLowerCase().trim();
    const list=this.teamsQuery.data() || [];
    return s ? list.filter((t:any)=> t.name.toLowerCase().includes(s)) : list;
  });

  createMutation = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.ps.createTeam(this.projectId(), this.newTeamName().trim(), this.newTeamDesc().trim() || undefined)),
    onSuccess: () => { this.qc.invalidateQueries({queryKey:['teams', this.projectId()]}); this.showCreate.set(false); this.newTeamName.set(''); this.newTeamDesc.set(''); }
  }));
  updateMutation = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.ps.updateTeam(this.editingTeam()!.id, this.newTeamName().trim(), this.newTeamDesc().trim() || undefined)),
    onSuccess: () => { this.qc.invalidateQueries({queryKey:['teams', this.projectId()]}); this.showCreate.set(false); this.editingTeam.set(null); this.newTeamName.set(''); }
  }));
  deleteMutation = injectMutation(() => ({
    mutationFn: (id:string) => firstValueFrom(this.ps.deleteTeam(id)),
    onSuccess: () => { this.qc.invalidateQueries({queryKey:['teams', this.projectId()]}); this.deleteTarget.set(null); }
  }));
  addMemberMutation = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.ps.addTeamMember(this.selectedTeamId()!, this.selectedUserId())),
    onSuccess: () => { this.qc.invalidateQueries({queryKey:['team-members', this.selectedTeamId()]}); this.qc.invalidateQueries({queryKey:['teams', this.projectId()]}); this.selectedUserId.set(''); this.addMemberSearch.set(''); }
  }));
  removeMemberMutation = injectMutation(() => ({
    mutationFn: (userId:string) => firstValueFrom(this.ps.removeTeamMember(this.selectedTeamId()!, userId)),
    onSuccess: () => { this.qc.invalidateQueries({queryKey:['team-members', this.selectedTeamId()]}); this.qc.invalidateQueries({queryKey:['teams', this.projectId()]}); }
  }));

  openCreate(){ this.editingTeam.set(null); this.newTeamName.set(''); this.newTeamDesc.set(''); this.showCreate.set(true); }
  openEdit(t:any){ this.editingTeam.set(t); this.newTeamName.set(t.name); this.newTeamDesc.set(t.description||''); this.showCreate.set(true); }
  submit(){ if(this.editingTeam()) this.updateMutation.mutate(); else this.createMutation.mutate(); }
  confirmDelete(t:any){ this.deleteTarget.set(t); }
  selectTeam(t:any){
    const wid = this.workspaceId();
    const pid = this.projectId();
    this.router.navigate([`/w/${wid}/p/${pid}/team/${t.id}`]);
  }
  filteredWorkspaceMembers = computed(() => {
    const q=this.addMemberSearch().toLowerCase().trim();
    const list=this.workspaceMembersQuery.data() || [];
    const teamMembers=this.membersQuery.data() || [];
    const existingIds=new Set(teamMembers.map((m:any)=> m.userId));
    const available=list.filter((m:any)=> !existingIds.has(m.userId));
    if(!q) return available.slice(0,8);
    return available.filter((m:any)=> m.fullName.toLowerCase().includes(q) || m.email.toLowerCase().includes(q)).slice(0,8);
  });
}
