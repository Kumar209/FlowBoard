import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { ROLE_LABEL_MAP, OrgRoleValues } from '../../shared/constants/roles';

export const superAdminGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) {
    // Try silent refresh like authGuard (deduped)
    try {
      const { firstValueFrom } = await import('rxjs');
      const res: any = await firstValueFrom(auth.refreshDeduped());
      if (res?.accessToken) {
        auth.accessToken.set(res.accessToken);
        try {
          const me: any = await firstValueFrom(auth.meDeduped());
          auth.hydrateFromMe(me);
        } catch {}
        // Fallback hydrate from token if me failed
        if (auth.memberships().length === 0 && res.accessToken) {
          try {
            let b64 = res.accessToken.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
            while (b64.length % 4) b64 += '=';
            const payload = JSON.parse(atob(b64));
            const isSuper = payload.is_super_admin === 'true' || payload.is_super_admin === true;
            const roles: any =
              payload.role ||
              payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
              [];
            const roleArr = Array.isArray(roles) ? roles : roles ? [roles] : [];
            if (isSuper || roleArr.includes(ROLE_LABEL_MAP[String(OrgRoleValues.SuperAdmin)])) {
              auth.memberships.set([
                {
                  workspaceId: '00000000-0000-0000-0000-000000000000',
                  role: OrgRoleValues.SuperAdmin,
                  roleName: ROLE_LABEL_MAP[String(OrgRoleValues.SuperAdmin)],
                } as any,
              ]);
            }
          } catch {}
        }
      }
    } catch {}
    if (!auth.isAuthenticated()) {
      router.navigate(['/login']);
      return false;
    }
  }
  // If memberships not yet hydrated, try me() once (guard is async, wait for it)
  if (auth.memberships().length === 0) {
    try {
      const { firstValueFrom } = await import('rxjs');
      const me: any = await firstValueFrom(auth.meDeduped());
      auth.hydrateFromMe(me);
    } catch {}
    if (auth.memberships().length === 0) return true; // still allow, component will show loading then redirect if not superadmin
  }
  if (auth.isSuperAdmin()) return true;
  router.navigate(['/']);
  return false;
};
