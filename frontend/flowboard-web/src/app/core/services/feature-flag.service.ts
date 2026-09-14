import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class FeatureFlagService {
  private http = inject(HttpClient);
  private flags = signal<Map<string, boolean>>(new Map());
  private loaded = signal(false);

  async load(force = false): Promise<void> {
    if (this.loaded() && !force) return;
    try {
      const list: any = await firstValueFrom(this.http.get<any[]>(`${environment.apiUrl}/api/feature-flags`, { withCredentials: true }));
      const map = new Map<string, boolean>();
      for (const f of list) map.set(f.key, !!f.isEnabled);
      this.flags.set(map);
      this.loaded.set(true);
    } catch {}
  }

  private orgCache = new Map<string, { map: Map<string, boolean>; at: number }>();
  async loadForOrg(orgId: string, force = false): Promise<Map<string, boolean>> {
    const cached = this.orgCache.get(orgId);
    if (!force && cached && Date.now() - cached.at < 5_000) return cached.map;
    try {
      const list: any = await firstValueFrom(this.http.get<any[]>(`${environment.apiUrl}/api/feature-flags/organizations/${orgId}`, { withCredentials: true }));
      const map = new Map<string, boolean>();
      for (const f of list) map.set(f.key, !!f.isEnabled);
      this.orgCache.set(orgId, { map, at: Date.now() });
      return map;
    } catch {
      await this.load(true);
      return this.flags();
    }
  }
  clearOrgCache(orgId?: string) {
    if (orgId) this.orgCache.delete(orgId);
    else this.orgCache.clear();
  }

  isEnabled(key: string): boolean {
    const m = this.flags();
    if (!m.has(key)) return true;
    return !!m.get(key);
  }

  async isEnabledForOrg(key: string, orgId?: string): Promise<{ enabled: boolean; by: string }> {
    await this.load();
    const globalOn = this.isEnabled(key);
    if (!globalOn) return { enabled: false, by: 'SuperAdmin' };
    if (orgId) {
      try {
        const orgMap = await this.loadForOrg(orgId);
        if (orgMap.has(key) && !orgMap.get(key)) return { enabled: false, by: 'Organization Owner' };
      } catch {}
    }
    return { enabled: true, by: '' };
  }
}
