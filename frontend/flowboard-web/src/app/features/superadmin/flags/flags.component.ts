import { Component, ChangeDetectionStrategy, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { injectQuery, injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';
import { SuperAdminService } from '../../../core/services/superadmin.service';
import { ToastService } from '../../../core/services/toast.service';
import { FeatureFlagService } from '../../../core/services/feature-flag.service';

@Component({
  selector: 'app-superadmin-flags',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './flags.component.html',
  styleUrls: ['./flags.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FlagsComponent {
  private sa = inject(SuperAdminService);
  private toast = inject(ToastService);
  private qc = inject(QueryClient);
  private flagService = inject(FeatureFlagService);
  tab = signal<'global' | 'overrides'>('global');
  orgSearch = signal('');
  orgPage = signal(1);
  orgPageSize = 10;

  query = injectQuery(() => ({
    queryKey: ['superadmin-flags'] as const,
    queryFn: () => firstValueFrom(this.sa.getFeatureFlags()),
  }));

  orgsQuery = injectQuery(() => ({
    queryKey: ['superadmin-orgs-flags', this.orgSearch(), this.orgPage()] as const,
    queryFn: () => firstValueFrom(this.sa.getOrganizations(this.orgSearch() || undefined, this.orgPage(), this.orgPageSize)),
    enabled: this.tab() === 'overrides',
  }));

  get flags() { return (this.query.data() ?? []) as any[]; }
  get aiFlags() { return this.flags.filter(f => ['ai_draft','ai_breakdown','ai_enhance_description','ai_generate_criteria'].includes(f.key)); }
  get orgs() { return (this.orgsQuery.data()?.items ?? []) as any[]; }
  get orgTotal() { return this.orgsQuery.data()?.total ?? 0; }
  get orgTotalPages() { return Math.max(1, Math.ceil(this.orgTotal / this.orgPageSize)); }

  orgFlagsMap = signal<Map<string, any[]>>(new Map());

  toggleMutation = injectMutation(() => ({
    mutationFn: (key: string) => firstValueFrom(this.sa.toggleFlag(key)),
    onSuccess: () => {
      this.toast.success('Flag updated');
      try { this.flagService.clearOrgCache(); } catch {}
      this.qc.invalidateQueries({ queryKey: ['superadmin-flags'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Toggle failed')
  }));

  orgToggleMutation = injectMutation(() => ({
    mutationFn: ({ orgId, key }: { orgId: string; key: string }) => firstValueFrom(this.sa.toggleOrgFlag(orgId, key)),
    onSuccess: (_: any, vars: any) => {
      this.toast.success('Org override updated');
      // Clear per-org cache so tenant sees fresh on next check
      try { (this as any).flagService?.clearOrgCache?.(vars.orgId); } catch {}
      firstValueFrom(this.sa.getOrgFeatureFlags(vars.orgId)).then(flags => {
        const m = new Map(this.orgFlagsMap());
        m.set(vars.orgId, flags);
        this.orgFlagsMap.set(m);
      });
      this.qc.invalidateQueries({ queryKey: ['superadmin-flags'] });
      this.qc.invalidateQueries({ queryKey: ['superadmin-orgs-flags'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Toggle failed')
  }));

  constructor() {
    effect(() => {
      if (this.tab() !== 'overrides') return;
      const orgs = this.orgs;
      if (!orgs.length) return;
      orgs.forEach(o => {
        if (this.orgFlagsMap().has(o.id)) return;
        firstValueFrom(this.sa.getOrgFeatureFlags(o.id)).then(flags => {
          const m = new Map(this.orgFlagsMap());
          m.set(o.id, flags);
          this.orgFlagsMap.set(m);
        });
      });
    }, { allowSignalWrites: true });
  }

  onToggle(flag: any) { (this.toggleMutation as any).mutate(flag.key); }
  onOrgToggle(org: any, flag: any) { (this.orgToggleMutation as any).mutate({ orgId: org.id, key: flag.key }); }
  getOrgFlag(orgId: string, key: string): boolean | null {
    const arr = this.orgFlagsMap().get(orgId);
    if (!arr) return null;
    const f = arr.find((x: any) => x.key === key);
    return f ? !!f.isEnabled : null;
  }
  onOrgSearch(v: string) { this.orgSearch.set(v); this.orgPage.set(1); this.orgFlagsMap.set(new Map()); }
  prevOrg() { if (this.orgPage() > 1) { this.orgPage.update(v => v - 1); this.orgFlagsMap.set(new Map()); } }
  nextOrg() { if (this.orgPage() < this.orgTotalPages) { this.orgPage.update(v => v + 1); this.orgFlagsMap.set(new Map()); } }
}
