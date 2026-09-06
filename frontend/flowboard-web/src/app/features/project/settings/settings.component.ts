import { Component, ChangeDetectionStrategy, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-project-settings',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  private qc = inject(QueryClient);
  projectId = signal(this.route.parent?.snapshot.paramMap.get('pid') || '');
  name = signal('');
  boardQuery = injectQuery(() => ({
    queryKey: ['board', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getBoard(this.projectId())),
    enabled: !!this.projectId(),
  }));
  showDeleteConfirm = signal(false);
  constructor(){
    effect(()=>{
      const p=this.boardQuery.data()?.project?.name;
      if(p && !this.name()) this.name.set(p);
    });
  }
  updateMut = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.ps.updateProject(this.projectId(), this.name().trim())),
    onSuccess: () => { this.qc.invalidateQueries({queryKey:['board', this.projectId()]}); this.qc.invalidateQueries({queryKey:['projects']}); alert('Project updated'); },
    onError: (e:any)=> alert(e.error?.error||'Update failed'),
  }));
  deleteMut = injectMutation(() => ({
    mutationFn: (): Promise<any> => firstValueFrom(this.ps.deleteProject(this.projectId())),
    onSuccess: () => { this.showDeleteConfirm.set(false); alert('Project deleted'); location.href='/w'; },
    onError: (e:any)=> alert(e.error?.error||'Delete failed'),
  }));
}
