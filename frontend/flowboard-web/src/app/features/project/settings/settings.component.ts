import { Component, ChangeDetectionStrategy, inject, signal, effect, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { PermissionService } from '../../../core/services/permission.service';
import { PermissionKeys } from '../../../shared/constants/permissions';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-project-settings',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './settings.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  private perm = inject(PermissionService);
  private qc = inject(QueryClient);

  projectId = signal(this.route.parent?.snapshot.paramMap.get('pid') || '');
  workspaceId = signal(this.route.parent?.snapshot.paramMap.get('wid') || this.route.snapshot.paramMap.get('wid') || '');
  name = signal('');
  showDeleteConfirm = signal(false);
  PermissionKeys = PermissionKeys;
  canView = computed(() => this.perm.hasPermissionSync(this.workspaceId(), PermissionKeys.ProjectView));
  canUpdate = computed(() => this.perm.hasPermissionSync(this.workspaceId(), PermissionKeys.ProjectUpdate));
  canDelete = computed(() => this.perm.hasPermissionSync(this.workspaceId(), PermissionKeys.ProjectDelete));

  boardQuery = injectQuery(() => ({
    queryKey: ['board', this.projectId()] as const,
    queryFn: () => firstValueFrom(this.ps.getBoard(this.projectId())),
    enabled: !!this.projectId()
  }));

  constructor() {
    effect(() => {
      const projectName = this.boardQuery.data()?.project?.name;
      if (projectName && !this.name()) {
        this.name.set(projectName);
      }
    });
  }

  updateMut = injectMutation(() => ({
    mutationFn: () => firstValueFrom(
      this.ps.updateProject(this.projectId(), this.name().trim())
    ),
    onSuccess: () => {
      this.qc.invalidateQueries({ queryKey: ['board', this.projectId()] });
      this.qc.invalidateQueries({ queryKey: ['projects'] });
      alert('Project updated');
    },
    onError: (e: any) => alert(e.error?.error || 'Update failed')
  }));

  deleteMut = injectMutation(() => ({
    mutationFn: (): Promise<any> => firstValueFrom(
      this.ps.deleteProject(this.projectId())
    ),
    onSuccess: () => {
      this.showDeleteConfirm.set(false);
      alert('Project deleted');
      location.href = '/w';
    },
    onError: (e: any) => alert(e.error?.error || 'Delete failed')
  }));

  closeDelete() {
    this.showDeleteConfirm.set(false);
  }
}