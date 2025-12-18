import { apiClient } from '@/shared/api/client';
import { NotificationDto, CreateNotificationInputDto } from '../types';

const API_V1 = 'api/v1';
const NOTIFICATION_BASE = `${API_V1}/notification`;

export const notificationsService = {
  /**
   * Get user notifications
   */
  getUserNotifications: async (userId: number): Promise<NotificationDto[]> => {
    const response = await apiClient.get<NotificationDto[]>(
      `${NOTIFICATION_BASE}/get-user-notifications`,
      {
        params: { id: userId },
      }
    );
    return response.data;
  },

  /**
   * Send notification
   */
  sendNotification: async (data: CreateNotificationInputDto): Promise<NotificationDto> => {
    const response = await apiClient.post<NotificationDto>(
      `${NOTIFICATION_BASE}/send-notification`,
      data
    );
    return response.data;
  },

  /**
   * Mark notification as read
   */
  markAsRead: async (notificationId: number): Promise<void> => {
    await apiClient.post(
      `${NOTIFICATION_BASE}/mark-as-read?notificationId=${notificationId}`
    );
  },
};

