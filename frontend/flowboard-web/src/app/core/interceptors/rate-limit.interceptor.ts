import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

// Handles 429 Too Many Requests — shows Retry-After and disables mutate via toast
// Gateway RateLimitMiddleware returns {error, retryAfter} + header Retry-After
export const rateLimitInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  return next(req).pipe(
    catchError((err: any) => {
      if (err.status === 429) {
        const retryAfter = err.headers?.get?.('Retry-After') || err.error?.retryAfter || err.error?.retry_after || 60;
        const msg = `Too many requests — please try again after ${retryAfter}s`;
        try { toast.error(msg); } catch {}
        // Also expose retryAfter on error for callers to disable mutate buttons
        err.retryAfter = Number(retryAfter);
      }
      return throwError(() => err);
    })
  );
};
