import { apiClient } from './client';

/** A notification as the server stores it (NotificationDto): the unread ones, newest first. */
export interface StoredNotification {
    id: number;
    /** Serbian text: shown only when the browser has no translation of `key`. */
    title: string;
    message: string;
    actionType: string;
    /** A route of the app when the notification leads somewhere; older ones hold an id or nothing useful. */
    actionTarget: string;
    /** Translation key, `notifications:events.<key>` (null for notifications stored before keys existed). */
    key?: string | null;
    /** Values for the translated text (name, title...). */
    params?: Record<string, string> | null;
    isRead: boolean;
    createdDate: string;
    senderUserId: number;
    recipientUserId: number;
}

export const notificationsApi = {
    getUserNotifications: async (userId: number): Promise<StoredNotification[]> => {
        const response = await apiClient.get<StoredNotification[]>(`/api/v1/notification/get-user-notifications`, {
            params: { id: userId },
        });
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
