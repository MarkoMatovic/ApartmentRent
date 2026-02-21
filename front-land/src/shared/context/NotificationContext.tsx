import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import * as signalR from '@microsoft/signalr';
import { messagesApi } from '../../api/messagesApi';
import RejectionDialog from '../../components/Dialogs/RejectionDialog';

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
  unreadMessagesCount: number;
  refreshUnreadMessagesCount: () => Promise<void>;
  addNotification: (notification: Omit<Notification, 'id' | 'timestamp' | 'read'>) => void;
  markAsRead: (id: string) => void;
  markAllAsRead: () => void;
  clearNotification: (id: string) => void;
  clearAll: () => void;
  showRejectionDialog: (apartmentTitle: string, apartmentId: number) => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export const NotificationProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadMessagesCount, setUnreadMessagesCount] = useState<number>(0);
  const [rejectionInfo, setRejectionInfo] = useState<{ apartmentTitle: string; apartmentId: number } | null>(null);

  // Učitaj broj nepročitanih poruka
  const refreshUnreadMessagesCount = async () => {
    try {
      const token = localStorage.getItem('authToken');
      if (!token) return;

      // Parsiraj userId iz JWT tokena
      const payload = JSON.parse(atob(token.split('.')[1]));
      const userId = parseInt(payload.userId);

      if (userId) {
        const count = await messagesApi.getUnreadCount(userId);
        setUnreadMessagesCount(count);
      }
    } catch (error) {
      // Ignoriši greške
    }
  };

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(import.meta.env.VITE_SIGNALR_URL || 'https://localhost:7092/notificationHub')
      .withAutomaticReconnect()
      .build();

    newConnection
      .start()
      .catch(() => { });

    newConnection.on('ReceiveNotification', (title: string, message: string, type: string, metadata?: string) => {
      // Check if this is a rejection notification
      if (type === 'rejection' || title.toLowerCase().includes('reject') || message.toLowerCase().includes('rejected')) {
        try {
          const meta = metadata ? JSON.parse(metadata) : null;
          const aptTitle = meta?.apartmentTitle || 'the apartment';
          const aptId = meta?.apartmentId || 0;
          setRejectionInfo({ apartmentTitle: aptTitle, apartmentId: aptId });
        } catch {
          setRejectionInfo({ apartmentTitle: 'the apartment', apartmentId: 0 });
        }
      }
      addNotification({
        title,
        message,
        type: (type === 'rejection' ? 'error' : type) as Notification['type'],
      });
    });

    return () => {
      if (newConnection) {
        newConnection.stop();
      }
    };
  }, []);

  // Poveži se na ChatHub za real-time ažuriranje broja nepročitanih poruka
  useEffect(() => {
    const token = localStorage.getItem('authToken');
    if (!token) return;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const userId = parseInt(payload.userId);

      if (!userId) return;

      const newChatConnection = new signalR.HubConnectionBuilder()
        .withUrl('https://localhost:7092/chatHub', {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();

      newChatConnection
        .start()
        .then(() => {
          newChatConnection.invoke('JoinChatRoom', userId).catch(() => { });
          // Učitaj broj nepročitanih poruka kada se poveže
          refreshUnreadMessagesCount();
        })
        .catch(() => { });

      // Osveži broj nepročitanih poruka kada stigne nova poruka
      newChatConnection.on('ReceiveMessage', () => {
        refreshUnreadMessagesCount();
      });

      newChatConnection.on('MessageSent', () => {
        refreshUnreadMessagesCount();
      });

      return () => {
        if (newChatConnection) {
          newChatConnection.stop();
        }
      };
    } catch (error) {
      // Ignoriši greške
    }
  }, []);

  // Učitaj broj nepročitanih poruka kada postoji token (korisnik je ulogovan)
  useEffect(() => {
    const checkAndLoad = async () => {
      const token = localStorage.getItem('authToken');
      if (!token) {
        setUnreadMessagesCount(0);
        return;
      }

      await refreshUnreadMessagesCount();
    };

    // Proveri odmah kada se komponenta mount-uje
    checkAndLoad();

    // Osveži svakih 30 sekundi
    const interval = setInterval(() => {
      checkAndLoad();
    }, 30000);

    // Osluškuj custom event za promenu tokena (kada se korisnik uloguje u istom prozoru)
    const handleAuthChange = () => {
      checkAndLoad();
    };

    window.addEventListener('authTokenChanged', handleAuthChange);
    // Takođe osluškuj storage event za promene iz drugih prozora
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === 'authToken') {
        checkAndLoad();
      }
    };

    window.addEventListener('storage', handleStorageChange);

    return () => {
      clearInterval(interval);
      window.removeEventListener('authTokenChanged', handleAuthChange);
      window.removeEventListener('storage', handleStorageChange);
    };
  }, []); // Prazan dependency array - učitava se samo jednom pri mount-u

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

  const showRejectionDialog = (apartmentTitle: string, apartmentId: number) => {
    setRejectionInfo({ apartmentTitle, apartmentId });
  };

  const handleKeepApartment = () => {
    setRejectionInfo(null);
  };

  const handleRemoveApartment = () => {
    // Could call an API to hide this apartment from search in the future
    setRejectionInfo(null);
  };

  const value: NotificationContextType = {
    notifications,
    unreadCount,
    unreadMessagesCount,
    refreshUnreadMessagesCount,
    addNotification,
    markAsRead,
    markAllAsRead,
    clearNotification,
    clearAll,
    showRejectionDialog,
  };

  return (
    <NotificationContext.Provider value={value}>
      {children}
      {rejectionInfo && (
        <RejectionDialog
          open={true}
          apartmentTitle={rejectionInfo.apartmentTitle}
          onKeep={handleKeepApartment}
          onRemove={handleRemoveApartment}
        />
      )}
    </NotificationContext.Provider>
  );
};

export const useNotifications = () => {
  const context = useContext(NotificationContext);
  if (context === undefined) {
    throw new Error('useNotifications must be used within a NotificationProvider');
  }
  return context;
};

