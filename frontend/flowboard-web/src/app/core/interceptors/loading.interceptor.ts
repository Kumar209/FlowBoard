import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { LoadingService } from '../services/loading.service';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loading = inject(LoadingService);
  // Silent polling — don't disturb UI with loader overlay
  const silentUrls = ['/api/platform/maintenance', '/api/platform/general', '/api/feature-flags', '/api/auth/me', '/api/auth/refresh'];
  const isSilent = silentUrls.some(u => req.url.includes(u)) || req.headers.has('X-Silent');
  if (!isSilent) loading.show();
  return next(req).pipe(finalize(() => { if (!isSilent) loading.hide(); }));
};
