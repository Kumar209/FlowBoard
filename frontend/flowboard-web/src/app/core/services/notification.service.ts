import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface NotificationDto {
  id: string;
  eventId: string;
  recipientUserId: string;
  projectId: string;
  taskId: string;
  actorUserId: string;
  action: string;
  payloadJson: string;
  workspaceId: string;
  isRead: boolean;
  occurredOnUtc: string;
  createdAt: string;
}
export interface PaginatedNotifications {
  items: NotificationDto[];
  total: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  
  getNotifications(page = 1, pageSize = 20, unreadOnly?: boolean, search?: string) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (unreadOnly) params = params.set('unreadOnly', 'true');
    if (search) params = params.set('search', search);
    return this.http.get<PaginatedNotifications>(`${environment.apiUrl}/api/notifications`, {
      params,
      withCredentials: true,
    });
  }

  markRead(id: string) {
    return this.http.put(
      `${environment.apiUrl}/api/notifications/${id}/read`,
      {},
      { withCredentials: true },
    );
  }

  markAllRead() {
    return this.http.put(
      `${environment.apiUrl}/api/notifications/read-all`,
      {},
      { withCredentials: true },
    );
  }
}
