import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { QueryClient } from '@tanstack/angular-query-experimental';

@Injectable({ providedIn: 'root' })
export class BoardRealtimeService {
  private auth = inject(AuthService);
  private queryClient = inject(QueryClient);
  private hub: signalR.HubConnection | null = null;
  private lastProjectId: string | null = null;
  connected = signal(false);
  lastEvent = signal<string>('');

  async connect(): Promise<void> {
    if (this.hub?.state === signalR.HubConnectionState.Connected) return;
    const token = this.auth.currentUser() ? localStorage.getItem('accessToken') || sessionStorage.getItem('accessToken') || '' : '';
    // Fallback: token stored in auth service signal? AuthService holds accessToken signal
    const accessToken = (this.auth as any).accessToken?.() || token;
    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, {
        accessTokenFactory: () => accessToken,
        withCredentials: true,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.hub.on('taskMoved', (payload: any) => {
      this.lastEvent.set(`taskMoved:${payload.taskId}`);
      this.queryClient.invalidateQueries({ queryKey: ['board'] });
      this.queryClient.invalidateQueries({ queryKey: ['task-detail', payload.taskId] });
      this.queryClient.invalidateQueries({ queryKey: ['notifications'] });
    });
    this.hub.on('taskCreated', (payload: any) => {
      this.lastEvent.set(`taskCreated:${payload.taskId}`);
      this.queryClient.invalidateQueries({ queryKey: ['board'] });
      this.queryClient.invalidateQueries({ queryKey: ['notifications'] });
    });
    this.hub.on('taskCommented', (payload: any) => {
      this.lastEvent.set(`taskCommented:${payload.taskId}`);
      this.queryClient.invalidateQueries({ queryKey: ['comments'] });
      this.queryClient.invalidateQueries({ queryKey: ['board'] });
      this.queryClient.invalidateQueries({ queryKey: ['notifications'] });
    });
    this.hub.on('connected', () => this.connected.set(true));
    this.hub.onclose(() => this.connected.set(false));
    this.hub.onreconnected(async () => {
      this.connected.set(true);
      // Rejoin last project group after silent reconnect — workspace groups are rejoined by server OnConnectedAsync via JWT claims
      if (this.lastProjectId) {
        try { await this.hub?.invoke('JoinProject', this.lastProjectId); } catch {}
      }
    });

    try {
      await this.hub.start();
      this.connected.set(true);
    } catch (e) {
      this.connected.set(false);
    }
  }

  async joinProject(projectId: string): Promise<void> {
    this.lastProjectId = projectId;
    if (!this.hub || this.hub.state !== signalR.HubConnectionState.Connected) await this.connect();
    try { await this.hub?.invoke('JoinProject', projectId); } catch {}
  }

  async disconnect(): Promise<void> {
    try { await this.hub?.stop(); } catch {}
    this.connected.set(false);
  }
}
