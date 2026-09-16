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
  orgPageSize = signal(10);

  query = injectQuery(() => ({
    queryKey: ['superadmin-flags'] as const,
    queryFn: () => firstValueFrom(this.sa.getFeatureFlags()),
  }));

  orgsQuery = injectQuery(() => ({
    queryKey: ['superadmin-orgs-flags', this.orgSearch(), this.orgPage(), this.orgPageSize()] as const,
    queryFn: () => firstValueFrom(
      this.sa.getOrganizations(
        this.orgSearch() || undefined,
        this.orgPage(),
        this.orgPageSize()
      )
    ),
    enabled: this.tab() === 'overrides',
  }));

  get flags() {
    return (this.query.data() ?? []) as any[];
  }

  get aiFlags() {
    return this.flags.filter(f =>
      ['ai_draft', 'ai_breakdown', 'ai_enhance_description', 'ai_generate_criteria'].includes(f.key)
    );
  }

  get orgs() {
    return (this.orgsQuery.data()?.items ?? []) as any[];
  }

  get orgTotal() {
    return this.orgsQuery.data()?.total ?? 0;
  }

  get orgTotalPages() {
    return Math.max(1, Math.ceil(this.orgTotal / this.orgPageSize()));
  }

  orgFlagsMap = signal<Map<string, any[]>>(new Map());

  toggleMutation = injectMutation(() => ({
    mutationFn: (key: string) => firstValueFrom(this.sa.toggleFlag(key)),
    onSuccess: () => {
      this.toast.success('Flag updated');

      try {
        this.flagService.clearOrgCache();
      } catch {}

      this.qc.invalidateQueries({ queryKey: ['superadmin-flags'] });
    },
    onError: (e: any) => this.toast.error(e.error?.error || 'Toggle failed')
  }));

  orgToggleMutation = injectMutation(() => ({
    mutationFn: ({ orgId, key }: { orgId: string; key: string }) =>
      firstValueFrom(this.sa.toggleOrgFlag(orgId, key)),

    onSuccess: (_: any, vars: any) => {
      this.toast.success('Org override updated');

      try {
        (this as any).flagService?.clearOrgCache?.(vars.orgId);
      } catch {}

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

  onToggle(flag: any) {
    (this.toggleMutation as any).mutate(flag.key);
  }

  onOrgToggle(org: any, flag: any) {
    (this.orgToggleMutation as any).mutate({
      orgId: org.id,
      key: flag.key
    });
  }

  getOrgFlag(orgId: string, key: string): boolean | null {
    const arr = this.orgFlagsMap().get(orgId);

    if (!arr) return null;

    const f = arr.find((x: any) => x.key === key);

    return f ? !!f.isEnabled : null;
  }

  onOrgSearch(v: string) {
    this.orgSearch.set(v);
    this.orgPage.set(1);
    this.orgFlagsMap.set(new Map());
  }

  setOrgPage(page: number) {
    const target = Math.min(Math.max(page, 1), this.orgTotalPages);

    if (target === this.orgPage()) return;

    this.orgPage.set(target);
    this.orgFlagsMap.set(new Map());
  }

  firstOrgPage() {
    this.setOrgPage(1);
  }

  previousOrgPage() {
    this.setOrgPage(this.orgPage() - 1);
  }

  nextOrgPage() {
    this.setOrgPage(this.orgPage() + 1);
  }

  lastOrgPage() {
    this.setOrgPage(this.orgTotalPages);
  }

  setOrgPageSize(value: number) {
    const size = Number(value) || 10;

    this.orgPageSize.set(size);
    this.orgPage.set(1);
    this.orgFlagsMap.set(new Map());
  }

  visibleOrgPages(): number[] {
    const total = this.orgTotalPages;
    const current = this.orgPage();

    if (total <= 3) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    if (current === 1) {
      return [1, 2, 3];
    }

    if (current === total) {
      return [total - 2, total - 1, total];
    }

    return [current - 1, current, current + 1];
  }

  orgShowingFrom() {
    return this.orgTotal === 0
      ? 0
      : (this.orgPage() - 1) * this.orgPageSize() + 1;
  }

  orgShowingTo() {
    return Math.min(
      this.orgPage() * this.orgPageSize(),
      this.orgTotal
    );
  }

  flagAccent(key: string) {
    switch (key) {
      case 'ai_draft':
        return 'bg-primary/10 text-primary border-primary/20';
      case 'ai_breakdown':
        return 'bg-secondary/10 text-secondary border-secondary/20';
      case 'ai_enhance_description':
        return 'bg-accent/10 text-accent border-accent/20';
      case 'ai_generate_criteria':
        return 'bg-info/10 text-info border-info/20';
      default:
        return 'bg-base-200 text-base-content border-base-300';
    }
  }

  flagIcon(key: string) {
    switch (key) {
      case 'ai_draft':
        return '✦';
      case 'ai_breakdown':
        return '⌘';
      case 'ai_enhance_description':
        return '✎';
      case 'ai_generate_criteria':
        return '✓';
      default:
        return '⚙';
    }
  }
}