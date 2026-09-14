import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private cache = new Map<string, { perms: Set<string>; at: number }>();
  private ttl = 60_000; // 1m cache

  async hasPermission(workspaceId: string, permKey: string): Promise<boolean> {
    if (!workspaceId || !permKey) return false;
    const perms = await this.getMyPermissions(workspaceId);
    return perms.has(permKey);
  }

  async hasOrgPermission(orgId: string, permKey: string): Promise<boolean> {
    if (!orgId || !permKey) return false;
    const perms = await this.getMyOrgPermissions(orgId);
    return perms.has(permKey);
  }

  private orgCache = new Map<string, { perms: Set<string>; at: number }>();
  async getMyOrgPermissions(orgId: string): Promise<Set<string>> {
    const cached = this.orgCache.get(orgId);
    if (cached && Date.now() - cached.at < this.ttl) return cached.perms;
    try {
      const res: any = await firstValueFrom(this.http.get(`${environment.apiUrl}/api/organizations/${orgId}/my-permissions`, { withCredentials: true }));
      const arr: string[] = res.permissions || [];
      const set = new Set(arr);
      this.orgCache.set(orgId, { perms: set, at: Date.now() });
      return set;
    } catch { return new Set(); }
  }

  async getMyPermissions(workspaceId: string): Promise<Set<string>> {
    // Prefer B (me cache) — single source, no extra HTTP
    const fromMe = this.auth.memberships().find(m => m.workspaceId === workspaceId)?.permissions;
    if (fromMe && fromMe.length) {
      const set = new Set(fromMe);
      this.cache.set(workspaceId, { perms: set, at: Date.now() });
      return set;
    }
    const cached = this.cache.get(workspaceId);
    if (cached && Date.now() - cached.at < this.ttl) return cached.perms;
    try {
      const res: any = await firstValueFrom(this.http.get(`${environment.apiUrl}/api/workspaces/${workspaceId}/my-permissions`, { withCredentials: true }));
      const arr: string[] = res.permissions || res.Permissions || [];
      const set = new Set(arr);
      this.cache.set(workspaceId, { perms: set, at: Date.now() });
      return set;
    } catch {
      return new Set();
    }
  }

  // Synchronous check for already cached, fallback to auth fixed role checks via caller
  hasCached(workspaceId: string, permKey: string): boolean | null {
    const c = this.cache.get(workspaceId);
    if (!c) return null;
    return c.perms.has(permKey);
  }

  clear(workspaceId?: string) {
    if (workspaceId) { this.cache.delete(workspaceId); this.orgCache.delete(workspaceId); }
    else { this.cache.clear(); this.orgCache.clear(); }
  }

  // Helper for templates: check if has any of list
  async hasAny(workspaceId: string, keys: string[]): Promise<boolean> {
    const perms = await this.getMyPermissions(workspaceId);
    return keys.some(k => perms.has(k));
  }
}
