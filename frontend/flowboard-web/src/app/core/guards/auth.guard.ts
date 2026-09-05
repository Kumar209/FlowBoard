import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService, WorkspaceRole } from '../services/auth.service';

// Protects routes - requires isAuthenticated (Signals) with in-memory restore via HttpOnly refresh
export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  // Try silent refresh via HttpOnly cookie (in-memory fix: no sessionStorage)
  try {
    const { firstValueFrom } = await import('rxjs');
    const res: any = await firstValueFrom(auth.refresh());
    if (res?.accessToken) {
      auth.accessToken.set(res.accessToken);
      const me: any = await firstValueFrom(auth.me());
      auth.hydrateFromMe(me);
      return auth.isAuthenticated();
    }
  } catch {}
  router.navigate(['/login']);
  return false;
};

// Role guard factory - checks memberships signal (MNC-grade: computed role helpers)
export const roleGuard = (allowedRoles: (WorkspaceRole | number | string)[]): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    if (!auth.isAuthenticated()) { router.navigate(['/login']); return false; }
    // No memberships yet (fresh login) -> allow but let page fetch me() to hydrate; guard passes to avoid blocking
    if (auth.memberships().length === 0) return true;
    const roleMap: Record<string, number> = { Member: 0, ProjectManager: 1, OrgAdmin: 2, Client: 3, Viewer: 4, SuperAdmin: 5 };
    const allowedNums = allowedRoles.map(r => typeof r === 'string' ? (roleMap[r] ?? Number(r)) : Number(r));
    const has = auth.memberships().some(m => allowedNums.includes(Number(m.role)) || (m.roleName && allowedNums.includes(roleMap[m.roleName] ?? -1)));
    if (!has) { router.navigate(['/']); return false; }
    return true;
  };
};

// Convenience: System health only for OrgAdmin/SuperAdmin
export const orgAdminGuard: CanActivateFn = roleGuard([WorkspaceRole.OrgAdmin, WorkspaceRole.SuperAdmin]);
