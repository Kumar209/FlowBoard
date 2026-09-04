import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

// Protects routes - requires isAuthenticated (Signals)
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  // Try me() could be async, but for guard we check signal - if null, redirect to login
  router.navigate(['/login']);
  return false;
};

// Role guard factory - checks currentUser role via workspace membership (future: fetch from me() workspaces)
export const roleGuard = (roles: string[]): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    // For now, allow if authenticated - role check will be via API in Task 1.4+ (WorkspaceMember.Role)
    if (auth.isAuthenticated()) return true;
    router.navigate(['/login']);
    return false;
  };
};
