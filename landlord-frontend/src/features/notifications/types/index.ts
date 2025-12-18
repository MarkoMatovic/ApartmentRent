// Notification types matching backend DTOs

export interface NotificationDto {
  id: number;
  title: string;
  message: string;
  actionType: string;
  actionTarget: string;
  isRead: boolean;
  createdDate: string; // ISO date string
  createdByGuid: string; // GUID
  senderUserId: number;
  recipientUserId: number;
}

export interface CreateNotificationInputDto {
  title: string;
  message: string;
  actionType: string;
  actionTarget: string;
  createdByGuid: string; // GUID
  senderUserId: number;
  recipientUserId: number;
}

