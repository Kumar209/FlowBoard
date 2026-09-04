import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface User {
  id: string;
  email: string;
  fullName: string;
  avatarUrl?: string;
}

export interface AuthResponse {
  user: { id: string; email: string; fullName: string };
  accessToken: string;
  accessTokenExpiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  // Signals - client state (no NgRx, enterprise modern)
  currentUser = signal<User | null>(null);
  accessToken = signal<string | null>(null);

  isAuthenticated = computed(() => this.currentUser() !== null && this.accessToken() !== null);

  constructor(private http: HttpClient) {
    // Try restore from sessionStorage (optional, refresh via cookie will re-auth)
    const saved = sessionStorage.getItem('accessToken');
    if (saved) this.accessToken.set(saved);
  }

  register(email: string, password: string, fullName: string) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/api/auth/register`, { email, password, fullName }, { withCredentials: true });
  }

  login(email: string, password: string) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, { email, password }, { withCredentials: true });
  }

  refresh() {
    return this.http.post<{ accessToken: string; accessTokenExpiresAt: string }>(`${environment.apiUrl}/api/auth/refresh`, {}, { withCredentials: true });
  }

  me() {
    return this.http.get<{ user: User; workspaces: any[] }>(`${environment.apiUrl}/api/auth/me`, { withCredentials: true });
  }

  logout() {
    return this.http.post(`${environment.apiUrl}/api/auth/logout`, {}, { withCredentials: true });
  }

  setSession(user: User, token: string) {
    this.currentUser.set(user);
    this.accessToken.set(token);
    sessionStorage.setItem('accessToken', token);
  }

  clearSession() {
    this.currentUser.set(null);
    this.accessToken.set(null);
    sessionStorage.removeItem('accessToken');
  }
}
