import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withPreloading } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { routes } from './app.routes';
import { SelectivePreloadStrategy } from './core/strategies/selective-preload.strategy';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { correlationInterceptor } from './core/interceptors/correlation.interceptor';
import { rateLimitInterceptor } from './core/interceptors/rate-limit.interceptor';
import { maintenanceInterceptor } from './core/interceptors/maintenance.interceptor';
import { loadingInterceptor } from './core/interceptors/loading.interceptor';
import { errorToastInterceptor } from './core/interceptors/error-toast.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withPreloading(SelectivePreloadStrategy)),
    provideHttpClient(withInterceptors([correlationInterceptor, maintenanceInterceptor, rateLimitInterceptor, loadingInterceptor, errorToastInterceptor, authInterceptor])),
    provideTanStackQuery(new QueryClient({
      defaultOptions: {
        queries: { retry: 1, staleTime: 1000 * 60 * 2, refetchOnWindowFocus: false, refetchOnReconnect: false, gcTime: 1000 * 60 * 10 } // 2m board, no window focus refetch per 13.2; me/flags use 5m dedup in auth.service
      }
    }))
  ]
};
