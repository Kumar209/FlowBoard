import { Injectable } from '@angular/core';
import { PreloadingStrategy, Route } from '@angular/router';
import { Observable, of } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SelectivePreloadStrategy implements PreloadingStrategy {
  preload(route: Route, load: () => Observable<any>): Observable<any> {
    // Preload if data.preload !== false and not superadmin (guard will block non-superadmin anyway, but we avoid preloading superadmin for Member/Client)
    const shouldPreload = route.data?.['preload'] !== false && route.path !== 'superadmin';
    return shouldPreload ? load() : of(null);
  }
}
