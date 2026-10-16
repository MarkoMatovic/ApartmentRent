import { useEffect, useState, useCallback, useRef } from 'react';
import { MessageDto } from '../api/messagesApi';
import type { ChatGroupMessage } from '../shared/api/chatGroups';
import { useNotifications } from '../shared/context/NotificationContext';

/**
 * The chat page's view of the tab's live connection. It does not open a connection of its own: it uses
 * the one NotificationContext keeps for the bell, so a tab holds one connection, not two or three.
 */
export const useChatSignalR = (userId: number | null) => {
  const { liveConnection, liveConnected, liveReconnects } = useNotifications();
  const connection = userId ? liveConnection : null;
  const connected = !!connection && liveConnected;

  const [newMessage, setNewMessage] = useState<MessageDto | null>(null);
  // Ids of messages the other person has read. Accumulated (not a single value) because several
  // "MessageRead" events can arrive within one render and none of them may be lost.
  const [readMessageIds, setReadMessageIds] = useState<number[]>([]);
  const [userTyping, setUserTyping] = useState<number | null>(null);
  // Group conversations (roommates + landlord): the latest message pushed to us, and a counter that
  // ticks whenever somebody adds us to a new group so the list can be reloaded.
  const [newGroupMessage, setNewGroupMessage] = useState<ChatGroupMessage | null>(null);
  const [groupsChanged, setGroupsChanged] = useState(0);
  const typingTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (!connection) return;

    const onMessage = (message: MessageDto) => setNewMessage(message);
    const onGroupMessage = (message: ChatGroupMessage) => setNewGroupMessage(message);
    const onGroupCreated = () => setGroupsChanged(count => count + 1);
    const onRead = (data: { messageId: number }) =>
      setReadMessageIds(prev => (prev.includes(data.messageId) ? prev : [...prev, data.messageId]));
    // The other person opened the conversation: everything they had not read, in one event.
    const onAllRead = (data: { messageIds: number[] }) =>
      setReadMessageIds(prev => {
        const fresh = (data.messageIds ?? []).filter(id => !prev.includes(id));
        return fresh.length === 0 ? prev : [...prev, ...fresh];
      });
    const onTyping = (data: { userId: number }) => {
      setUserTyping(data.userId);
      // Clear any previous typing timeout before starting a new one
      if (typingTimeoutRef.current !== null) clearTimeout(typingTimeoutRef.current);
      typingTimeoutRef.current = setTimeout(() => {
        setUserTyping(null);
        typingTimeoutRef.current = null;
      }, 3000);
    };

    connection.on('ReceiveMessage', onMessage);
    connection.on('MessageSent', onMessage);
    connection.on('ReceiveGroupMessage', onGroupMessage);
    connection.on('GroupCreated', onGroupCreated);
    connection.on('MessageRead', onRead);
    connection.on('MessagesRead', onAllRead);
    connection.on('UserTyping', onTyping);

    return () => {
      // Only this page's handlers go; the connection stays open for the bell.
      connection.off('ReceiveMessage', onMessage);
      connection.off('MessageSent', onMessage);
      connection.off('ReceiveGroupMessage', onGroupMessage);
      connection.off('GroupCreated', onGroupCreated);
      connection.off('MessageRead', onRead);
      connection.off('MessagesRead', onAllRead);
      connection.off('UserTyping', onTyping);
      // Cancel pending typing-indicator timeout to avoid setState on unmounted component
      if (typingTimeoutRef.current !== null) {
        clearTimeout(typingTimeoutRef.current);
        typingTimeoutRef.current = null;
      }
    };
  }, [connection]);

  // Whatever arrived while the connection was down is read from the server.
  useEffect(() => {
    if (liveReconnects > 0) setGroupsChanged(count => count + 1);
  }, [liveReconnects]);

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

  /** The hub takes only the receiver (the sender comes from the token); it used to get both and refuse. */
  const notifyTyping = useCallback(async (_userId: number, receiverId: number) => {
    if (connection && connected) {
      try {
        await connection.invoke('UserTyping', receiverId);
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
