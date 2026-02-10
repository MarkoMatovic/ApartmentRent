import { apiClient } from './client';

export interface Notification {
    notificationId: number;
    userId: number;
    title: string;
    message: string;
    type: 'info' | 'success' | 'warning' | 'error';
    isRead: boolean;
    createdDate: string;
    link?: string;
}

export interface CreateNotificationInput {
    userId: number;
    title: string;
    message: string;
    type: 'info' | 'success' | 'warning' | 'error';
    link?: string;
}

export const notificationsApi = {
    // Get all notifications for a user
    getUserNotifications: async (userId: number): Promise<Notification[]> => {
        const response = await apiClient.get(`/api/v1/notifications/user/${userId}`);
        return response.data;
    },

    // Send a notification
    sendNotification: async (input: CreateNotificationInput): Promise<Notification> => {
        const response = await apiClient.post('/api/v1/notifications/send', input);
        return response.data;
    },

    // Mark notification as read
    markAsRead: async (notificationId: number): Promise<void> => {
        await apiClient.post(`/api/v1/notifications/${notificationId}/mark-read`);
    },

    // Delete a notification
    deleteNotification: async (notificationId: number): Promise<boolean> => {
        const response = await apiClient.delete(`/api/v1/notifications/${notificationId}`);
        return response.data;
    },

    // Mark all notifications as read
    markAllAsRead: async (userId: number): Promise<boolean> => {
        const response = await apiClient.post('/api/v1/notifications/mark-all-read', userId);
        return response.data;
    },
};
