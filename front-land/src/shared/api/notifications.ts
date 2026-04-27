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
    getUserNotifications: async (userId: number): Promise<Notification[]> => {
        const response = await apiClient.get(`/api/v1/notification/get-user-notifications`, {
            params: { id: userId },
        });
        return response.data;
    },

    sendNotification: async (input: CreateNotificationInput): Promise<Notification> => {
        const response = await apiClient.post('/api/v1/notification/send-notification', input);
        return response.data;
    },

    markAsRead: async (notificationId: number): Promise<void> => {
        await apiClient.post(`/api/v1/notification/mark-as-read`, null, {
            params: { notificationId },
        });
    },

    deleteNotification: async (notificationId: number): Promise<boolean> => {
        const response = await apiClient.delete(`/api/v1/notification/delete/${notificationId}`);
        return response.data;
    },

    markAllAsRead: async (): Promise<boolean> => {
        const response = await apiClient.post('/api/v1/notification/mark-all-as-read');
        return response.data;
    },
};
