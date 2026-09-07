import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { ToastService } from '../../../core/services/toast.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-project-members',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './project-members.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectMembersComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);

  projectId = signal(this.route.snapshot.paramMap.get('pid') || this.route.parent?.snapshot.paramMap.get('pid') || '');
  workspaceId = signal(this.route.snapshot.paramMap.get('wid') || this.route.parent?.snapshot.paramMap.get('wid') || '');

  constructor(){
    this.route.paramMap.subscribe(m => {
      const pid=m.get('pid'); if(pid) this.projectId.set(pid);
      const wid=m.get('wid'); if(wid) this.workspaceId.set(wid);
    });
    this.route.parent?.paramMap.subscribe(m => {
      const pid=m.get('pid'); if(pid) this.projectId.set(pid);
      const wid=m.get('wid'); if(wid) this.workspaceId.set(wid);
    });
  }

  search = signal('');
  addSearch = signal('');
  page = signal(1);
  pageSize = 8;
  addPage = signal(1);
  addPageSize = 8;

  membersQuery = injectQuery(() => ({
    queryKey: ['project-members', this.projectId(), this.search(), this.page()] as const,
    queryFn: async () => {
      const res:any = await firstValueFrom(this.ps.getProjectMembers(this.projectId(), this.page(), this.pageSize, this.search()||undefined));
      return res;
    },
    enabled: !!this.projectId(),
  }));

  workspaceMembersQuery = injectQuery(() => ({
    queryKey: ['workspace-members', this.workspaceId(), this.addSearch(), this.addPage()] as const,
    queryFn: async () => {
      const res:any = await firstValueFrom(this.ps.getWorkspaceMembersPaged(this.workspaceId(), this.addPage(), this.addPageSize, this.addSearch()||undefined));
      if(res.items) return {items: res.items as any[], total: res.total as number};
      const arr=res as any[]; return {items: arr, total: arr.length};
    },
    enabled: !!this.workspaceId(),
  }));
  workspaceItems = computed(()=> this.workspaceMembersQuery.data()?.items || []);
  workspaceTotal = computed(()=> this.workspaceMembersQuery.data()?.total || 0);

  paginatedMembers = computed(()=> this.membersQuery.data()?.items || []);
  totalPages = computed(()=> Math.max(1, Math.ceil((this.membersQuery.data()?.total||0)/this.pageSize)));

  filteredAvailable = computed(()=>{
    const list=this.workspaceItems();
    const existing=new Set((this.paginatedMembers()||[]).map((m:any)=>m.userId));
    // Also need full project members list to exclude? Use current page's members for now
    return list.filter((m:any)=>!existing.has(m.userId));
  });
  totalAddPages = computed(()=> Math.max(1, Math.ceil(this.workspaceTotal()/this.addPageSize)));

  addMutation = injectMutation(()=>({
    mutationFn: (userId:string)=> firstValueFrom(this.ps.addProjectMember(this.projectId(), userId)),
    onSuccess: ()=>{ this.qc.invalidateQueries({queryKey:['project-members', this.projectId()]}); this.toast.success('Member added to project'); this.addSearch.set(''); },
    onError: (e:any)=> this.toast.error(e.error?.error||'Add failed')
  }));
  removeMutation = injectMutation(()=>({
    mutationFn: (userId:string)=> firstValueFrom(this.ps.removeProjectMember(this.projectId(), userId)),
    onSuccess: ()=>{ this.qc.invalidateQueries({queryKey:['project-members', this.projectId()]}); this.toast.success('Member removed'); },
    onError: (e:any)=> this.toast.error(e.error?.error||'Remove failed')
  }));

  add(userId:string){ this.addMutation.mutate(userId); }
  remove(userId:string){ if(confirm('Remove from project? Also removes from teams.')) this.removeMutation.mutate(userId); }
}
