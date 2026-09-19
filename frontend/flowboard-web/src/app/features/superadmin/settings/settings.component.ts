import { Component, ChangeDetectionStrategy, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-superadmin-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsComponent {
  private sa = inject(SuperAdminService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);

  query = injectQuery(() => ({
    queryKey: ['superadmin-settings'] as const,
    queryFn: () => firstValueFrom(this.sa.getSettings()),
  }));

  plansQuery = injectQuery(() => ({
    queryKey: ['superadmin-plans'] as const,
    queryFn: () => firstValueFrom(this.sa.getSubscriptions()).then(r => r.plans),
  }));

  get d() {
    return this.query.data();
  }

  get plans() {
    return this.plansQuery.data() ?? [];
  }

  generalEdit = signal<any>(null);
  securityEdit = signal<any>(null);
  aiEdit = signal<any>(null);
  rateEdit = signal<any>(null);
  maintEdit = signal<any>(null);
  tenantEdit = signal<any>(null);
  logoFile = signal<File | null>(null);

  constructor() {
    effect(() => {
      const data = this.query.data();
      if (!data) return;
      if (!this.generalEdit()) this.generalEdit.set({ ...data.general });
      if (!this.securityEdit()) this.securityEdit.set({ ...data.security });
      if (!this.aiEdit()) this.aiEdit.set(JSON.parse(JSON.stringify(data.ai)));
      if (!this.rateEdit()) this.rateEdit.set({ ...data.rateLimits });
      if (!this.maintEdit()) this.maintEdit.set({ ...data.maintenance });
      if (!this.tenantEdit()) this.tenantEdit.set({ ...data.tenantDefaults });
    }, { allowSignalWrites: true });
  }

  saveGeneral = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.sa.updateGeneral(this.generalEdit())),
    onSuccess: () => {
      this.toast.success('Platform updated');
      this.qc.invalidateQueries({ queryKey: ['superadmin-settings'] });
      this.qc.invalidateQueries({ queryKey: ['platform-general'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Save failed')
  }));

  saveSecurity = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.sa.updateSecurity(this.securityEdit())),
    onSuccess: () => {
      this.toast.success('Security updated');
      this.qc.invalidateQueries({ queryKey: ['superadmin-settings'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Save failed')
  }));

  saveTenant = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.sa.updateTenantDefaults(this.tenantEdit())),
    onSuccess: () => {
      this.toast.success('Tenant defaults updated');
      this.qc.invalidateQueries({ queryKey: ['superadmin-settings'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Save failed')
  }));

  saveAi = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.sa.updateAi(this.aiEdit())),
    onSuccess: () => {
      this.toast.success('AI settings updated');
      this.qc.invalidateQueries({ queryKey: ['superadmin-settings'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Save failed')
  }));

  saveRate = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.sa.updateRateLimits(this.rateEdit())),
    onSuccess: () => {
      this.toast.success('Rate limits updated');
      this.qc.invalidateQueries({ queryKey: ['superadmin-settings'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Save failed')
  }));

  saveMaint = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.sa.updateMaintenance(this.maintEdit())),
    onSuccess: () => {
      this.toast.success('Maintenance updated');
      this.qc.invalidateQueries({ queryKey: ['superadmin-settings'] });
      this.qc.invalidateQueries({ queryKey: ['platform-maintenance'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Save failed')
  }));

  uploadLogoMut = injectMutation(() => ({
    mutationFn: () => {
      const f = this.logoFile();
      if (!f) throw new Error('No file');
      return firstValueFrom(this.sa.uploadLogo(f));
    },
    onSuccess: (res: any) => {
      this.toast.success('Logo uploaded');
      if (this.generalEdit()) {
        this.generalEdit.set({ ...this.generalEdit(), logoUrl: res.logoUrl });
      }
      this.logoFile.set(null);
      this.qc.invalidateQueries({ queryKey: ['superadmin-settings'] });
      this.qc.invalidateQueries({ queryKey: ['platform-general'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Upload failed')
  }));

  purgeConfirm = signal('');
  purgeDialogOpen = signal(false);

  purgeMut = injectMutation(() => ({
    mutationFn: () => firstValueFrom(this.sa.purgeAll()),
    onSuccess: (res: any) => {
      this.toast.success(`Purged ${res.deletedOrganizations} orgs, ${res.deletedUsers} users, ${res.deletedProjects} projects`);
      this.purgeDialogOpen.set(false);
      this.purgeConfirm.set('');
      this.qc.invalidateQueries({ queryKey: ['superadmin-settings'] });
      this.qc.invalidateQueries({ queryKey: ['superadmin-dashboard'] });
      this.qc.invalidateQueries({ queryKey: ['superadmin-organizations'] });
      this.qc.invalidateQueries({ queryKey: ['superadmin-users'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Purge failed')
  }));

  onLogoSelected(e: Event) {
    const input = e.target as HTMLInputElement;
    if (input.files?.[0]) this.logoFile.set(input.files[0]);
  }
}