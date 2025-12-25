import React, { useState, useEffect, useRef } from 'react';
import {
  Container,
  Box,
  Paper,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Avatar,
  TextField,
  IconButton,
  Typography,
  Badge,
  CircularProgress
} from '@mui/material';
import SendIcon from '@mui/icons-material/Send';
import { useTranslation } from 'react-i18next';
import { messagesApi, ConversationDto, MessageDto } from '../api/messagesApi';
import { useChatSignalR } from '../hooks/useChatSignalR';
import { useAuth } from '../shared/context/AuthContext';

const ChatPage: React.FC = () => {
  const { t } = useTranslation('chat');
  const { user } = useAuth();
  const [conversations, setConversations] = useState<ConversationDto[]>([]);
  const [selectedConversation, setSelectedConversation] = useState<ConversationDto | null>(null);
  const [messages, setMessages] = useState<MessageDto[]>([]);
  const [messageText, setMessageText] = useState('');
  const [loading, setLoading] = useState(true);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const currentUserId = user?.userId || 1;
  const { connected, sendMessage, markAsRead, newMessage, userTyping } = useChatSignalR(currentUserId);

  useEffect(() => {
    loadConversations();
  }, [currentUserId]);

  useEffect(() => {
    if (newMessage) {
      if (selectedConversation && 
          (newMessage.senderId === selectedConversation.otherUserId || 
           newMessage.receiverId === selectedConversation.otherUserId)) {
        setMessages(prev => [...prev, newMessage]);
        
        if (newMessage.receiverId === currentUserId) {
          markAsRead(newMessage.messageId);
        }
      }
      
      loadConversations();
    }
  }, [newMessage, selectedConversation, currentUserId, markAsRead]);

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const loadConversations = async () => {
    try {
      const data = await messagesApi.getUserConversations(currentUserId);
      setConversations(data);
      setLoading(false);
    } catch {
      setLoading(false);
    }
  };

  const loadMessages = async (conv: ConversationDto) => {
    try {
      const data = await messagesApi.getConversation(currentUserId, conv.otherUserId);
      setMessages(data.messages);
      setSelectedConversation(conv);
    } catch {
    }
  };

  const handleSendMessage = async () => {
    if (!messageText.trim() || !selectedConversation) return;

    try {
      if (connected) {
        await sendMessage(currentUserId, selectedConversation.otherUserId, messageText);
      } else {
        const newMsg = await messagesApi.sendMessage({
          senderId: currentUserId,
          receiverId: selectedConversation.otherUserId,
          messageText
        });
        setMessages(prev => [...prev, newMsg]);
      }
      
      setMessageText('');
    } catch {
    }
  };

  const formatTime = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
  };

  if (loading) {
    return (
      <Container maxWidth="lg" sx={{ py: 4, display: 'flex', justifyContent: 'center' }}>
        <CircularProgress />
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 4, height: 'calc(100vh - 100px)' }}>
      <Typography variant="h4" component="h1" gutterBottom>
        {t('title', 'Messages')}
      </Typography>
      
      <Paper sx={{ display: 'flex', height: '80vh' }}>
        <Box sx={{ width: 300, borderRight: 1, borderColor: 'divider', overflow: 'auto' }}>
          <List>
            {conversations.length === 0 ? (
              <ListItem>
                <ListItemText 
                  primary={t('noConversations', 'No conversations yet')}
                  primaryTypographyProps={{ align: 'center', color: 'text.secondary' }}
                />
              </ListItem>
            ) : (
              conversations.map((conv) => (
                <ListItem
                  button
                  key={conv.otherUserId}
                  selected={selectedConversation?.otherUserId === conv.otherUserId}
                  onClick={() => loadMessages(conv)}
                >
                  <ListItemAvatar>
                    <Badge badgeContent={conv.unreadCount} color="error">
                      <Avatar src={conv.otherUserProfilePicture || undefined}>
                        {conv.otherUserName?.charAt(0)}
                      </Avatar>
                    </Badge>
                  </ListItemAvatar>
                  <ListItemText
                    primary={conv.otherUserName}
                    secondary={
                      conv.lastMessage && conv.lastMessage.messageText.length > 30
                        ? conv.lastMessage.messageText.substring(0, 30) + '...'
                        : conv.lastMessage?.messageText
                    }
                    secondaryTypographyProps={{
                      noWrap: true,
                      fontWeight: conv.unreadCount > 0 ? 'bold' : 'normal'
                    }}
                  />
                </ListItem>
              ))
            )}
          </List>
        </Box>

        <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
          {!selectedConversation ? (
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', flex: 1 }}>
              <Typography color="text.secondary">
                {t('selectConversation', 'Select a conversation to start messaging')}
              </Typography>
            </Box>
          ) : (
            <>
              <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider' }}>
                <Box sx={{ display: 'flex', alignItems: 'center' }}>
                  <Avatar src={selectedConversation.otherUserProfilePicture || undefined} sx={{ mr: 2 }}>
                    {selectedConversation.otherUserName?.charAt(0)}
                  </Avatar>
                  <Box>
                    <Typography variant="h6">{selectedConversation.otherUserName}</Typography>
                    {userTyping === selectedConversation.otherUserId && (
                      <Typography variant="caption" color="text.secondary">
                        {t('typing', 'typing...')}
                      </Typography>
                    )}
                  </Box>
                </Box>
              </Box>

              <Box sx={{ flex: 1, overflow: 'auto', p: 2 }}>
                {messages.map((msg) => {
                  const isOwn = msg.senderId === currentUserId;
                  return (
                    <Box
                      key={msg.messageId}
                      sx={{
                        display: 'flex',
                        justifyContent: isOwn ? 'flex-end' : 'flex-start',
                        mb: 2
                      }}
                    >
                      <Box
                        sx={{
                          maxWidth: '70%',
                          p: 1.5,
                          borderRadius: 2,
                          bgcolor: isOwn ? 'primary.main' : 'grey.200',
                          color: isOwn ? 'white' : 'text.primary'
                        }}
                      >
                        <Typography variant="body1">{msg.messageText}</Typography>
                        <Typography variant="caption" sx={{ opacity: 0.7, display: 'block', mt: 0.5 }}>
                          {formatTime(msg.sentAt)}
                          {isOwn && msg.isRead && ' • Read'}
                        </Typography>
                      </Box>
                    </Box>
                  );
                })}
                <div ref={messagesEndRef} />
              </Box>

              <Box sx={{ p: 2, borderTop: 1, borderColor: 'divider' }}>
                <Box sx={{ display: 'flex', gap: 1 }}>
                  <TextField
                    fullWidth
                    variant="outlined"
                    placeholder={t('typeMessage', 'Type a message...')}
                    value={messageText}
                    onChange={(e) => setMessageText(e.target.value)}
                    onKeyPress={(e) => {
                      if (e.key === 'Enter' && !e.shiftKey) {
                        e.preventDefault();
                        handleSendMessage();
                      }
                    }}
                  />
                  <IconButton
                    color="primary"
                    onClick={handleSendMessage}
                    disabled={!messageText.trim()}
                  >
                    <SendIcon />
                  </IconButton>
                </Box>
                {!connected && (
                  <Typography variant="caption" color="error" sx={{ mt: 1 }}>
                    {t('offline', 'Offline - messages will be sent when reconnected')}
                  </Typography>
                )}
              </Box>
            </>
          )}
        </Box>
      </Paper>
    </Container>
  );
};

export default ChatPage;
