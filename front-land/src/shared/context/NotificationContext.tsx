import React, { createContext, useContext, useState, useEffect, useCallback, useRef, ReactNode } from 'react';
import * as signalR from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { messagesApi } from '../../api/messagesApi';
import RejectionDialog from '../../components/Dialogs/RejectionDialog';
import { parseServerDate } from '../../components/Chat/chatMessageUtils';
import { apiBaseUrl } from '../api/client';
import { getAccessToken } from '../api/tokenStore';
import { notificationsApi, StoredNotification } from '../api/notifications';
import { BILLS_KEY, BILLS_SUMMARY_KEY, MY_PAIRING_KEY, SHARED_APARTMENTS_KEY, TENANCIES_KEY } from '../api/queryKeys';
import { NotificationDescriptor, isInAppRoute, parseDescriptor } from '../notifications/descriptor';
import { useAuth } from './AuthContext';

interface Notification {
  id: string;
  title: string;
  message: string;
  type: 'info' | 'success' | 'warning' | 'error';
  timestamp: Date;
  read: boolean;
  /** Set by notifications that come from the server: translated at display time, and where a click leads. */
  descriptor?: NotificationDescriptor;
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
  /** The tab's one live connection (chat and notifications); null while nobody is signed in. */
  liveConnection: signalR.HubConnection | null;
  liveConnected: boolean;
  /** Goes up after every reconnect, so whoever uses the connection can reload what may have been missed. */
  liveReconnects: number;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

/** The bell keeps this many; the older ones stay on the server until they are marked read. */
const MAX_NOTIFICATIONS = 50;
/** While the connection is down the unread count is asked for this often... */
const UNREAD_POLL_MS = 30_000;
/** ...and while it is up only this often (new messages refresh it live; this catches reads in another tab). */
const UNREAD_POLL_CONNECTED_MS = 5 * 60_000;

const hubUrl = (path: string) => apiBaseUrl + path;

const buildConnection = (url: string) =>
  new signalR.HubConnectionBuilder()
    .withUrl(url, {
      // Read from the in-memory store each time (a reconnect may come after the token was renewed).
      accessTokenFactory: () => getAccessToken() ?? '',
    })
    .withAutomaticReconnect()
    .build();

/** A notification stored on the server, shown in the bell as if it had just arrived. */
const fromStored = (n: StoredNotification): Notification => ({
  id: `server-${n.id}`,
  title: n.title,
  message: n.message,
  type: 'info',
  timestamp: parseServerDate(n.createdDate),
  read: false,
  descriptor: {
    id: n.id,
    key: n.key ?? undefined,
    params: n.params ?? undefined,
    link: isInAppRoute(n.actionTarget) ? n.actionTarget : undefined,
  },
});

/** Newest first, one entry per stored notification, no more than the bell can hold. */
const mergeNotifications = (current: Notification[], incoming: Notification[]): Notification[] => {
  const known = new Set(current.map(n => n.descriptor?.id).filter((id): id is number => id !== undefined));
  const fresh = incoming.filter(n => n.descriptor?.id === undefined || !known.has(n.descriptor.id));
  return [...current, ...fresh]
    .sort((a, b) => b.timestamp.getTime() - a.timestamp.getTime())
    .slice(0, MAX_NOTIFICATIONS);
};

export const NotificationProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const queryClient = useQueryClient();
  const { user, token } = useAuth();
  // Only a signed-in user (a token AND the user it belongs to) gets live connections.
  const userId = token && user && user.userId > 0 ? user.userId : null;
  const userIdRef = useRef<number | null>(userId);
  userIdRef.current = userId;

  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadMessagesCount, setUnreadMessagesCount] = useState<number>(0);
  const [rejectionInfo, setRejectionInfo] = useState<{ apartmentTitle: string; apartmentId: number } | null>(null);
  const [liveConnection, setLiveConnection] = useState<signalR.HubConnection | null>(null);
  const [liveConnected, setLiveConnected] = useState(false);
  const [liveReconnects, setLiveReconnects] = useState(0);

  // Broj nepročitanih poruka — memoised so consumers get a stable reference
  const refreshUnreadMessagesCount = useCallback(async () => {
    const id = userIdRef.current;
    if (!id) return;
    try {
      const count = await messagesApi.getUnreadCount(id);
      // The user may have signed out while the request was running.
      if (userIdRef.current === id) setUnreadMessagesCount(count);
    } catch {
      // The badge simply keeps its last value.
    }
  }, []);

  const addNotification = useCallback((notification: Omit<Notification, 'id' | 'timestamp' | 'read'>) => {
    const newNotification: Notification = {
      ...notification,
      id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
      timestamp: new Date(),
      read: false,
    };
    // Cap so the in-memory array never grows unboundedly during a long session.
    setNotifications((prev) => [newNotification, ...prev].slice(0, MAX_NOTIFICATIONS));
  }, []);

  // Connect when somebody is signed in, disconnect when they sign out. This used to run once when the
  // app started, before the session was restored, so there was no token, nothing connected, and
  // notifications never arrived live until the page was loaded again (and then the same happened).
  useEffect(() => {
    // Whatever the bell and the badge held belonged to somebody else (or to nobody): start from nothing.
    setNotifications([]);
    setUnreadMessagesCount(0);
    if (!userId) return;

    // Notifications that arrived while the user was away (or before this page was opened).
    const loadStored = () =>
      notificationsApi
        .getUserNotifications(userId)
        .then(stored => {
          if (userIdRef.current !== userId) return;
          setNotifications(prev => mergeNotifications(prev, stored.map(fromStored)));
        })
        .catch(() => {
          // The live ones still work; the stored ones are tried again at the next reconnect.
        });

    // One connection per tab for the bell and the chat (the chat page uses this one too): every extra
    // connection is one more the server has to hold for each person on the site.
    const hub = buildConnection(hubUrl('/chatHub'));

    // Handlers first, then start: nothing that arrives right after the connection opens is missed.
    hub.on('ReceiveNotification', (title: string, message: string, type: string, metadata?: string, descriptorJson?: string) => {
      const descriptor = parseDescriptor(descriptorJson);

      // The other person answered a roommate proposal or shared an apartment: what this user sees is out of date now.
      if (descriptor?.key?.startsWith('pairing_')) {
        queryClient.invalidateQueries({ queryKey: [MY_PAIRING_KEY] });
        queryClient.invalidateQueries({ queryKey: [SHARED_APARTMENTS_KEY] });
      }

      // A bill, a payment or a tenancy changed somewhere else: the badge and the bills pages follow.
      if (descriptor?.key?.startsWith('bill_') || descriptor?.key?.startsWith('tenancy_')) {
        queryClient.invalidateQueries({ queryKey: [BILLS_KEY] });
        queryClient.invalidateQueries({ queryKey: [BILLS_SUMMARY_KEY] });
        queryClient.invalidateQueries({ queryKey: [TENANCIES_KEY] });
      }

      // A rejected application opens the dialog that offers to keep or remove the apartment.
      if (type === 'rejection' || title.toLowerCase().includes('reject') || message.toLowerCase().includes('rejected')) {
        try {
          const meta = metadata ? JSON.parse(metadata) : null;
          setRejectionInfo({ apartmentTitle: meta?.apartmentTitle || 'the apartment', apartmentId: meta?.apartmentId || 0 });
        } catch {
          setRejectionInfo({ apartmentTitle: 'the apartment', apartmentId: 0 });
        }
      }

      const live: Notification = {
        id: descriptor?.id !== undefined ? `server-${descriptor.id}` : `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
        title,
        message,
        type: (type === 'rejection' ? 'error' : type) as Notification['type'],
        timestamp: new Date(),
        read: false,
        descriptor,
      };
      setNotifications(prev => mergeNotifications(prev, [live]));
    });

    // Chat: the badge counts one-to-one and shared-conversation messages alike.
    hub.on('ReceiveMessage', refreshUnreadMessagesCount);
    hub.on('MessageSent', refreshUnreadMessagesCount);
    hub.on('ReceiveGroupMessage', refreshUnreadMessagesCount);

    // "connected" follows the real state, so the chat page falls back to REST while the connection is down.
    hub.onreconnecting(() => setLiveConnected(false));
    hub.onclose(() => setLiveConnected(false));
    // A reconnect is a new connection: whatever came in the meantime is fetched, not guessed.
    hub.onreconnected(() => {
      setLiveConnected(true);
      // The server puts a new connection into the user's room and group itself; asking again is harmless.
      hub.invoke('JoinChatRoom', userId).catch(() => { });
      loadStored();
      refreshUnreadMessagesCount();
      setLiveReconnects(count => count + 1);
    });

    setLiveConnection(hub);
    hub
      .start()
      .then(() => {
        setLiveConnected(true);
        return hub.invoke('JoinChatRoom', userId);
      })
      .catch(() => { });

    loadStored();
    refreshUnreadMessagesCount();
    let lastPoll = Date.now();
    const interval = setInterval(() => {
      const live = hub.state === signalR.HubConnectionState.Connected;
      if (!live || Date.now() - lastPoll >= UNREAD_POLL_CONNECTED_MS) {
        lastPoll = Date.now();
        refreshUnreadMessagesCount();
      }
    }, UNREAD_POLL_MS);

    return () => {
      clearInterval(interval);
      setLiveConnection(null);
      setLiveConnected(false);
      hub.stop().catch(() => { });
    };
  }, [userId, queryClient, refreshUnreadMessagesCount]);

  // What the user marks read in the bell is marked read on the server too, or it would come back at the next load.
  const markOnServer = (call: () => Promise<unknown>) => { call().catch(() => { }); };

  const markAsRead = (id: string) => {
    const serverId = notifications.find(n => n.id === id)?.descriptor?.id;
    setNotifications((prev) => prev.map((notif) => (notif.id === id ? { ...notif, read: true } : notif)));
    if (serverId !== undefined) markOnServer(() => notificationsApi.markAsRead(serverId));
  };

  const markAllAsRead = () => {
    setNotifications((prev) => prev.map((notif) => ({ ...notif, read: true })));
    if (userId) markOnServer(() => notificationsApi.markAllAsRead());
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
    liveConnection,
    liveConnected,
    liveReconnects,
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
