import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './auth.service';
import { PermissionKeys } from '../../shared/constants/permissions';

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private cache = new Map<string, { perms: Set<string>; at: number }>();
  private ttl = 5 * 60 * 1000; // 5m per Task 13.2 — single source, no window focus refetch, invalidate only after role save/promotion

  // Sync single-source helper — first check isSuperAdmin/isOrgAdmin (zero WorkspaceMembers bypass), then me permissions[], no extra HTTP
  hasPermissionSync(workspaceId: string, permKey: string): boolean {
    if (!workspaceId || !permKey) return false;
    if (this.auth.isSuperAdmin() || this.auth.isOrgAdmin() || this.auth.isOrgAdminFor(workspaceId)) return true;
    return this.auth.hasPermission(workspaceId, permKey);
  }

  // Back-compat async — prefers sync me cache, falls back to HTTP only if me empty
  async hasPermission(workspaceId: string, permKey: string): Promise<boolean> {
    if (!workspaceId || !permKey) return false;
    if (this.auth.isSuperAdmin() || this.auth.isOrgAdmin() || this.auth.isOrgAdminFor(workspaceId)) return true;
    const perms = await this.getMyPermissions(workspaceId);
    return perms.has(permKey);
  }

  hasOrgPermissionSync(orgId: string, permKey: string): boolean {
    if (this.auth.isSuperAdmin() || this.auth.isOrgAdmin()) return true;
    return false; // org perms are dynamic via getMyOrgPermissions async, but OrgAdmin bypass already handled
  }
  async hasOrgPermission(orgId: string, permKey: string): Promise<boolean> {
    if (!orgId || !permKey) return false;
    if (this.auth.isSuperAdmin() || this.auth.isOrgAdmin()) return true;
    const perms = await this.getMyOrgPermissions(orgId);
    return perms.has(permKey);
  }

  private orgCache = new Map<string, { perms: Set<string>; at: number }>();
  async getMyOrgPermissions(orgId: string): Promise<Set<string>> {
    const cached = this.orgCache.get(orgId);
    if (cached && Date.now() - cached.at < this.ttl) return cached.perms;
    try {
      const res: any = await firstValueFrom(this.http.get(`${environment.apiUrl}/api/organizations/${orgId}/my-permissions`, { withCredentials: true, headers: { 'X-Silent': 'true' } as any }));
      const arr: string[] = res.permissions || [];
      const set = new Set(arr);
      this.orgCache.set(orgId, { perms: set, at: Date.now() });
      return set;
    } catch { return new Set(); }
  }

  async getMyPermissions(workspaceId: string): Promise<Set<string>> {
    // Prefer B (me cache) — single source, no extra HTTP
    if (this.auth.isSuperAdmin() || this.auth.isOrgAdmin() || this.auth.isOrgAdminFor(workspaceId)) {
      // OrgAdmin/SuperAdmin has all perms — return full set from PermissionKeys.All to avoid HTTP
      const set = new Set<string>([...(this.auth.memberships().find(m => m.workspaceId === workspaceId)?.permissions ?? [])]);
      // If me has perms, use them; else return all keys implicitly
      if (set.size === 0) (Object.values(PermissionKeys) as string[]).forEach(k => set.add(k));
      this.cache.set(workspaceId, { perms: set, at: Date.now() });
      return set;
    }
    const fromMe = this.auth.memberships().find(m => m.workspaceId === workspaceId)?.permissions;
    if (fromMe && fromMe.length) {
      const set = new Set(fromMe);
      this.cache.set(workspaceId, { perms: set, at: Date.now() });
      return set;
    }
    const cached = this.cache.get(workspaceId);
    if (cached && Date.now() - cached.at < this.ttl) return cached.perms;
    try {
      const res: any = await firstValueFrom(this.http.get(`${environment.apiUrl}/api/workspaces/${workspaceId}/my-permissions`, { withCredentials: true, headers: { 'X-Silent': 'true' } as any }));
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

  // Invalidate single source after role save/promotion — call after POST /workspace-roles/:id/permissions or promotion
  invalidateAll() { this.clear(); this.auth.invalidateMeCache(); }

  // Helper for templates: check if has any of list
  async hasAny(workspaceId: string, keys: string[]): Promise<boolean> {
    const perms = await this.getMyPermissions(workspaceId);
    return keys.some(k => perms.has(k));
  }
}
