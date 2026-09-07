import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { ToastService } from '../../../core/services/toast.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-team-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './team-detail.component.html',
  styleUrls: ['./team-detail.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TeamDetailComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);

  projectId = signal(this.route.snapshot.paramMap.get('pid') || this.route.parent?.snapshot.paramMap.get('pid') || '');
  workspaceId = signal(this.route.snapshot.paramMap.get('wid') || this.route.parent?.snapshot.paramMap.get('wid') || '');
  teamId = signal(this.route.snapshot.paramMap.get('teamId') || '');

  constructor() {
    this.route.paramMap.subscribe(m => {
      const tid = m.get('teamId'); if(tid) this.teamId.set(tid);
      const pid = m.get('pid'); if(pid) this.projectId.set(pid);
      const wid = m.get('wid'); if(wid) this.workspaceId.set(wid);
    });
    this.route.parent?.paramMap.subscribe(m => {
      const pid = m.get('pid'); if(pid) this.projectId.set(pid);
      const wid = m.get('wid'); if(wid) this.workspaceId.set(wid);
    });
  }

  teamQuery = injectQuery(() => ({
    queryKey: ['teams', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getTeams(this.projectId())),
    enabled: !!this.projectId(),
  }));
  team = computed(() => (this.teamQuery.data() || []).find((t:any) => t.id === this.teamId()));

  membersQuery = injectQuery(() => ({
    queryKey: ['team-members', this.teamId()] as const,
    queryFn: () => firstValueFrom(this.ps.getTeamMembers(this.teamId())),
    enabled: !!this.teamId(),
  }));

  workspaceMembersQuery = injectQuery(() => ({
    queryKey: ['workspace-members', this.workspaceId(), this.addSearch(), this.addPage()] as const,
    queryFn: async () => {
      const res:any = await firstValueFrom(this.ps.getWorkspaceMembersPaged(this.workspaceId(), this.addPage(), this.addPageSize, this.addSearch() || undefined));
      // Backend returns {items, total} when paginated (normalize to same shape)
      if (res.items) return { items: res.items as any[], total: res.total as number };
      const arr = res as any[];
      return { items: arr, total: arr.length };
    },
    enabled: !!this.workspaceId(),
  }));
  workspaceMembersItems = computed(() => this.workspaceMembersQuery.data()?.items || []);
  workspaceMembersTotalRaw = computed(() => this.workspaceMembersQuery.data()?.total || 0);

  search = signal('');
  addSearch = signal('');
  selectedUserId = signal('');
  page = signal(1);
  pageSize = 8;
  addPage = signal(1);
  addPageSize = 8;

  // Map team members to full workspace user info (name, email, role)
  // Note: workspace list is paginated (pageSize 8), so map may be partial — fallback keeps name from team data
  enrichedMembers = computed(() => {
    const teamMembers = this.membersQuery.data() || [];
    const wsMembers = this.workspaceMembersItems() as any[];
    const map = new Map<string, any>(wsMembers.map((m:any) => [m.userId, m]));
    return teamMembers.map((tm:any) => {
      const ws:any = map.get(tm.userId);
      return { ...tm, fullName: ws?.fullName || tm.userId.slice(0,8), email: ws?.email || '', role: ws?.role || 'Member', avatarUrl: ws?.avatarUrl };
    });
  });

  filteredMembers = computed(() => {
    const q = this.search().toLowerCase().trim();
    const list = this.enrichedMembers();
    if(!q) return list;
    return list.filter((m:any) => m.fullName.toLowerCase().includes(q) || m.email.toLowerCase().includes(q) || m.role.toLowerCase().includes(q));
  });
  totalPages = computed(() => Math.max(1, Math.ceil(this.filteredMembers().length / this.pageSize)));
  paginatedMembers = computed(() => {
    const start = (this.page()-1)*this.pageSize;
    return this.filteredMembers().slice(start, start+this.pageSize);
  });

  filteredWorkspaceMembers = computed(() => {
    const list = this.workspaceMembersItems();
    const teamMembers = this.membersQuery.data() || [];
    const existing = new Set(teamMembers.map((m:any) => m.userId));
    // Server already filters by addSearch, so only exclude team members here (handles 100s via API pagination)
    return list.filter((m:any) => !existing.has(m.userId));
  });
  totalAddPages = computed(() => Math.max(1, Math.ceil(this.workspaceMembersTotalRaw() / this.addPageSize)));
  paginatedAvailableMembers = computed(() => this.filteredWorkspaceMembers());

  addMutation = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.ps.addTeamMember(this.teamId(), this.selectedUserId())),
    onSuccess: () => {
      this.qc.invalidateQueries({queryKey:['team-members', this.teamId()]});
      this.qc.invalidateQueries({queryKey:['teams', this.projectId()]});
      this.toast.success('Member added');
      this.selectedUserId.set(''); this.addSearch.set('');
    },
    onError: (e:any) => this.toast.error(e.error?.error || 'Add failed')
  }));

  removeMutation = injectMutation(() => ({
    mutationFn: (userId:string) => firstValueFrom(this.ps.removeTeamMember(this.teamId(), userId)),
    onSuccess: () => {
      this.qc.invalidateQueries({queryKey:['team-members', this.teamId()]});
      this.qc.invalidateQueries({queryKey:['teams', this.projectId()]});
      this.toast.success('Member removed');
    },
    onError: (e:any) => this.toast.error(e.error?.error || 'Remove failed')
  }));

  remove(member:any){ if(confirm(`Remove ${member.userId} from team?`)) this.removeMutation.mutate(member.userId); }
}
