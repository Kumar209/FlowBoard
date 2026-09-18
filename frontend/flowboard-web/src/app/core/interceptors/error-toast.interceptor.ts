import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

function toHuman(err: any): string {
  const raw = err?.error?.error || err?.error?.message || err?.message || 'Something went wrong — please try again.';
  let msg = String(raw).trim();
  // Http failure raw from Angular (e.g., "Http failure response for http://localhost:5000/api/tasks: 400 Bad Request") -> map to human
  if (msg.startsWith('Http failure response for')) {
    // Prefer backend human error if present, else generic
    const backend = err?.error?.error || err?.error?.message;
    if (backend && String(backend).trim().length > 0 && String(backend).trim().length <= 120) msg = String(backend).trim();
    else msg = 'Something went wrong — please try again.';
  }
  // Strip Raw:/LineNumber/stack per Human Error Rule
  if (msg.includes('Raw:') || msg.includes('LineNumber') || msg.includes('at System.') || msg.includes('BytePosition') || msg.length > 120) {
    msg = 'Something went wrong — please try again.';
  }
  // Truncate >120
  if (msg.length > 120) msg = msg.slice(0, 120) + '…';
  return msg;
}

export const errorToastInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  return next(req).pipe(
    catchError((err: any) => {
      // Skip 401 handled by authInterceptor refresh, 429/503 handled by their interceptors
      if (err.status === 401 || err.status === 429 || err.status === 503) return throwError(() => err);
      // Skip platform polling 30s and auth endpoints to avoid spam
      if (req.url.includes('/api/platform/maintenance') || req.url.includes('/api/platform/general')) return throwError(() => err);
      // For task/board/project/workspace mutations the component handles toast with its own human message — avoid double toast
      const isTaskMutation = req.method !== 'GET' && (req.url.includes('/api/tasks') || req.url.includes('/api/boards') || req.url.includes('/api/projects') || req.url.includes('/api/workspaces'));
      if (isTaskMutation) return throwError(() => err);
      // Only toast for mutating requests or failed queries (status >=400)
      if (err.status >= 400) {
        const human = toHuman(err);
        try { toast.error(human); } catch {}
      }
      return throwError(() => err);
    })
  );
};
