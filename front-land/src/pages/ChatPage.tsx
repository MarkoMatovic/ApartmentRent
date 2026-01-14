import React, { useState, useEffect, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  Container,
  Box,
  Paper,
  List,
  ListItemButton,
  ListItemAvatar,
  ListItemText,
  Avatar,
  TextField,
  IconButton,
  Typography,
  Badge,
  CircularProgress,
  Divider,
  Chip,
  InputAdornment,
  Fade,
  Zoom
} from '@mui/material';
import {
  Send as SendIcon,
  Search as SearchIcon,
  MoreVert as MoreVertIcon,
  DoneAll as DoneAllIcon,
  Done as DoneIcon,
  ChatBubbleOutline as ChatBubbleOutlineIcon
} from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { messagesApi, ConversationDto, MessageDto } from '../api/messagesApi';
import { useChatSignalR } from '../hooks/useChatSignalR';
import { useAuth } from '../shared/context/AuthContext';
import axios from 'axios';

const ChatPage: React.FC = () => {
  const { t } = useTranslation('chat');
  const { user } = useAuth();
  const [conversations, setConversations] = useState<ConversationDto[]>([]);
  const [selectedConversation, setSelectedConversation] = useState<ConversationDto | null>(null);
  const [messages, setMessages] = useState<MessageDto[]>([]);
  const [messageText, setMessageText] = useState('');
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const [searchParams] = useSearchParams();
  const targetUserIdParam = searchParams.get('userId');

  const currentUserId = user?.userId || 1;
  const { connected, sendMessage, markAsRead, newMessage, userTyping } = useChatSignalR(currentUserId);

  useEffect(() => {
    loadConversations();
  }, [currentUserId]);

  useEffect(() => {
    if (!loading && targetUserIdParam) {
      const targetId = Number(targetUserIdParam);

      if (!isNaN(targetId)) {
        // Find existing conversation
        const existing = conversations.find(c => c.otherUserId === targetId);

        if (existing) {
          setSelectedConversation(existing);
          loadMessages(existing);
        } else {
          // Load user info for new conversation
          loadUserInfoAndCreateConversation(targetId);
        }
      }
    }
  }, [targetUserIdParam, conversations, loading]);

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

  const loadUserInfoAndCreateConversation = async (userId: number) => {
    try {
      // Try to fetch user info from the users API
      const response = await axios.get(`https://localhost:7092/api/v1/users/${userId}`, {
        headers: {
          Authorization: `Bearer ${localStorage.getItem('authToken')}`
        }
      });

      const userData = response.data;
      const newConv: ConversationDto = {
        otherUserId: userId,
        otherUserName: `${userData.firstName} ${userData.lastName}`,
        otherUserProfilePicture: userData.profilePicture,
        unreadCount: 0
      };
      setSelectedConversation(newConv);
      setMessages([]);
    } catch (error) {
      // Fallback to basic conversation
      const newConv: ConversationDto = {
        otherUserId: userId,
        otherUserName: `User ${userId}`,
        unreadCount: 0
      };
      setSelectedConversation(newConv);
      setMessages([]);
    }
  };

  const loadConversations = async () => {
    try {
      const data = await messagesApi.getUserConversations(currentUserId);
      setConversations(data);
      setLoading(false);
    } catch (error) {
      setLoading(false);
    }
  };

  const loadMessages = async (conv: ConversationDto) => {
    try {
      const data = await messagesApi.getConversation(currentUserId, conv.otherUserId);
      setMessages(data.messages);
      setSelectedConversation(conv);
    } catch (error) {
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
      loadConversations();
    } catch (error) {
    }
  };

  const formatTime = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffInHours = (now.getTime() - date.getTime()) / (1000 * 60 * 60);

    if (diffInHours < 24) {
      return date.toLocaleTimeString('sr-RS', { hour: '2-digit', minute: '2-digit' });
    } else if (diffInHours < 48) {
      return t('yesterday', 'Juče');
    } else {
      return date.toLocaleDateString('sr-RS', { day: 'numeric', month: 'short' });
    }
  };

  const filteredConversations = conversations.filter(conv =>
    conv.otherUserName?.toLowerCase().includes(searchQuery.toLowerCase())
  );

  if (loading) {
    return (
      <Container maxWidth="xl" sx={{ py: 4, display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '80vh' }}>
        <CircularProgress size={60} />
      </Container>
    );
  }

  return (
    <Container maxWidth="xl" sx={{ py: 3, height: 'calc(100vh - 80px)' }}>
      <Box sx={{
        display: 'flex',
        height: '100%',
        gap: 2
      }}>
        {/* Conversations Sidebar */}
        <Paper
          elevation={3}
          sx={{
            width: 360,
            display: 'flex',
            flexDirection: 'column',
            borderRadius: 3,
            overflow: 'hidden',
            background: 'linear-gradient(to bottom, #ffffff 0%, #f8f9fa 100%)'
          }}
        >
          {/* Sidebar Header */}
          <Box sx={{
            p: 2.5,
            background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
            color: 'white'
          }}>
            <Typography variant="h5" fontWeight="700" gutterBottom>
              {t('title', 'Poruke')}
            </Typography>
            <TextField
              fullWidth
              size="small"
              placeholder={t('searchConversations', 'Pretraži razgovore...')}
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              sx={{
                mt: 1.5,
                '& .MuiOutlinedInput-root': {
                  bgcolor: 'rgba(255, 255, 255, 0.15)',
                  backdropFilter: 'blur(10px)',
                  borderRadius: 2,
                  color: 'white',
                  '& fieldset': { borderColor: 'rgba(255, 255, 255, 0.3)' },
                  '&:hover fieldset': { borderColor: 'rgba(255, 255, 255, 0.5)' },
                  '&.Mui-focused fieldset': { borderColor: 'white' }
                },
                '& .MuiInputBase-input::placeholder': {
                  color: 'rgba(255, 255, 255, 0.7)',
                  opacity: 1
                }
              }}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon sx={{ color: 'rgba(255, 255, 255, 0.7)' }} />
                  </InputAdornment>
                )
              }}
            />
          </Box>

          {/* Conversations List */}
          <List sx={{ flex: 1, overflow: 'auto', p: 1 }}>
            {filteredConversations.length === 0 ? (
              <Box sx={{ textAlign: 'center', py: 8 }}>
                <ChatBubbleOutlineIcon sx={{ fontSize: 64, color: 'text.disabled', mb: 2 }} />
                <Typography color="text.secondary">
                  {searchQuery ? t('noResults', 'Nema rezultata') : t('noConversations', 'Nema razgovora')}
                </Typography>
              </Box>
            ) : (
              filteredConversations.map((conv) => (
                <Fade in key={conv.otherUserId}>
                  <ListItemButton
                    selected={selectedConversation?.otherUserId === conv.otherUserId}
                    onClick={() => loadMessages(conv)}
                    sx={{
                      borderRadius: 2,
                      mb: 0.5,
                      transition: 'all 0.2s',
                      '&.Mui-selected': {
                        bgcolor: 'rgba(102, 126, 234, 0.1)',
                        borderLeft: '4px solid #667eea',
                        '&:hover': {
                          bgcolor: 'rgba(102, 126, 234, 0.15)',
                        }
                      },
                      '&:hover': {
                        bgcolor: 'rgba(0, 0, 0, 0.04)',
                        transform: 'translateX(4px)'
                      }
                    }}
                  >
                    <ListItemAvatar>
                      <Badge
                        badgeContent={conv.unreadCount}
                        color="error"
                        overlap="circular"
                      >
                        <Avatar
                          src={conv.otherUserProfilePicture || undefined}
                          sx={{
                            width: 50,
                            height: 50,
                            border: '2px solid #fff',
                            boxShadow: 2
                          }}
                        >
                          {conv.otherUserName?.charAt(0).toUpperCase()}
                        </Avatar>
                      </Badge>
                    </ListItemAvatar>
                    <ListItemText
                      primary={
                        <Typography variant="subtitle1" fontWeight={conv.unreadCount > 0 ? 700 : 500}>
                          {conv.otherUserName}
                        </Typography>
                      }
                      secondary={
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          sx={{
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            whiteSpace: 'nowrap',
                            fontWeight: conv.unreadCount > 0 ? 600 : 400
                          }}
                        >
                          {conv.lastMessage?.messageText || t('startConversation', 'Započni razgovor')}
                        </Typography>
                      }
                      sx={{ ml: 1.5 }}
                    />
                    {conv.lastMessage && (
                      <Typography variant="caption" color="text.secondary" sx={{ alignSelf: 'flex-start', mt: 1 }}>
                        {formatTime(conv.lastMessage.sentAt)}
                      </Typography>
                    )}
                  </ListItemButton>
                </Fade>
              ))
            )}
          </List>
        </Paper>

        {/* Chat Area */}
        <Paper
          elevation={3}
          sx={{
            flex: 1,
            display: 'flex',
            flexDirection: 'column',
            borderRadius: 3,
            overflow: 'hidden'
          }}
        >
          {!selectedConversation ? (
            <Box sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flex: 1,
              flexDirection: 'column',
              gap: 2
            }}>
              <ChatBubbleOutlineIcon sx={{ fontSize: 100, color: 'text.disabled' }} />
              <Typography variant="h6" color="text.secondary">
                {t('selectConversation', 'Izaberi razgovor da počneš sa dopisivanjem')}
              </Typography>
            </Box>
          ) : (
            <>
              {/* Chat Header */}
              <Box sx={{
                p: 2.5,
                borderBottom: 1,
                borderColor: 'divider',
                background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                color: 'white'
              }}>
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center' }}>
                    <Avatar
                      src={selectedConversation.otherUserProfilePicture || undefined}
                      sx={{
                        width: 48,
                        height: 48,
                        mr: 2,
                        border: '2px solid white',
                        boxShadow: 3
                      }}
                    >
                      {selectedConversation.otherUserName?.charAt(0).toUpperCase()}
                    </Avatar>
                    <Box>
                      <Typography variant="h6" fontWeight="600">
                        {selectedConversation.otherUserName}
                      </Typography>
                      {userTyping === selectedConversation.otherUserId && (
                        <Fade in>
                          <Typography variant="caption" sx={{ opacity: 0.9 }}>
                            {t('typing', 'kuca...')}
                          </Typography>
                        </Fade>
                      )}
                      {!userTyping && (
                        <Typography variant="caption" sx={{ opacity: 0.8 }}>
                          {connected ? t('online', 'Aktivan') : t('offline', 'Offline')}
                        </Typography>
                      )}
                    </Box>
                  </Box>
                  <IconButton sx={{ color: 'white' }}>
                    <MoreVertIcon />
                  </IconButton>
                </Box>
              </Box>

              {/* Messages List */}
              <Box sx={{
                flex: 1,
                overflow: 'auto',
                p: 3,
                background: 'linear-gradient(to bottom, #f8f9fa 0%, #e9ecef 100%)'
              }}>
                {messages.length === 0 && (
                  <Box sx={{ textAlign: 'center', py: 8 }}>
                    <ChatBubbleOutlineIcon sx={{ fontSize: 80, color: 'text.disabled', mb: 2 }} />
                    <Typography variant="h6" color="text.secondary" gutterBottom>
                      {t('startChatting', 'Pozdrav! 👋')}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {t('sendFirstMessage', 'Pošalji prvu poruku da započneš razgovor')}
                    </Typography>
                  </Box>
                )}
                {messages.map((msg, index) => {
                  const isOwn = msg.senderId === currentUserId;
                  const showAvatar = index === 0 || messages[index - 1].senderId !== msg.senderId;

                  return (
                    <Zoom in key={msg.messageId} style={{ transitionDelay: `${index * 50}ms` }}>
                      <Box
                        sx={{
                          display: 'flex',
                          justifyContent: isOwn ? 'flex-end' : 'flex-start',
                          mb: 1.5,
                          alignItems: 'flex-end'
                        }}
                      >
                        {!isOwn && showAvatar && (
                          <Avatar
                            src={selectedConversation.otherUserProfilePicture || undefined}
                            sx={{ width: 32, height: 32, mr: 1, mb: 0.5 }}
                          >
                            {selectedConversation.otherUserName?.charAt(0)}
                          </Avatar>
                        )}
                        {!isOwn && !showAvatar && <Box sx={{ width: 40 }} />}

                        <Box
                          sx={{
                            maxWidth: '65%',
                            position: 'relative'
                          }}
                        >
                          <Paper
                            elevation={isOwn ? 3 : 1}
                            sx={{
                              p: 1.5,
                              borderRadius: isOwn ? '18px 18px 4px 18px' : '18px 18px 18px 4px',
                              background: isOwn
                                ? 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)'
                                : 'white',
                              color: isOwn ? 'white' : 'text.primary',
                              wordWrap: 'break-word'
                            }}
                          >
                            <Typography variant="body1" sx={{ lineHeight: 1.5 }}>
                              {msg.messageText}
                            </Typography>
                            <Box sx={{
                              display: 'flex',
                              alignItems: 'center',
                              gap: 0.5,
                              mt: 0.5,
                              justifyContent: 'flex-end'
                            }}>
                              <Typography
                                variant="caption"
                                sx={{
                                  opacity: isOwn ? 0.8 : 0.6,
                                  fontSize: '0.7rem'
                                }}
                              >
                                {formatTime(msg.sentAt)}
                              </Typography>
                              {isOwn && (
                                msg.isRead ? (
                                  <DoneAllIcon sx={{ fontSize: 14, opacity: 0.8 }} />
                                ) : (
                                  <DoneIcon sx={{ fontSize: 14, opacity: 0.8 }} />
                                )
                              )}
                            </Box>
                          </Paper>
                        </Box>
                      </Box>
                    </Zoom>
                  );
                })}
                <div ref={messagesEndRef} />
              </Box>

              {/* Input Area */}
              <Box sx={{
                p: 2.5,
                borderTop: 1,
                borderColor: 'divider',
                bgcolor: 'background.paper'
              }}>
                <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'flex-end' }}>
                  <TextField
                    fullWidth
                    multiline
                    maxRows={4}
                    variant="outlined"
                    placeholder={t('typeMessage', 'Napiši poruku...')}
                    value={messageText}
                    onChange={(e) => setMessageText(e.target.value)}
                    onKeyPress={(e) => {
                      if (e.key === 'Enter' && !e.shiftKey) {
                        e.preventDefault();
                        handleSendMessage();
                      }
                    }}
                    sx={{
                      '& .MuiOutlinedInput-root': {
                        borderRadius: 3,
                        bgcolor: 'background.default',
                        '&:hover fieldset': {
                          borderColor: 'primary.main',
                        }
                      }
                    }}
                  />
                  <IconButton
                    color="primary"
                    onClick={handleSendMessage}
                    disabled={!messageText.trim()}
                    sx={{
                      background: messageText.trim()
                        ? 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)'
                        : 'transparent',
                      color: messageText.trim() ? 'white' : 'action.disabled',
                      width: 48,
                      height: 48,
                      transition: 'all 0.3s',
                      '&:hover': {
                        background: messageText.trim()
                          ? 'linear-gradient(135deg, #764ba2 0%, #667eea 100%)'
                          : 'transparent',
                        transform: messageText.trim() ? 'scale(1.1)' : 'none'
                      },
                      '&:disabled': {
                        background: 'transparent'
                      }
                    }}
                  >
                    <SendIcon />
                  </IconButton>
                </Box>
                {!connected && (
                  <Chip
                    label={t('offline', 'Offline - poruke će biti poslate kad se povežeš')}
                    color="warning"
                    size="small"
                    sx={{ mt: 1 }}
                  />
                )}
              </Box>
            </>
          )}
        </Paper>
      </Box>
    </Container>
  );
};

export default ChatPage;
