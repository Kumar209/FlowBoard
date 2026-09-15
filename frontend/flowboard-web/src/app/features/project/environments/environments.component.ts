import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { ToastService } from '../../../core/services/toast.service';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';

@Component({
  selector: 'app-environments',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './environments.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EnvironmentsComponent {
  private route = inject(ActivatedRoute);
  private ps = inject(ProjectService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);

  projectId = signal(
    this.route.parent?.snapshot.paramMap.get('pid') ||
    this.route.snapshot.paramMap.get('pid') ||
    ''
  );

  envsQuery = injectQuery(() => ({
    queryKey: ['environments', this.projectId()] as const,
    queryFn: () =>
      firstValueFrom(
        this.ps.getEnvironments(this.projectId())
      ),
    enabled: !!this.projectId()
  }));

  showCreate = signal(false);
  editing = signal<any>(null);
  deleteTarget = signal<any>(null);

  name = signal('');
  url = signal('');
  description = signal('');
  status = signal('Active');

  environments = () =>
    (this.envsQuery.data() as any[]) || [];

  createMutation = injectMutation(() => ({
    mutationFn: () =>
      firstValueFrom(
        this.ps.createEnvironment(
          this.projectId(),
          this.name().trim(),
          this.url().trim(),
          this.description().trim() || undefined,
          this.status()
        )
      ),
    onSuccess: () => {
      this.qc.invalidateQueries({
        queryKey: ['environments', this.projectId()]
      });

      this.reset();
      this.toast.success('Environment created');
    },
    onError: (e: any) =>
      this.toast.error(
        e.error?.error || 'Create failed'
      )
  }));

  updateMutation = injectMutation(() => ({
    mutationFn: () =>
      firstValueFrom(
        this.ps.updateEnvironment(
          this.editing()!.id,
          this.name().trim(),
          this.url().trim(),
          this.description().trim() || undefined,
          this.status()
        )
      ),
    onSuccess: () => {
      this.qc.invalidateQueries({
        queryKey: ['environments', this.projectId()]
      });

      this.reset();
      this.toast.success('Environment updated');
    },
    onError: (e: any) =>
      this.toast.error(
        e.error?.error || 'Update failed'
      )
  }));

  deleteMutation = injectMutation(() => ({
    mutationFn: (id: string) =>
      firstValueFrom(
        this.ps.deleteEnvironment(id)
      ),
    onSuccess: () => {
      this.qc.invalidateQueries({
        queryKey: ['environments', this.projectId()]
      });

      this.deleteTarget.set(null);
      this.toast.success('Environment deleted');
    },
    onError: (e: any) =>
      this.toast.error(
        e.error?.error || 'Delete failed'
      )
  }));

  openCreate() {
    this.editing.set(null);
    this.name.set('');
    this.url.set('');
    this.description.set('');
    this.status.set('Active');
    this.showCreate.set(true);
  }

  openEdit(env: any) {
    this.editing.set(env);
    this.name.set(env.name);
    this.url.set(env.url || '');
    this.description.set(env.description || '');
    this.status.set(env.status || 'Active');
    this.showCreate.set(true);
  }

  submit() {
    if (!this.name().trim()) return;

    if (this.editing()) {
      this.updateMutation.mutate();
    } else {
      this.createMutation.mutate();
    }
  }

  reset() {
    this.showCreate.set(false);
    this.editing.set(null);
    this.name.set('');
    this.url.set('');
    this.description.set('');
    this.status.set('Active');
  }

  confirmDelete(env: any) {
    this.deleteTarget.set(env);
  }

  closeDelete() {
    if (this.deleteMutation.isPending()) return;
    this.deleteTarget.set(null);
  }

  doDelete() {
    const target = this.deleteTarget();

    if (!target || this.deleteMutation.isPending()) return;

    this.deleteMutation.mutate(target.id);
  }

  statusClass(status: string) {
    if (status === 'Active') {
      return 'bg-success/10 text-success border-success/20';
    }

    if (status === 'Maintenance') {
      return 'bg-warning/10 text-warning border-warning/20';
    }

    return 'bg-base-200 text-base-content/65 border-base-300';
  }
}