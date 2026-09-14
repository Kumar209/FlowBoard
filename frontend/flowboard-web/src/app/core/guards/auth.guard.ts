import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { WorkspaceRole, ROLE_VALUE_MAP, ROLE_LABEL_MAP, OrgRoleValues } from '../../shared/constants/roles';

// Protects routes - requires isAuthenticated (Signals) with in-memory restore via HttpOnly refresh (deduped)
export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  try {
    const { firstValueFrom } = await import('rxjs');
    const res: any = await firstValueFrom(auth.refreshDeduped());
    if (res?.accessToken) {
      auth.accessToken.set(res.accessToken);
      try {
        const me: any = await firstValueFrom(auth.meDeduped());
        auth.hydrateFromMe(me);
      } catch {
        // me failed but token is valid — still consider authenticated, try to hydrate minimally
        // Don't block navigation; let layout hydrate later
      }
      if (auth.accessToken()) {
        if ((!auth.currentUser() || auth.memberships().length === 0) && res?.accessToken) {
          try {
            let b64 = res.accessToken.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
            while (b64.length % 4) b64 += '=';
            const payload = JSON.parse(atob(b64));
            const sub =
              payload.sub ||
              payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
              '';
            const email =
              payload.email ||
              payload.Email ||
              payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
              '';
            const name =
              payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
              payload.name ||
              payload.unique_name ||
              (email ? email.split('@')[0] : 'User');
            if (sub && !auth.currentUser())
              auth.currentUser.set({ id: sub, email, fullName: name });
            // Hydrate memberships from token claims if me() failed
            if (auth.memberships().length === 0) {
              const wids: any = payload.workspace_id || payload['workspace_id'] || [];
              const roles: any =
                payload.role ||
                payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
                payload[
                  Object.keys(payload).find((k) => k.toLowerCase().includes('role')) as string
                ] ||
                [];
              const isSuper = payload.is_super_admin === 'true' || payload.is_super_admin === true;
              const widArr = Array.isArray(wids) ? wids : wids ? [wids] : [];
              const roleArr = Array.isArray(roles) ? roles : roles ? [roles] : [];
              if (widArr.length || roleArr.length || isSuper) {
                const memberships: any[] = [];
                const max = Math.max(widArr.length, roleArr.length);
                for (let i = 0; i < max; i++) {
                  const wid = widArr[i] || widArr[0] || '00000000-0000-0000-0000-000000000000';
                  const r = roleArr[i] || roleArr[0] || (isSuper ? ROLE_LABEL_MAP[String(OrgRoleValues.SuperAdmin)] : ROLE_LABEL_MAP[String(OrgRoleValues.Member)]);
                  memberships.push({
                    workspaceId: wid,
                    role: r,
                    roleName: typeof r === 'string' ? r : undefined,
                  });
                }
                if (
                  isSuper &&
                  !memberships.some((m) => m.role === OrgRoleValues.SuperAdmin || m.roleName === ROLE_LABEL_MAP[String(OrgRoleValues.SuperAdmin)])
                ) {
                  memberships.push({
                    workspaceId: '00000000-0000-0000-0000-000000000000',
                    role: OrgRoleValues.SuperAdmin,
                    roleName: ROLE_LABEL_MAP[String(OrgRoleValues.SuperAdmin)],
                  });
                }
                if (memberships.length) auth.memberships.set(memberships as any);
              }
            }
          } catch {}
        }
        return !!auth.accessToken();
      }
    }
  } catch {}
  router.navigate(['/login']);
  return false;
};

// Role guard factory - checks memberships signal (computed role helpers)
export const roleGuard = (allowedRoles: (WorkspaceRole | number | string)[]): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    if (!auth.isAuthenticated()) {
      router.navigate(['/login']);
      return false;
    }
    // No memberships yet (fresh login) -> allow but let page fetch me() to hydrate; guard passes to avoid blocking
    if (auth.memberships().length === 0) return true;
    const allowedNums = allowedRoles.map((r) =>
      typeof r === 'string' ? (ROLE_VALUE_MAP[r as string] ?? Number(r)) : Number(r),
    );
    const has = auth
      .memberships()
      .some(
        (m) =>
          allowedNums.includes(Number(m.role)) ||
          (m.roleName && allowedNums.includes(ROLE_VALUE_MAP[m.roleName] ?? -1)),
      );
    if (!has) {
      router.navigate(['/']);
      return false;
    }
    return true;
  };
};

// Convenience: System health only for OrgAdmin/SuperAdmin
export const orgAdminGuard: CanActivateFn = roleGuard([
  WorkspaceRole.OrgAdmin,
  WorkspaceRole.SuperAdmin,
]);
