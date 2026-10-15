import { useEffect, useState, useCallback, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { MessageDto } from '../api/messagesApi';
import type { ChatGroupMessage } from '../shared/api/chatGroups';
import { apiBaseUrl } from '../shared/api/client';
import { getAccessToken } from '../shared/api/tokenStore';

export const useChatSignalR = (userId: number | null) => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [connected, setConnected] = useState(false);
  const [newMessage, setNewMessage] = useState<MessageDto | null>(null);
  // Ids of messages the other person has read. Accumulated (not a single value) because several
  // "MessageRead" events can arrive within one render and none of them may be lost.
  const [readMessageIds, setReadMessageIds] = useState<number[]>([]);
  const [userTyping, setUserTyping] = useState<number | null>(null);
  // Group conversations (roommates + landlord): the latest message pushed to us, and a counter that
  // ticks whenever somebody adds us to a new group so the list can be reloaded.
  const [newGroupMessage, setNewGroupMessage] = useState<ChatGroupMessage | null>(null);
  const [groupsChanged, setGroupsChanged] = useState(0);
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const typingTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (!userId) return;

    const chatHubUrl = apiBaseUrl + '/chatHub';
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(chatHubUrl, {
        // Use in-memory token — never sessionStorage (XSS risk).
        accessTokenFactory: () => getAccessToken() ?? ''
      })
      .withAutomaticReconnect()
      .build();

    connectionRef.current = newConnection;
    setConnection(newConnection);

    // Handlers first, then start: a message that arrives right after the connection opens is not missed.
    newConnection.on('ReceiveMessage', (message: MessageDto) => {
      setNewMessage(message);
    });

    newConnection.on('MessageSent', (message: MessageDto) => {
      setNewMessage(message);
    });

    newConnection.on('ReceiveGroupMessage', (message: ChatGroupMessage) => {
      setNewGroupMessage(message);
    });

    newConnection.on('GroupCreated', () => {
      setGroupsChanged(count => count + 1);
    });

    newConnection.on('MessageRead', (data: { messageId: number }) => {
      setReadMessageIds(prev => (prev.includes(data.messageId) ? prev : [...prev, data.messageId]));
    });

    // The other person opened the conversation: everything they had not read, in one event.
    newConnection.on('MessagesRead', (data: { messageIds: number[] }) => {
      setReadMessageIds(prev => {
        const fresh = (data.messageIds ?? []).filter(id => !prev.includes(id));
        return fresh.length === 0 ? prev : [...prev, ...fresh];
      });
    });

    newConnection.on('UserTyping', (data: { userId: number }) => {
      setUserTyping(data.userId);
      // Clear any previous typing timeout before starting a new one
      if (typingTimeoutRef.current !== null) {
        clearTimeout(typingTimeoutRef.current);
      }
      typingTimeoutRef.current = setTimeout(() => {
        setUserTyping(null);
        typingTimeoutRef.current = null;
      }, 3000);
    });

    // "connected" follows the real state of the connection, so the page falls back to REST while it is down.
    newConnection.onreconnecting(() => setConnected(false));
    newConnection.onreconnected(() => {
      setConnected(true);
      // The server puts a new connection into the user's room itself; asking again is harmless.
      newConnection.invoke('JoinChatRoom', userId).catch(() => {});
      // Whatever arrived while the connection was down is read from the server.
      setGroupsChanged(count => count + 1);
    });
    newConnection.onclose(() => setConnected(false));

    newConnection.start()
      .then(() => {
        setConnected(true);
        newConnection.invoke('JoinChatRoom', userId).catch(() => {});
      })
      .catch(() => {
        setConnected(false);
      });

    return () => {
      // Cancel pending typing-indicator timeout to avoid setState on unmounted component
      if (typingTimeoutRef.current !== null) {
        clearTimeout(typingTimeoutRef.current);
        typingTimeoutRef.current = null;
      }
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, [userId]);

  /** Resolves true if the hub accepted the message, false if it was not sent (caller can fall back to REST). */
  const sendMessage = useCallback(async (receiverId: number, messageText: string): Promise<boolean> => {
    if (!connection || !connected) return false;
    try {
      await connection.invoke('SendMessage', receiverId, messageText);
      return true;
    } catch {
      return false;
    }
  }, [connection, connected]);

  const markAsRead = useCallback(async (messageId: number) => {
    if (connection && connected) {
      try {
        await connection.invoke('MarkMessageAsRead', messageId);
      } catch {
        // Best-effort: a missed read receipt is harmless; the unread count is re-synced from the API.
      }
    }
  }, [connection, connected]);

  const notifyTyping = useCallback(async (userId: number, receiverId: number) => {
    if (connection && connected) {
      try {
        await connection.invoke('UserTyping', userId, receiverId);
      } catch {
        // Best-effort: a dropped typing indicator is purely cosmetic.
      }
    }
  }, [connection, connected]);

  return {
    connected,
    sendMessage,
    markAsRead,
    notifyTyping,
    newMessage,
    readMessageIds,
    userTyping,
    newGroupMessage,
    groupsChanged,
  };
};
