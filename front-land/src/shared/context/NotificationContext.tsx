import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import * as signalR from '@microsoft/signalr';

interface Notification {
  id: string;
  title: string;
  message: string;
  type: 'info' | 'success' | 'warning' | 'error';
  timestamp: Date;
  read: boolean;
}

interface NotificationContextType {
  notifications: Notification[];
  unreadCount: number;
  addNotification: (notification: Omit<Notification, 'id' | 'timestamp' | 'read'>) => void;
  markAsRead: (id: string) => void;
  markAllAsRead: () => void;
  clearNotification: (id: string) => void;
  clearAll: () => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export const NotificationProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(import.meta.env.VITE_SIGNALR_URL || 'http://localhost:5197/notificationsHub')
      .withAutomaticReconnect()
      .build();

    newConnection
      .start()
      .then(() => {
        console.log('SignalR Connected');
      })
      .catch((err) => {
        console.error('SignalR Connection Error: ', err);
      });

    newConnection.on('ReceiveNotification', (title: string, message: string, type: string) => {
      addNotification({
        title,
        message,
        type: type as Notification['type'],
      });
    });

    setConnection(newConnection);

    return () => {
      if (newConnection) {
        newConnection.stop();
      }
    };
  }, []);

  const addNotification = (notification: Omit<Notification, 'id' | 'timestamp' | 'read'>) => {
    const newNotification: Notification = {
      ...notification,
      id: Date.now().toString(),
      timestamp: new Date(),
      read: false,
    };
    setNotifications((prev) => [newNotification, ...prev]);
  };

  const markAsRead = (id: string) => {
    setNotifications((prev) =>
      prev.map((notif) => (notif.id === id ? { ...notif, read: true } : notif))
    );
  };

  const markAllAsRead = () => {
    setNotifications((prev) => prev.map((notif) => ({ ...notif, read: true })));
  };

  const clearNotification = (id: string) => {
    setNotifications((prev) => prev.filter((notif) => notif.id !== id));
  };

  const clearAll = () => {
    setNotifications([]);
  };

  const unreadCount = notifications.filter((n) => !n.read).length;

  const value: NotificationContextType = {
    notifications,
    unreadCount,
    addNotification,
    markAsRead,
    markAllAsRead,
    clearNotification,
    clearAll,
  };

  return <NotificationContext.Provider value={value}>{children}</NotificationContext.Provider>;
};

export const useNotifications = () => {
  const context = useContext(NotificationContext);
  if (context === undefined) {
    throw new Error('useNotifications must be used within a NotificationProvider');
  }
  return context;
};

