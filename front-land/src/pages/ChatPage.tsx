import React, { useState, useEffect, useRef, useMemo } from 'react';
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
  Chip,
  InputAdornment,
  Fade,
  alpha,
  useTheme
} from '@mui/material';
import {
  Send as SendIcon,
  Search as SearchIcon,
  MoreVert as MoreVertIcon,
  DoneAll as DoneAllIcon,
  Done as DoneIcon,
  ChatBubbleOutline as ChatBubbleOutlineIcon,
  Circle as CircleIcon
} from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { messagesApi, ConversationDto, MessageDto } from '../api/messagesApi';
import { useChatSignalR } from '../hooks/useChatSignalR';
import { useAuth } from '../shared/context/AuthContext';
import { useNotifications } from '../shared/context/NotificationContext';
import axios from 'axios';

const chatColors = {
  primary: '#0891b2',
  primaryLight: '#22d3ee',
  primaryDark: '#0e7490',
  accent: '#14b8a6',
  accentLight: '#5eead4',
  bgLight: '#f0fdfa',
  bgMedium: '#ccfbf1',
  headerBg: '#0f766e',
  headerBgLight: '#14b8a6',
  messageSent: '#0891b2', // Moje poruke - teal
  messageSentLight: '#06b6d4',
  messageReceived: '#f3f4f6', // Primljene poruke - svetlo siva (light mode)
  messageReceivedDark: '#4b5563', // Svetlija siva za dark mode (vidljivija)
  online: '#10b981',
  textOnPrimary: '#ffffff', // Beli tekst na teal pozadini
  textOnReceived: '#111827', // Tamno sivi tekst na svetloj pozadini (light mode)
  textOnReceivedDark: '#f9fafb', // Svetlo sivi tekst na tamnoj pozadini (dark mode)
  textMuted: '#64748b',
  borderColor: '#e2e8f0',
  hoverBg: '#f1f5f9',
  selectedBg: '#e0f2fe',
  selectedBorder: '#0891b2',
};

