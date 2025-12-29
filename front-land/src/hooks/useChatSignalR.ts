import { useEffect, useState, useCallback, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { MessageDto } from '../api/messagesApi';

export const useChatSignalR = (userId: number | null) => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [connected, setConnected] = useState(false);
  const [newMessage, setNewMessage] = useState<MessageDto | null>(null);
  const [messageRead, setMessageRead] = useState<number | null>(null);
  const [userTyping, setUserTyping] = useState<number | null>(null);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!userId) return;

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7092/chatHub', {
        accessTokenFactory: () => {
          return localStorage.getItem('authToken') || '';
        }
      })
      .withAutomaticReconnect()
      .build();

    connectionRef.current = newConnection;
    setConnection(newConnection);

    newConnection.start()
      .then(() => {
        setConnected(true);
        
        newConnection.invoke('JoinChatRoom', userId)
          .catch(() => {});

        newConnection.on('ReceiveMessage', (message: MessageDto) => {
          setNewMessage(message);
        });

        newConnection.on('MessageSent', (message: MessageDto) => {
          setNewMessage(message);
        });

        newConnection.on('MessageRead', (data: { messageId: number }) => {
          setMessageRead(data.messageId);
        });

        newConnection.on('UserTyping', (data: { userId: number }) => {
          setUserTyping(data.userId);
          setTimeout(() => setUserTyping(null), 3000);
        });
      })
      .catch(() => {
        setConnected(false);
      });

    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, [userId]);

  const sendMessage = useCallback(async (senderId: number, receiverId: number, messageText: string) => {
    if (connection && connected) {
      try {
        await connection.invoke('SendMessage', senderId, receiverId, messageText);
      } catch {
      }
    }
  }, [connection, connected]);

  const markAsRead = useCallback(async (messageId: number) => {
    if (connection && connected) {
      try {
        await connection.invoke('MarkMessageAsRead', messageId);
      } catch {
      }
    }
  }, [connection, connected]);

  const notifyTyping = useCallback(async (userId: number, receiverId: number) => {
    if (connection && connected) {
      try {
        await connection.invoke('UserTyping', userId, receiverId);
      } catch {
      }
    }
  }, [connection, connected]);

  return {
    connected,
    sendMessage,
    markAsRead,
    notifyTyping,
    newMessage,
    messageRead,
    userTyping
  };
};
