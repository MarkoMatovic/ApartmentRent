import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { notificationsApi, Notification } from '../../shared/api/notifications';
import { useAuth } from '../../shared/context/AuthContext';

export const NotificationsPanel: React.FC = () => {
    const { t } = useTranslation();
    const { user } = useAuth();
    const [notifications, setNotifications] = useState<Notification[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        if (user?.userId) {
            loadNotifications();
        }
    }, [user]);

    const loadNotifications = async () => {
        try {
            setLoading(true);
            const data = await notificationsApi.getUserNotifications(user!.userId);
            setNotifications(data);
        } catch (error) {
            console.error('Error loading notifications:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleMarkAsRead = async (notificationId: number) => {
        try {
            await notificationsApi.markAsRead(notificationId);
            setNotifications(notifications.map(n =>
                n.notificationId === notificationId ? { ...n, isRead: true } : n
            ));
        } catch (error) {
            console.error('Error marking notification as read:', error);
        }
    };

    const handleDelete = async (notificationId: number) => {
        try {
            await notificationsApi.deleteNotification(notificationId);
            setNotifications(notifications.filter(n => n.notificationId !== notificationId));
        } catch (error) {
            console.error('Error deleting notification:', error);
        }
    };

    const handleMarkAllAsRead = async () => {
        try {
            await notificationsApi.markAllAsRead(user!.userId);
            setNotifications(notifications.map(n => ({ ...n, isRead: true })));
        } catch (error) {
            console.error('Error marking all as read:', error);
        }
    };

    const getNotificationIcon = (type: string) => {
        switch (type) {
            case 'success': return '✅';
            case 'warning': return '⚠️';
            case 'error': return '❌';
            default: return 'ℹ️';
        }
    };

    if (loading) {
        return <div className="loading">Loading...</div>;
    }

    return (
        <div className="notifications-panel">
            <div className="panel-header">
                <h2>{t('notifications.title')}</h2>
                {notifications.length > 0 && (
                    <button onClick={handleMarkAllAsRead} className="btn-link">
                        {t('notifications.markAllRead')}
                    </button>
                )}
            </div>

            {notifications.length === 0 ? (
                <div className="empty-state">
                    <p>{t('notifications.noNotifications')}</p>
                    <p className="text-muted">{t('notifications.noNotificationsDescription')}</p>
                </div>
            ) : (
                <div className="notifications-list">
                    {notifications.map(notification => (
                        <div
                            key={notification.notificationId}
                            className={`notification-item ${notification.isRead ? 'read' : 'unread'}`}
                        >
                            <div className="notification-icon">
                                {getNotificationIcon(notification.type)}
                            </div>
                            <div className="notification-content">
                                <h4>{notification.title}</h4>
                                <p>{notification.message}</p>
                                <span className="notification-time">
                                    {new Date(notification.createdDate).toLocaleString()}
                                </span>
                            </div>
                            <div className="notification-actions">
                                {!notification.isRead && (
                                    <button
                                        onClick={() => handleMarkAsRead(notification.notificationId)}
                                        className="btn-icon"
                                        title={t('notifications.markAsRead')}
                                    >
                                        ✓
                                    </button>
                                )}
                                <button
                                    onClick={() => handleDelete(notification.notificationId)}
                                    className="btn-icon"
                                    title={t('notifications.delete')}
                                >
                                    🗑️
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};
