import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

// Handles 503 Platform under maintenance — shows banner or navigates to /maintenance
// Identity MaintenanceMiddleware returns {error: "Platform under maintenance — please try again later.", retryAfter}
let lastToastAt = 0;
export const maintenanceInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const router = inject(Router);
  return next(req).pipe(
    catchError((err: any) => {
      if (err.status === 503) {
        const retryAfter = err.headers?.get?.('Retry-After') || err.error?.maintenance?.retryAfter || err.error?.retryAfter || 60;
        const msg = err.error?.error || `Platform under maintenance — please try again after ${retryAfter}s`;
        const now = Date.now();
        if (now - lastToastAt > 5000) {
          lastToastAt = now;
          try { toast.error(msg); } catch {}
        }
        // Navigate to maintenance page if not already there and not polling platform/general
        if (!req.url.includes('/api/platform/maintenance') && !req.url.includes('/api/platform/general') && router.url !== '/maintenance') {
          // Keep user on page but show platform-notice; optionally navigate
          // Uncomment to hard redirect: router.navigate(['/maintenance']);
        }
      }
      return throwError(() => err);
    })
  );
};
