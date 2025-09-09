export interface Notification {
  id: number;
  userId: number;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
  relatedEntityId?: number;
  relatedEntityType?: string;
}

export interface MarkNotificationReadRequest {
  notificationIds: number[];
}

export interface NotificationResponse {
  notifications: Notification[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface UnreadCountResponse {
  unreadCount: number;
}

