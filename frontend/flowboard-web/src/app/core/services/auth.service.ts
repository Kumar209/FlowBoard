import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ROLE_VALUE_MAP as SharedRoleMap } from '../../shared/constants/roles';
import { Observable, shareReplay, finalize } from 'rxjs';

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

// Re-export compact WorkspaceRole for compat — SuperAdmin 0, Member 1, OrgAdmin 2, Client 3 (legacy ProjectManager/Viewer → Member 1)
export enum WorkspaceRole {
  SuperAdmin = 0,
  Member = 1,
  OrgAdmin = 2,
  Client = 3,
  ProjectManager = 1,
  Viewer = 1
}

export interface Membership {
  workspaceId: string;
  role: WorkspaceRole | number;
  roleName?: string;
}

export interface MeResponse {
  user: User;
  workspaces: { workspaceId: string; role: string | number }[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  // Signals - client state (no NgRx, modern)
  currentUser = signal<User | null>(null);
  accessToken = signal<string | null>(null);
  memberships = signal<Membership[]>([]);

  isAuthenticated = computed(() => this.currentUser() !== null && this.accessToken() !== null);

  // Global role helpers - computed memoized (OnPush reads)
  hasAnyRole = computed(() => this.memberships().length > 0);
  isSuperAdmin = computed(() => this.memberships().some(m => Number(m.role) === WorkspaceRole.SuperAdmin || m.roleName === 'SuperAdmin'));
  isOrgAdmin = computed(() => this.memberships().some(m => Number(m.role) === WorkspaceRole.OrgAdmin || m.roleName === 'OrgAdmin') || this.isSuperAdmin());
  isProjectManager = computed(() => this.memberships().some(m => Number(m.role) === WorkspaceRole.ProjectManager || m.roleName === 'ProjectManager'));
  isMember = computed(() => this.memberships().some(m => Number(m.role) === WorkspaceRole.Member));
  isClient = computed(() => this.memberships().some(m => Number(m.role) === WorkspaceRole.Client || m.roleName === 'Client'));
  isViewer = computed(() => this.memberships().some(m => Number(m.role) === WorkspaceRole.Viewer || m.roleName === 'Viewer'));

  // Workspace-scoped checks
  isManagerFor = (workspaceId: string) => this.memberships().some(m => m.workspaceId === workspaceId && Number(m.role) === WorkspaceRole.ProjectManager);
  isOrgAdminFor = (workspaceId: string) => this.memberships().some(m => m.workspaceId === workspaceId && (Number(m.role) === WorkspaceRole.OrgAdmin || Number(m.role) === WorkspaceRole.SuperAdmin));
  canCreateProject = computed(() => this.isOrgAdmin() || this.isProjectManager() || this.isSuperAdmin());
  canCreateWorkspace = computed(() => this.isOrgAdmin() || this.isSuperAdmin());
  canCreateTask = computed(() => !this.isClient() && !this.isViewer()); // Client 403, Viewer 403
  canComment = computed(() => !this.isViewer()); // Viewer no comment, Client can comment

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

  me() {
    return this.http.get<MeResponse>(`${environment.apiUrl}/api/auth/me`, { withCredentials: true });
  }

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
        return { workspaceId: (w as any).workspaceId ?? (w as any).workspaceID ?? (w as any).id, role: isNaN(roleNum) ? raw : roleNum, roleName: roleName ?? (typeof raw === 'string' ? raw : undefined) };
      });
      this.memberships.set(mapped);
    }
  }

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
  }

  // In-memory restore: try HttpOnly refresh -> me
  restoreFromRefresh() {
    return this.refresh();
  }
}