const ChatPage: React.FC = () => {
  const { t } = useTranslation('chat');
  const { user } = useAuth();
  const theme = useTheme();
  const [conversations, setConversations] = useState<ConversationDto[]>([]);
  const [selectedConversation, setSelectedConversation] = useState<ConversationDto | null>(null);
  const [messages, setMessages] = useState<MessageDto[]>([]);
  const [messageText, setMessageText] = useState('');
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const scrollTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [searchParams] = useSearchParams();
  const targetUserIdParam = searchParams.get('userId');

  const currentUserId = user?.userId || 1;
  const { connected, sendMessage, markAsRead, newMessage, userTyping } = useChatSignalR(currentUserId);
  const { refreshUnreadMessagesCount } = useNotifications();

  useEffect(() => {
    loadConversations();
    // Učitaj broj nepročitanih poruka kada se otvori Messages stranica
    refreshUnreadMessagesCount();
  }, [currentUserId]);

  useEffect(() => {
    if (!loading && targetUserIdParam) {
      const targetId = Number(targetUserIdParam);

      if (!isNaN(targetId)) {
        // Ako konverzacije nisu učitane, učitaj ih prvo
        if (conversations.length === 0) {
          messagesApi.getUserConversations(currentUserId).then((conversationsData) => {
            setConversations(conversationsData);
            const existing = conversationsData.find(c => c.otherUserId === targetId);
            if (existing) {
              setSelectedConversation(existing);
              loadMessages(existing);
            } else {
              loadUserInfoAndCreateConversation(targetId);
            }
          });
        } else {
          const existing = conversations.find(c => c.otherUserId === targetId);
          if (existing) {
            setSelectedConversation(existing);
            loadMessages(existing);
          } else {
            loadUserInfoAndCreateConversation(targetId);
          }
        }
      }
    }
  }, [targetUserIdParam, conversations, loading, currentUserId]);

  useEffect(() => {
    if (newMessage) {
      // Ažuriraj listu konverzacija kada stigne nova poruka
      loadConversations();
      
      if (selectedConversation &&
        (newMessage.senderId === selectedConversation.otherUserId ||
          newMessage.receiverId === selectedConversation.otherUserId)) {
        setMessages(prev => {
          // Proveri da li poruka već postoji (izbegni duplikate)
          const exists = prev.some(m => m.messageId === newMessage.messageId);
          if (exists) return prev;
          return [...prev, newMessage];
        });

        if (newMessage.receiverId === currentUserId) {
          markAsRead(newMessage.messageId);
          // Ažuriraj broj nepročitanih poruka
          refreshUnreadMessagesCount();
        }
      }
    }
  }, [newMessage, selectedConversation, currentUserId, markAsRead]);

  useEffect(() => {
    // Debounce scroll - ne skroluj odmah, sačekaj malo
    if (scrollTimeoutRef.current) {
      clearTimeout(scrollTimeoutRef.current);
    }
    
    scrollTimeoutRef.current = setTimeout(() => {
      scrollToBottom();
    }, 100);

    return () => {
      if (scrollTimeoutRef.current) {
        clearTimeout(scrollTimeoutRef.current);
      }
    };
  }, [messages]);

  const scrollToBottom = () => {
    // Koristi 'auto' umesto 'smooth' za brže skrolovanje bez animacije
    messagesEndRef.current?.scrollIntoView({ behavior: 'auto' });
  };

  const loadUserInfoAndCreateConversation = async (userId: number) => {
    try {
      // Prvo učitaj konverzacije da dobiješ pravo ime korisnika
      const conversationsData = await messagesApi.getUserConversations(currentUserId);
      setConversations(conversationsData);
      
      // Proveri da li konverzacija već postoji u listi
      const existing = conversationsData.find(c => c.otherUserId === userId);
      if (existing) {
        setSelectedConversation(existing);
        await loadMessages(existing);
        return;
      }

      // Ako ne postoji, pokušaj da dohvatiš korisnika direktno
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
      // Učitaj poruke za novu konverzaciju
      try {
        const messagesData = await messagesApi.getConversation(currentUserId, userId);
        setMessages(messagesData.messages);
      } catch (error) {
        setMessages([]);
      }
    } catch (error) {
      // Ako ne može da se dohvati korisnik, učitaj konverzacije ponovo
      try {
        const conversationsData = await messagesApi.getUserConversations(currentUserId);
        setConversations(conversationsData);
        const existing = conversationsData.find(c => c.otherUserId === userId);
        if (existing) {
          setSelectedConversation(existing);
          await loadMessages(existing);
        } else {
          // Fallback - koristi userId kao ime samo ako stvarno ne postoji
          const newConv: ConversationDto = {
            otherUserId: userId,
            otherUserName: `User ${userId}`,
            unreadCount: 0
          };
          setSelectedConversation(newConv);
          // Pokušaj da učitam poruke čak i sa fallback imenom
          try {
            const messagesData = await messagesApi.getConversation(currentUserId, userId);
            setMessages(messagesData.messages);
          } catch (error) {
            setMessages([]);
          }
        }
      } catch (loadError) {
        // Apsolutni fallback
        const newConv: ConversationDto = {
          otherUserId: userId,
          otherUserName: `User ${userId}`,
          unreadCount: 0
        };
        setSelectedConversation(newConv);
        // Pokušaj da učitam poruke čak i sa fallback imenom
        try {
          const messagesData = await messagesApi.getConversation(currentUserId, userId);
          setMessages(messagesData.messages);
        } catch (error) {
          setMessages([]);
        }
      }
    }
  };

  const loadConversations = async () => {
    try {
      const data = await messagesApi.getUserConversations(currentUserId);
      setConversations(data);
      setLoading(false);
      // Ažuriraj broj nepročitanih poruka
      await refreshUnreadMessagesCount();
    } catch (error) {
      setLoading(false);
    }
  };

  const loadMessages = async (conv: ConversationDto) => {
    try {
      const data = await messagesApi.getConversation(currentUserId, conv.otherUserId);
      setMessages(data.messages);
      setSelectedConversation(conv);
      
      // Označi sve nepročitane poruke kao pročitane kada se otvori konverzacija
      const unreadMessages = data.messages.filter(m => m.receiverId === currentUserId && !m.isRead);
      for (const msg of unreadMessages) {
        try {
          await messagesApi.markAsRead(msg.messageId);
        } catch (error) {
          // Ignoriši greške
        }
      }
      
      // Ažuriraj broj nepročitanih poruka nakon označavanja poruka kao pročitanih
      await refreshUnreadMessagesCount();
      
      // Ažuriraj listu konverzacija bez pozivanja loadConversations (da izbegnemo circular dependency)
      try {
        const conversationsData = await messagesApi.getUserConversations(currentUserId);
        setConversations(conversationsData);
      } catch (error) {
        // Ignoriši greške
      }
    } catch (error) {
    }
  };

  const handleSendMessage = async () => {
    if (!messageText.trim() || !selectedConversation) return;

    const messageToSend = messageText.trim();

    try {
      if (connected) {
        await sendMessage(currentUserId, selectedConversation.otherUserId, messageToSend);
      } else {
        const newMsg = await messagesApi.sendMessage({
          senderId: currentUserId,
          receiverId: selectedConversation.otherUserId,
          messageText: messageToSend
        });
        setMessages(prev => [...prev, newMsg]);
      }

      setMessageText('');
      
      // Ažuriraj lokalno stanje konverzacije umesto pozivanja loadConversations
      setConversations(prev => prev.map(conv => 
        conv.otherUserId === selectedConversation.otherUserId
          ? { 
              ...conv, 
              lastMessage: { 
                messageId: 0,
                senderId: currentUserId,
                receiverId: selectedConversation.otherUserId,
                messageText: messageToSend,
                sentAt: new Date().toISOString(),
                isRead: true,
              }
            }
          : conv
      ));
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

  const filteredConversations = useMemo(() => 
    conversations.filter(conv =>
      conv.otherUserName?.toLowerCase().includes(searchQuery.toLowerCase())
    ),
    [conversations, searchQuery]
  );

  if (loading) {
    return (
      <Container maxWidth="xl" sx={{ py: 4, display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '80vh' }}>
        <CircularProgress size={60} sx={{ color: chatColors.primary }} />
      </Container>
    );
  }

  return (
    <Container maxWidth="xl" sx={{ py: 3, height: 'calc(100vh - 80px)' }}>
      <Box sx={{
        display: 'flex',
        height: '100%',
        gap: 2,
        bgcolor: theme.palette.mode === 'dark' ? 'background.default' : '#f8fafc',
        borderRadius: 4,
        overflow: 'hidden',
        boxShadow: '0 4px 24px rgba(0, 0, 0, 0.08)',
      }}>
        {/* Conversations Sidebar */}
        <Paper
          elevation={0}
          sx={{
            width: 380,
            display: 'flex',
            flexDirection: 'column',
            borderRadius: 0,
            borderRight: `1px solid ${chatColors.borderColor}`,
            bgcolor: theme.palette.mode === 'dark' ? 'background.paper' : '#ffffff',
          }}
        >
          {/* Sidebar Header */}
          <Box sx={{
            p: 3,
            borderBottom: `1px solid ${chatColors.borderColor}`,
          }}>
            <Typography 
              variant="h5" 
              fontWeight="700" 
              sx={{ 
                color: chatColors.headerBg,
                mb: 2,
                letterSpacing: '-0.5px'
              }}
            >
              {t('title', 'Poruke')}
            </Typography>
            <TextField
              fullWidth
              size="small"
              placeholder={t('searchConversations', 'Pretraži razgovore...')}
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              sx={{
                '& .MuiOutlinedInput-root': {
                  bgcolor: theme.palette.mode === 'dark' ? 'background.default' : '#f1f5f9',
                  borderRadius: 3,
                  border: 'none',
                  '& fieldset': { 
                    border: 'none',
                  },
                  '&:hover': {
                    bgcolor: theme.palette.mode === 'dark' ? 'action.hover' : '#e2e8f0',
                  },
                  '&.Mui-focused': {
                    bgcolor: theme.palette.mode === 'dark' ? 'background.default' : '#ffffff',
                    boxShadow: `0 0 0 2px ${chatColors.primary}`,
                  }
                },
              }}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon sx={{ color: chatColors.textMuted }} />
                  </InputAdornment>
                )
              }}
            />
          </Box>

          {/* Conversations List */}
          <List sx={{ flex: 1, overflow: 'auto', p: 1.5 }}>
            {filteredConversations.length === 0 ? (
              <Box sx={{ textAlign: 'center', py: 8 }}>
                <Box sx={{ 
                  width: 80, 
                  height: 80, 
                  borderRadius: '50%', 
                  bgcolor: chatColors.bgLight,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  mx: 'auto',
                  mb: 2
                }}>
                  <ChatBubbleOutlineIcon sx={{ fontSize: 40, color: chatColors.primary }} />
                </Box>
                <Typography color="text.secondary" fontWeight={500}>
                  {searchQuery ? t('noResults', 'Nema rezultata') : t('noConversations', 'Nema razgovora')}
                </Typography>
              </Box>
            ) : (
              filteredConversations.map((conv) => (
                <ListItemButton
                  key={conv.otherUserId}
                  selected={selectedConversation?.otherUserId === conv.otherUserId}
                  onClick={() => loadMessages(conv)}
                  sx={{
                    borderRadius: 3,
                    mb: 1,
                    py: 1.5,
                    transition: 'all 0.2s ease',
                    '&.Mui-selected': {
                      bgcolor: chatColors.selectedBg,
                      borderLeft: `3px solid ${chatColors.selectedBorder}`,
                      '&:hover': {
                        bgcolor: alpha(chatColors.selectedBg, 0.8),
                      }
                    },
                    '&:hover': {
                      bgcolor: chatColors.hoverBg,
                    }
                  }}
                >
                    <ListItemAvatar>
                      <Badge
                        badgeContent={conv.unreadCount}
                        color="error"
                        overlap="circular"
                        sx={{
                          '& .MuiBadge-badge': {
                            bgcolor: '#ef4444',
                            fontWeight: 600,
                          }
                        }}
                      >
                        <Avatar
                          src={conv.otherUserProfilePicture || undefined}
                          sx={{
                            width: 52,
                            height: 52,
                            bgcolor: chatColors.primary,
                            fontSize: '1.2rem',
                            fontWeight: 600,
                          }}
                        >
                          {conv.otherUserName?.charAt(0).toUpperCase()}
                        </Avatar>
                      </Badge>
                    </ListItemAvatar>
                    <ListItemText
                      primary={
                        <Typography 
                          variant="subtitle1" 
                          fontWeight={conv.unreadCount > 0 ? 700 : 500}
                          sx={{ color: 'text.primary' }}
                        >
                          {conv.otherUserName}
                        </Typography>
                      }
                      secondary={
                        <Typography
                          variant="body2"
                          sx={{
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            whiteSpace: 'nowrap',
                            fontWeight: conv.unreadCount > 0 ? 500 : 400,
                            color: conv.unreadCount > 0 ? 'text.primary' : 'text.secondary',
                          }}
                        >
                          {conv.lastMessage?.messageText || t('startConversation', 'Započni razgovor')}
                        </Typography>
                      }
                      sx={{ ml: 1.5 }}
                    />
                    {conv.lastMessage && (
                      <Typography 
                        variant="caption" 
                        sx={{ 
                          alignSelf: 'flex-start', 
                          mt: 0.5,
                          color: chatColors.textMuted,
                          fontWeight: 500
                        }}
                      >
                        {formatTime(conv.lastMessage.sentAt)}
                      </Typography>
                    )}
                  </ListItemButton>
              ))
            )}
          </List>
        </Paper>

        {/* Chat Area */}
        <Box
          sx={{
            flex: 1,
            display: 'flex',
            flexDirection: 'column',
            bgcolor: theme.palette.mode === 'dark' ? 'background.paper' : '#ffffff',
          }}
        >
          {!selectedConversation ? (
            <Box sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flex: 1,
              flexDirection: 'column',
              gap: 3,
              bgcolor: theme.palette.mode === 'dark' ? 'background.default' : chatColors.bgLight,
            }}>
              <Box sx={{ 
                width: 120, 
                height: 120, 
                borderRadius: '50%', 
                bgcolor: theme.palette.mode === 'dark' ? 'action.hover' : chatColors.bgMedium,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}>
                <ChatBubbleOutlineIcon sx={{ fontSize: 56, color: chatColors.primary }} />
              </Box>
              <Box sx={{ textAlign: 'center' }}>
                <Typography variant="h5" fontWeight={600} color="text.primary" gutterBottom>
                  {t('selectConversation', 'Izaberi razgovor')}
                </Typography>
                <Typography variant="body1" color="text.secondary">
                  {t('startChattingHint', 'Izaberi konverzaciju sa leve strane da počneš sa dopisivanjem')}
                </Typography>
              </Box>
            </Box>
          ) : (
            <>
              {/* Chat Header */}
              <Box sx={{
                px: 3,
                py: 2,
                borderBottom: `1px solid ${chatColors.borderColor}`,
                bgcolor: theme.palette.mode === 'dark' ? 'background.paper' : '#ffffff',
              }}>
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center' }}>
                    <Box sx={{ position: 'relative' }}>
                      <Avatar
                        src={selectedConversation.otherUserProfilePicture || undefined}
                        sx={{
                          width: 48,
                          height: 48,
                          bgcolor: chatColors.primary,
                          fontSize: '1.2rem',
                          fontWeight: 600,
                        }}
                      >
                        {selectedConversation.otherUserName?.charAt(0).toUpperCase()}
                      </Avatar>
                      {connected && (
                        <CircleIcon 
                          sx={{ 
                            position: 'absolute',
                            bottom: 2,
                            right: 2,
                            fontSize: 14,
                            color: chatColors.online,
                            bgcolor: 'background.paper',
                            borderRadius: '50%',
                          }} 
                        />
                      )}
                    </Box>
                    <Box sx={{ ml: 2 }}>
                      <Typography variant="h6" fontWeight="600" color="text.primary">
                        {selectedConversation.otherUserName}
                      </Typography>
                      {userTyping === selectedConversation.otherUserId ? (
                        <Fade in>
                          <Typography variant="caption" sx={{ color: chatColors.primary, fontWeight: 500 }}>
                            {t('typing', 'kuca...')}
                          </Typography>
                        </Fade>
                      ) : (
                        <Typography variant="caption" sx={{ color: connected ? chatColors.online : chatColors.textMuted }}>
                          {connected ? t('online', 'Aktivan') : t('offline', 'Offline')}
                        </Typography>
                      )}
                    </Box>
                  </Box>
                  <IconButton sx={{ color: chatColors.textMuted }}>
                    <MoreVertIcon />
                  </IconButton>
                </Box>
              </Box>

              {/* Messages List */}
              <Box sx={{
                flex: 1,
                overflow: 'auto',
                p: 3,
                bgcolor: theme.palette.mode === 'dark' ? 'background.default' : '#f8fafc',
              }}>
                {messages.length === 0 && (
                  <Box sx={{ textAlign: 'center', py: 8 }}>
                    <Box sx={{ 
                      width: 100, 
                      height: 100, 
                      borderRadius: '50%', 
                      bgcolor: theme.palette.mode === 'dark' ? 'action.hover' : chatColors.bgMedium,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      mx: 'auto',
                      mb: 3
                    }}>
                      <Typography variant="h2" sx={{ lineHeight: 1 }}>👋</Typography>
                    </Box>
                    <Typography variant="h6" fontWeight={600} color="text.primary" gutterBottom>
                      {t('startChatting', 'Pozdrav!')}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {t('sendFirstMessage', 'Pošalji prvu poruku da započneš razgovor')}
                    </Typography>
                  </Box>
                )}
                {messages.map((msg, index) => {
                  const isOwn = msg.senderId === currentUserId;
                  const showAvatar = index === 0 || messages[index - 1].senderId !== msg.senderId;
                  const isLastInGroup = index === messages.length - 1 || messages[index + 1].senderId !== msg.senderId;

                  return (
                    <Box
                      key={msg.messageId}
                      sx={{
                        display: 'flex',
                        justifyContent: isOwn ? 'flex-start' : 'flex-end', // Obrnuto: moje levo, primljene desno
                        mb: isLastInGroup ? 2 : 0.5,
                        alignItems: 'flex-end'
                      }}
                    >
                      {/* Avatar za moje poruke (levo) */}
                      {isOwn && showAvatar && (
                        <Avatar
                          src={user?.profilePicture || undefined}
                          sx={{ 
                            width: 32, 
                            height: 32, 
                            mr: 1, 
                            mb: 0.5,
                            bgcolor: chatColors.primary,
                            fontSize: '0.875rem',
                          }}
                        >
                          {user?.firstName?.charAt(0) || 'U'}
                        </Avatar>
                      )}
                      {isOwn && !showAvatar && <Box sx={{ width: 40 }} />}

                      <Box
                        sx={{
                          maxWidth: '70%',
                          position: 'relative'
                        }}
                      >
                        <Box
                          sx={{
                            px: 2,
                            py: 1.25,
                            borderRadius: isOwn 
                              ? isLastInGroup ? '20px 20px 20px 4px' : '20px 20px 20px 20px' // Moje poruke: zaobljeni levi ugao
                              : isLastInGroup ? '20px 20px 4px 20px' : '20px 20px 20px 20px', // Primljene poruke: zaobljeni desni ugao
                            bgcolor: isOwn 
                              ? chatColors.messageSent 
                              : (theme.palette.mode === 'dark' ? chatColors.messageReceivedDark : chatColors.messageReceived),
                            color: isOwn 
                              ? chatColors.textOnPrimary 
                              : (theme.palette.mode === 'dark' ? chatColors.textOnReceivedDark : chatColors.textOnReceived), // Svetlo sivi tekst u dark mode, tamno sivi u light mode
                            boxShadow: isOwn 
                              ? '0 1px 2px rgba(0, 0, 0, 0.1)' 
                              : '0 1px 2px rgba(0, 0, 0, 0.08)',
                            border: isOwn ? 'none' : `1px solid ${theme.palette.mode === 'dark' ? '#4b5563' : chatColors.borderColor}`,
                          }}
                        >
                          <Typography 
                            variant="body1" 
                            sx={{ 
                              lineHeight: 1.5,
                              wordBreak: 'break-word',
                              color: 'inherit',
                              fontWeight: 400,
                            }}
                          >
                            {msg.messageText}
                          </Typography>
                        </Box>
                        {isLastInGroup && (
                          <Box sx={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: 0.5,
                            mt: 0.5,
                            px: 0.5,
                            justifyContent: isOwn ? 'flex-start' : 'flex-end' // Obrnuto poravnanje
                          }}>
                            <Typography
                              variant="caption"
                              sx={{
                                color: chatColors.textMuted,
                                fontSize: '0.7rem'
                              }}
                            >
                              {formatTime(msg.sentAt)}
                            </Typography>
                            {isOwn && (
                              msg.isRead ? (
                                <DoneAllIcon sx={{ fontSize: 14, color: chatColors.primary }} />
                              ) : (
                                <DoneIcon sx={{ fontSize: 14, color: chatColors.textMuted }} />
                              )
                            )}
                          </Box>
                        )}
                      </Box>

                      {/* Avatar za primljene poruke (desno) */}
                      {!isOwn && showAvatar && (
                        <Avatar
                          src={selectedConversation.otherUserProfilePicture || undefined}
                          sx={{ 
                            width: 32, 
                            height: 32, 
                            ml: 1, 
                            mb: 0.5,
                            bgcolor: chatColors.accent,
                            fontSize: '0.875rem',
                          }}
                        >
                          {selectedConversation.otherUserName?.charAt(0)}
                        </Avatar>
                      )}
                      {!isOwn && !showAvatar && <Box sx={{ width: 40 }} />}
                    </Box>
                  );
                })}
                <div ref={messagesEndRef} />
              </Box>

              {/* Input Area */}
              <Box sx={{
                p: 2.5,
                borderTop: `1px solid ${chatColors.borderColor}`,
                bgcolor: theme.palette.mode === 'dark' ? 'background.paper' : '#ffffff',
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
                        bgcolor: theme.palette.mode === 'dark' ? 'background.default' : '#f1f5f9',
                        '& fieldset': {
                          borderColor: 'transparent',
                        },
                        '&:hover fieldset': {
                          borderColor: chatColors.primary,
                        },
                        '&.Mui-focused fieldset': {
                          borderColor: chatColors.primary,
                          borderWidth: 2,
                        }
                      }
                    }}
                  />
                  <IconButton
                    onClick={handleSendMessage}
                    disabled={!messageText.trim()}
                    sx={{
                      bgcolor: messageText.trim() ? chatColors.primary : 'transparent',
                      color: messageText.trim() ? chatColors.textOnPrimary : chatColors.textMuted,
                      width: 48,
                      height: 48,
                      transition: 'all 0.2s ease',
                      '&:hover': {
                        bgcolor: messageText.trim() ? chatColors.primaryDark : 'transparent',
                        transform: messageText.trim() ? 'scale(1.05)' : 'none'
                      },
                      '&:disabled': {
                        bgcolor: 'transparent',
                        color: chatColors.textMuted,
                      }
                    }}
                  >
                    <SendIcon />
                  </IconButton>
                </Box>
                {!connected && (
                  <Chip
                    label={t('offline', 'Offline - poruke će biti poslate kad se povežeš')}
                    size="small"
                    sx={{ 
                      mt: 1.5,
                      bgcolor: '#fef3c7',
                      color: '#92400e',
                      fontWeight: 500,
                    }}
                  />
                )}
              </Box>
            </>
          )}
        </Box>
      </Box>
    </Container>
  );
};

export default ChatPage;
