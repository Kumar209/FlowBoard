import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ROLE_VALUE_MAP as SharedRoleMap, ROLE_LABEL_MAP, OrgRoleValues } from '../../shared/constants/roles';
import { PermissionKeys } from '../../shared/constants/permissions';
import { Observable, shareReplay, finalize, of, tap } from 'rxjs';

export interface User {
  id: string;
  email: string;
  fullName: string;
  avatarUrl?: string;
}

export interface AuthResponse {
  user: { id: string; email: string; fullName: string };
  accessToken: string;
  accessTokenExpiresAt: string;
}

// Fixed roles only — SuperAdmin 0, Member 1, OrgAdmin 2, Client 3. Workspace custom roles are dynamic via OrganizationWorkspaceRoles, not hardcoded here.
export enum WorkspaceRole {
  SuperAdmin = 0,
  Member = 1,
  OrgAdmin = 2,
  Client = 3,
}

export interface Membership {
  workspaceId: string;
  role: WorkspaceRole | number;
  roleName?: string;
  customRoleId?: string | null;
  customRoleName?: string | null;
  permissions?: string[];
}

export interface MeResponse {
  user: User;
  workspaces: { workspaceId: string; role: string | number; customRoleId?: string | null; customRoleName?: string | null; permissions?: string[] }[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  // Signals - client state (no NgRx, modern)
  currentUser = signal<User | null>(null);
  accessToken = signal<string | null>(null);
  memberships = signal<Membership[]>([]);

  isAuthenticated = computed(() => this.currentUser() !== null && this.accessToken() !== null);

  // Global role helpers — fixed roles only (Member/OrgAdmin/Client/SuperAdmin). Custom workspace roles are dynamic and checked via permissions on backend.
  hasAnyRole = computed(() => this.memberships().length > 0);
  isSuperAdmin = computed(() => this.memberships().some(m => Number(m.role) === OrgRoleValues.SuperAdmin || m.roleName === ROLE_LABEL_MAP[String(OrgRoleValues.SuperAdmin)]));
  isOrgAdmin = computed(() => this.memberships().some(m => Number(m.role) === OrgRoleValues.OrgAdmin || m.roleName === ROLE_LABEL_MAP[String(OrgRoleValues.OrgAdmin)]) || this.isSuperAdmin());
  isMember = computed(() => this.memberships().some(m => Number(m.role) === OrgRoleValues.Member));
  isClient = computed(() => this.memberships().some(m => Number(m.role) === OrgRoleValues.Client || m.roleName === ROLE_LABEL_MAP[String(OrgRoleValues.Client)]));
  // Custom workspace roles: not hardcoded — permission checks are backend-enforced via RolePermissions (e.g., project:create, comment:create). Frontend treats custom roles as Member-like for UX.
  isViewer = computed(() => false);
  isViewerFor = (_workspaceId: string) => false;

  // Workspace-scoped checks — only fixed roles; custom workspace roles are dynamic via OrganizationWorkspaceRoles
  isOrgAdminFor = (workspaceId: string) => this.memberships().some(m => m.workspaceId === workspaceId && (Number(m.role) === OrgRoleValues.OrgAdmin || Number(m.role) === OrgRoleValues.SuperAdmin));
  // legacy alias kept for compat — custom manager roles are not hardcoded, treat as OrgAdmin check
  isManagerFor = (workspaceId: string) => this.isOrgAdminFor(workspaceId);
  canCreateProject = computed(() => this.isOrgAdmin() || this.isSuperAdmin());
  canCreateWorkspace = computed(() => this.isOrgAdmin() || this.isSuperAdmin());
  canCreateTask = computed(() => !this.isClient()); // Client 403 via Roles.CanUpload
  canComment = computed(() => true); // Client and all fixed roles can comment; custom Viewer-like restrictions are backend 403 via permissions
  canCommentFor = (_workspaceId: string) => true;

  constructor(private http: HttpClient) {
    // In-memory only — no sessionStorage (Image 1 fix: nothing visible in Application > Session Storage)
    // Rehydrate via HttpOnly refresh cookie on app init (Layout ngOnInit -> me()/refresh)
  }

  register(email: string, password: string, fullName: string, companyName: string, companyDescription?: string) {
    this.clearRefreshDedup();
    return this.http.post<AuthResponse>(`${environment.apiUrl}/api/auth/register`, { email, password, fullName, companyName, companyDescription }, { withCredentials: true });
  }

  login(email: string, password: string) {
    this.clearRefreshDedup();
    // Clear stale session when switching accounts (avoid sending old Bearer on login)
    this.clearSession();
    return this.http.post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, { email, password }, { withCredentials: true });
  }

