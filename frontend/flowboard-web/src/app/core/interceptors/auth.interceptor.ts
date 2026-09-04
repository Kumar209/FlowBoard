import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, switchMap, throwError } from 'rxjs';

// Attach Bearer token from Signals, retry once on 401 via refresh (HttpOnly cookie)
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.accessToken();

  let cloned = req;
  if (token && !req.headers.has('Authorization')) {
    cloned = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  // Always send cookies (refreshToken HttpOnly)
  cloned = cloned.clone({ withCredentials: true });

  return next(cloned).pipe(
    catchError(err => {
      if (err.status === 401 && !req.url.includes('/api/auth/refresh') && !req.url.includes('/api/auth/login')) {
        // Try refresh once
        return auth.refresh().pipe(
          switchMap(res => {
            auth.accessToken.set(res.accessToken);
            sessionStorage.setItem('accessToken', res.accessToken);
            const retry = req.clone({ setHeaders: { Authorization: `Bearer ${res.accessToken}` }, withCredentials: true });
            return next(retry);
          }),
          catchError(refreshErr => {
            auth.clearSession();
            return throwError(() => refreshErr);
          })
        );
      }
      return throwError(() => err);
    })
  );
};
