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
        console.log('Chat SignalR Connected!');
        setConnected(true);
        
        newConnection.invoke('JoinChatRoom', userId)
          .catch(err => console.error('Error joining chat room:', err));

        newConnection.on('ReceiveMessage', (message: MessageDto) => {
          console.log('New message received:', message);
          setNewMessage(message);
        });

        newConnection.on('MessageSent', (message: MessageDto) => {
          console.log('Message sent confirmation:', message);
          setNewMessage(message);
        });

        newConnection.on('MessageRead', (data: { messageId: number }) => {
          console.log('Message read:', data.messageId);
          setMessageRead(data.messageId);
        });

        newConnection.on('UserTyping', (data: { userId: number }) => {
          console.log('User typing:', data.userId);
          setUserTyping(data.userId);
          setTimeout(() => setUserTyping(null), 3000);
        });
      })
      .catch(err => {
        console.error('Chat SignalR Connection Error:', err);
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
      } catch (err) {
        console.error('Error sending message:', err);
      }
    }
  }, [connection, connected]);

  const markAsRead = useCallback(async (messageId: number) => {
    if (connection && connected) {
      try {
        await connection.invoke('MarkMessageAsRead', messageId);
      } catch (err) {
        console.error('Error marking message as read:', err);
      }
    }
  }, [connection, connected]);

  const notifyTyping = useCallback(async (userId: number, receiverId: number) => {
    if (connection && connected) {
      try {
        await connection.invoke('UserTyping', userId, receiverId);
      } catch (err) {
        console.error('Error notifying typing:', err);
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