  // Deduped refresh — singleflight to avoid concurrent /refresh race that triggers reuse-revoke
  private refreshInFlight: Observable<{ accessToken: string; accessTokenExpiresAt: string }> | null = null;
  refresh() {
    return this.http.post<{ accessToken: string; accessTokenExpiresAt: string }>(`${environment.apiUrl}/api/auth/refresh`, {}, { withCredentials: true });
  }
  refreshDeduped(): Observable<{ accessToken: string; accessTokenExpiresAt: string }> {
    if (this.refreshInFlight) return this.refreshInFlight;
    this.refreshInFlight = this.http.post<{ accessToken: string; accessTokenExpiresAt: string }>(`${environment.apiUrl}/api/auth/refresh`, {}, { withCredentials: true }).pipe(
      shareReplay({ bufferSize: 1, refCount: true }),
      finalize(() => setTimeout(() => (this.refreshInFlight = null), 5000))
    );
    return this.refreshInFlight;
  }
  clearRefreshDedup() { this.refreshInFlight = null; }

  private meInFlight: Observable<MeResponse> | null = null;
  // 5m stale single shared me — singleflight + TTL cache, no window focus refetch, silent (loadingInterceptor silents /api/auth/me)
  private meCache: { data: MeResponse | null; at: number } = { data: null, at: 0 };
  private meCacheTtl = 5 * 60 * 1000; // 5m per Task 13.2 — matches backend isOrgAdmin bypass, invalidate only after role save/promotion
  me() {
    return this.http.get<MeResponse>(`${environment.apiUrl}/api/auth/me`, { withCredentials: true, headers: { 'X-Silent': 'true' } as any });
  }
  meDeduped(): Observable<MeResponse> {
    const now = Date.now();
    if (this.meCache.data && now - this.meCache.at < this.meCacheTtl) {
      return of(this.meCache.data);
    }
    if (this.meInFlight) return this.meInFlight;
    this.meInFlight = this.http.get<MeResponse>(`${environment.apiUrl}/api/auth/me`, { withCredentials: true, headers: { 'X-Silent': 'true' } as any }).pipe(
      tap(res => { this.meCache = { data: res, at: Date.now() }; }),
      shareReplay({ bufferSize: 1, refCount: true }),
      finalize(() => setTimeout(() => (this.meInFlight = null), 2000))
    );
    return this.meInFlight;
  }
  invalidateMeCache() { this.meCache = { data: null, at: 0 }; this.meInFlight = null; }

  hydrateFromMe(res: MeResponse) {
    if (res.user) this.currentUser.set(res.user);
    if (res.workspaces) {
      const mapped: Membership[] = res.workspaces.map(w => {
        const raw = (w as any).role;
        let roleNum: number = Number(raw);
        let roleName: string | undefined = typeof raw === 'string' ? raw : undefined;
        if (isNaN(roleNum) && roleName) {
          roleNum = (SharedRoleMap as any)[roleName] ?? 1;
        }
        return {
          workspaceId: (w as any).workspaceId ?? (w as any).workspaceID ?? (w as any).id,
          role: isNaN(roleNum) ? raw : roleNum,
          roleName: roleName ?? (typeof raw === 'string' ? raw : undefined),
          customRoleId: (w as any).customRoleId ?? null,
          customRoleName: (w as any).customRoleName ?? null,
          permissions: (w as any).permissions ?? []
        };
      });
      this.memberships.set(mapped);
    }
  }

  hasPermission(workspaceId: string, permKey: string): boolean {
    // Single source: isSuperAdmin/isOrgAdmin first (OrgAdmin with zero WorkspaceMembers has no membership row for that workspace but backend bypass allows — frontend mirrors)
    if (this.isSuperAdmin() || this.isOrgAdmin()) return true;
    if (workspaceId && this.isOrgAdminFor(workspaceId)) return true;
    const m = this.memberships().find(x => x.workspaceId === workspaceId);
    if (!m) return false;
    if (m.permissions && m.permissions.length) return m.permissions.includes(permKey);
    // Fallback to fixed role implicit: OrgAdmin/SuperAdmin have all (already handled globally above, kept for per-workspace role)
    if (Number(m.role) === OrgRoleValues.SuperAdmin || Number(m.role) === OrgRoleValues.OrgAdmin || m.roleName === ROLE_LABEL_MAP[String(OrgRoleValues.OrgAdmin)]) return true;
    return false;
  }

  // Single helper for all 35 keys — use PermissionKeys constants (no hardcoding)
  can(workspaceId: string, permKey: string): boolean { return this.hasPermission(workspaceId, permKey); }

  logout() {
    return this.http.post(`${environment.apiUrl}/api/auth/logout`, {}, { withCredentials: true });
  }

  setSession(user: User, token: string, memberships?: Membership[]) {
    this.currentUser.set(user);
    this.accessToken.set(token);
    if (memberships) this.memberships.set(memberships);
  }

  clearSession() {
    this.currentUser.set(null);
    this.accessToken.set(null);
    this.memberships.set([]);
    this.invalidateMeCache();
  }

  // In-memory restore: try HttpOnly refresh -> me
  restoreFromRefresh() {
    return this.refresh();
  }
}
