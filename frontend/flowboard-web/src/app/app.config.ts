import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { correlationInterceptor } from './core/interceptors/correlation.interceptor';
import { rateLimitInterceptor } from './core/interceptors/rate-limit.interceptor';
import { maintenanceInterceptor } from './core/interceptors/maintenance.interceptor';
import { loadingInterceptor } from './core/interceptors/loading.interceptor';
import { errorToastInterceptor } from './core/interceptors/error-toast.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([correlationInterceptor, maintenanceInterceptor, rateLimitInterceptor, loadingInterceptor, errorToastInterceptor, authInterceptor])),
    provideTanStackQuery(new QueryClient({
      defaultOptions: {
        queries: { retry: 1, staleTime: 1000 * 60 * 2 } // 2m cache for board — matches Redis 5m board cache
      }
    }))
  ]
};
