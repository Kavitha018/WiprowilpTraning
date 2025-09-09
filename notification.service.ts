import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { 
  Notification, 
  MarkNotificationReadRequest, 
  NotificationResponse, 
  UnreadCountResponse 
} from '../models/notification.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly API_URL = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getNotifications(page: number = 1, pageSize: number = 20): Observable<NotificationResponse> {
    return this.http.get<NotificationResponse>(`${this.API_URL}/notifications?page=${page}&pageSize=${pageSize}`);
  }

  getUnreadCount(): Observable<UnreadCountResponse> {
    return this.http.get<UnreadCountResponse>(`${this.API_URL}/notifications/unread-count`);
  }

  markNotificationsAsRead(request: MarkNotificationReadRequest): Observable<any> {
    return this.http.put(`${this.API_URL}/notifications/mark-read`, request);
  }

  markAllNotificationsAsRead(): Observable<any> {
    return this.http.put(`${this.API_URL}/notifications/mark-all-read`, {});
  }

  deleteNotification(id: number): Observable<any> {
    return this.http.delete(`${this.API_URL}/notifications/${id}`);
  }
}
