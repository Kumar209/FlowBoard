import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, switchMap, throwError } from 'rxjs';

// Attach Bearer token from Signals, retry once on 401 via deduped refresh (singleflight)
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.accessToken();

  // Don't send stale Bearer for auth endpoints (login/register/refresh) — allow switch to different account
  const isAuthEndpoint = req.url.includes('/api/auth/login') || req.url.includes('/api/auth/register') || req.url.includes('/api/auth/refresh');
  let cloned = req;
  if (token && !req.headers.has('Authorization') && !isAuthEndpoint) {
    cloned = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  cloned = cloned.clone({ withCredentials: true });

  return next(cloned).pipe(
    catchError((err: any) => {
      if (err.status === 401 && !req.url.includes('/api/auth/refresh') && !req.url.includes('/api/auth/login') && !req.url.includes('/api/auth/register')) {
        return auth.refreshDeduped().pipe(
          switchMap((res: any) => {
            const newToken = (res as any)?.accessToken;
            if (!newToken) throw err;
            auth.accessToken.set(newToken);
            if (!auth.currentUser()) {
              auth.me().subscribe({ next: (m: any) => auth.hydrateFromMe(m as any), error: () => {} });
            }
            const retry = req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` }, withCredentials: true });
            return next(retry);
          }),
          catchError((refreshErr: any) => {
            auth.clearSession();
            auth.clearRefreshDedup();
            return throwError(() => refreshErr);
          })
        );
      }
      return throwError(() => err);
    })
  );
};
