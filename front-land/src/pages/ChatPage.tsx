import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
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
  Button,
  Tooltip,
  alpha,
  useTheme
} from '@mui/material';
// One import per icon: the '@mui/icons-material' index pulls in thousands of modules.
import SendIcon from '@mui/icons-material/Send';
import SearchIcon from '@mui/icons-material/Search';
import MoreVertIcon from '@mui/icons-material/MoreVert';
import ScrollDownIcon from '@mui/icons-material/KeyboardArrowDown';
import ChatBubbleOutlineIcon from '@mui/icons-material/ChatBubbleOutline';
import CircleIcon from '@mui/icons-material/Circle';
import GroupAddIcon from '@mui/icons-material/GroupAdd';
import GroupsIcon from '@mui/icons-material/Groups';
import { useTranslation } from 'react-i18next';
import { getApiErrorMessage } from '../shared/utils/apiError';
import { messagesApi, ConversationDto, MessageDto } from '../api/messagesApi';
import { useChatSignalR } from '../hooks/useChatSignalR';
import ChatMessageList from '../components/Chat/ChatMessageList';
import GroupChatPanel from '../components/Chat/GroupChatPanel';
import { ChatGroup, ChatGroupMessage, chatGroupsApi, groupTitle } from '../shared/api/chatGroups';
import { useMyPairing } from '../hooks/useMyPairing';
import { applyLiveMessage, buildTimeline, markMessagesRead, parseServerDate } from '../components/Chat/chatMessageUtils';
import { useAuth } from '../shared/context/AuthContext';
import { useNotifications } from '../shared/context/NotificationContext';
import { analyticsApi } from '../shared/api/analytics';
import apiClient from '../shared/api/client';

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
  const { t, i18n } = useTranslation('chat');
  const { user } = useAuth();
  const theme = useTheme();
  const isDark = theme.palette.mode === 'dark';
  const [conversations, setConversations] = useState<ConversationDto[]>([]);
  const [selectedConversation, setSelectedConversation] = useState<ConversationDto | null>(null);
  const [messages, setMessages] = useState<MessageDto[]>([]);
  const [messageText, setMessageText] = useState('');
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  // Shared conversations of two roommates with a landlord.
  const [groups, setGroups] = useState<ChatGroup[]>([]);
  const groupsRef = useRef<ChatGroup[]>([]);
  const [selectedGroupId, setSelectedGroupId] = useState<number | null>(null);
  const selectedGroupIdRef = useRef<number | null>(null);
  const [openingGroup, setOpeningGroup] = useState(false);
  // Scrolling behaves like WhatsApp: stay glued to the bottom while you are there, and when you have
  // scrolled up to read, do not yank the view down - offer a button with the number of new messages.
  const scrollContainerRef = useRef<HTMLDivElement>(null);
  const stickToBottomRef = useRef(true);
  const openingConversationRef = useRef(false);
  const lastMessageIdRef = useRef<number | null>(null);
  const [showScrollDown, setShowScrollDown] = useState(false);
  const [newBelowCount, setNewBelowCount] = useState(0);
  // First message the user had not read when opening the conversation: the "N unread" divider goes above it.
  const [firstUnreadMessageId, setFirstUnreadMessageId] = useState<number | null>(null);
  const [searchParams] = useSearchParams();
  const targetUserIdParam = searchParams.get('userId');
  const targetGroupParam = searchParams.get('group');
  const openedGroupParamRef = useRef<string | null>(null);
  const apartmentTitleParam = searchParams.get('apartmentTitle');
  const prefilledForApartmentRef = useRef(false);

  const currentUserId = user!.userId;
  const { connected, sendMessage, markAsRead, newMessage, userTyping, readMessageIds, newGroupMessage, groupsChanged } = useChatSignalR(currentUserId);
  const { partner } = useMyPairing();
  const { refreshUnreadMessagesCount, addNotification } = useNotifications();

  useEffect(() => {
    loadConversations();
    loadGroups();
    // Učitaj broj nepročitanih poruka kada se otvori Messages stranica
    refreshUnreadMessagesCount();
  }, [currentUserId]);

  // Somebody added us to a shared conversation: show it without a reload.
  useEffect(() => {
    if (groupsChanged > 0) loadGroups();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [groupsChanged]);

  useEffect(() => {
    groupsRef.current = groups;
  }, [groups]);

  // A message arrived in a shared conversation: the sidebar preview and badge follow, without
  // asking for the whole list again (only a conversation we do not know yet needs that).
  useEffect(() => {
    if (!newGroupMessage) return;
    const message = newGroupMessage;
    if (!groupsRef.current.some(g => g.groupId === message.groupId)) {
      loadGroups();
      return;
    }
    setGroups(prev => {
      const target = prev.find(g => g.groupId === message.groupId);
      if (!target || target.lastMessage?.messageId === message.messageId) return prev;
      const unread = message.senderId !== currentUserId && message.groupId !== selectedGroupIdRef.current;
      const updated = { ...target, lastMessage: message, unreadCount: target.unreadCount + (unread ? 1 : 0) };
      return [updated, ...prev.filter(g => g.groupId !== message.groupId)];
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [newGroupMessage]);

  // A link to a shared conversation (from a notification) opens it, once the list has it.
  useEffect(() => {
    if (!targetGroupParam || openedGroupParamRef.current === targetGroupParam) return;
    const target = groups.find(g => g.groupId === Number(targetGroupParam));
    if (!target) return;
    openedGroupParamRef.current = targetGroupParam;
    openGroup(target);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [targetGroupParam, groups]);

  // Choosing a one-to-one conversation leaves the shared one.
  useEffect(() => {
    if (selectedConversation) setSelectedGroupId(null);
  }, [selectedConversation]);

  useEffect(() => {
    selectedGroupIdRef.current = selectedGroupId;
  }, [selectedGroupId]);

  useEffect(() => {
    if (loading || !targetUserIdParam) return;
    const targetId = Number(targetUserIdParam);
    if (isNaN(targetId)) return;

    // IMPORTANT: do NOT depend on `conversations` here. This effect calls
    // setConversations, so including `conversations` in the deps created an
    // infinite fetch → setState → re-run loop whenever the target user had no
    // existing conversation (e.g. messaging someone for the first time). That
    // loop fired hundreds of requests/minute and tripped the server rate limit
    // (429 on send/unread-count). Fetch fresh once per target/auth change instead.
    let cancelled = false;
    messagesApi.getUserConversations(currentUserId).then((conversationsData) => {
      if (cancelled) return;
      setConversations(conversationsData);
      const existing = conversationsData.find(c => c.otherUserId === targetId);
      if (existing) {
        setSelectedConversation(existing);
        loadMessages(existing);
      } else {
        loadUserInfoAndCreateConversation(targetId);
      }
    });
    return () => { cancelled = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [targetUserIdParam, loading, currentUserId]);

  useEffect(() => {
    if (newMessage) {
      // The list follows the message on its own; fetching the whole list again for every message
      // made the page slower the more conversations a user had.
      setConversations(prev =>
        applyLiveMessage(prev, newMessage, currentUserId, selectedConversation?.otherUserId ?? null));

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

  // Coming from an apartment page: type the first message for the user (they can edit it before sending).
  useEffect(() => {
    if (prefilledForApartmentRef.current || !apartmentTitleParam || !selectedConversation) return;
    if (selectedConversation.otherUserId !== Number(targetUserIdParam)) return;
    prefilledForApartmentRef.current = true;
    setMessageText(current => current || t('interestedInApartment', { title: apartmentTitleParam }));
  }, [apartmentTitleParam, targetUserIdParam, selectedConversation, t]);

  // A different conversation: start glued to the bottom and remember to jump to the unread divider.
  useEffect(() => {
    stickToBottomRef.current = true;
    openingConversationRef.current = true;
    lastMessageIdRef.current = null;
    setShowScrollDown(false);
    setNewBelowCount(0);
  }, [selectedConversation?.otherUserId]);

  const scrollToBottom = () => {
    const container = scrollContainerRef.current;
    if (container) container.scrollTop = container.scrollHeight;
  };

  useEffect(() => {
    const container = scrollContainerRef.current;
    if (!container) return;

    const last = messages[messages.length - 1];
    const isNewTail = last !== undefined && last.messageId !== lastMessageIdRef.current;
    lastMessageIdRef.current = last?.messageId ?? null;

    if (openingConversationRef.current && messages.length > 0) {
      openingConversationRef.current = false;
      // Jump to the first unread message if there is one, otherwise to the latest.
      const divider = container.querySelector('[data-unread-divider]');
      if (divider) divider.scrollIntoView({ block: 'center' });
      else scrollToBottom();
      return;
    }

    if (!isNewTail) return; // e.g. only a read receipt changed

    if (stickToBottomRef.current || last.senderId === currentUserId) {
      scrollToBottom();
      setNewBelowCount(0);
    } else {
      setNewBelowCount(count => count + 1);
    }
  }, [messages, currentUserId]);

  const handleMessagesScroll = () => {
    const container = scrollContainerRef.current;
    if (!container) return;
    const nearBottom = container.scrollHeight - container.scrollTop - container.clientHeight < 80;
    stickToBottomRef.current = nearBottom;
    setShowScrollDown(!nearBottom);
    if (nearBottom) setNewBelowCount(0);
  };

  // The other person read our messages: flip the ticks live (the server pushes "MessageRead").
  useEffect(() => {
    if (readMessageIds.length === 0) return;
    const read = new Set(readMessageIds);
    setMessages(prev => markMessagesRead(prev, read));
    setConversations(prev =>
      prev.map(conv =>
        conv.lastMessage && !conv.lastMessage.isRead && read.has(conv.lastMessage.messageId)
          ? { ...conv, lastMessage: { ...conv.lastMessage, isRead: true } }
          : conv,
      ),
    );
  }, [readMessageIds]);

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
      const response = await apiClient.get(`/api/v1/auth/profile/${userId}`);
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
            otherUserName: t('unknownUser', 'Korisnik'),
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
          otherUserName: t('unknownUser', 'Korisnik'),
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

  const loadGroups = async () => {
    try {
      const data = await chatGroupsApi.getMine();
      // The group that is open is being read right now, whatever the server counted a moment ago.
      setGroups(data.map(g => (g.groupId === selectedGroupIdRef.current ? { ...g, unreadCount: 0 } : g)));
    } catch {
      // One-to-one chats keep working without the shared ones.
    }
  };

  const openGroup = (group: ChatGroup) => {
    setSelectedConversation(null);
    setMessages([]);
    setSelectedGroupId(group.groupId);
  };

  const handleGroupRead = useCallback((groupId: number) => {
    setGroups(prev => prev.map(g => (g.groupId === groupId ? { ...g, unreadCount: 0 } : g)));
  }, []);

  const handleGroupMessage = useCallback((groupId: number, message: ChatGroupMessage) => {
    setGroups(prev => prev.map(g => (g.groupId === groupId ? { ...g, lastMessage: message } : g)));
  }, []);

  const handleGroupLeft = useCallback((groupId: number) => {
    setGroups(prev => prev.filter(g => g.groupId !== groupId));
    setSelectedGroupId(null);
  }, []);

  /** Puts my roommate into the conversation with this person (landlord); what was said so far comes along. */
  const startGroupWith = async (otherUserId: number) => {
    setOpeningGroup(true);
    try {
      const group = await chatGroupsApi.open(otherUserId);
      setGroups(prev => (prev.some(g => g.groupId === group.groupId) ? prev : [group, ...prev]));
      openGroup(group);
      addNotification({ title: t('groupOpened'), message: t('groupHistoryHint'), type: 'success' });
    } catch (error) {
      addNotification({ title: t('groupOpenFailed'), message: getApiErrorMessage(error, t), type: 'error' });
    } finally {
      setOpeningGroup(false);
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
      setFirstUnreadMessageId(
        data.messages.find(m => m.receiverId === currentUserId && !m.isRead)?.messageId ?? null,
      );
      setMessages(data.messages);
      setSelectedConversation(conv);

      // Opening a conversation reads it: one request for all of it (it used to be one per message).
      const hasUnread = conv.unreadCount > 0 || data.messages.some(m => m.receiverId === currentUserId && !m.isRead);
      if (hasUnread) {
        try {
          await messagesApi.markConversationRead(conv.otherUserId);
          setConversations(prev =>
            prev.map(c => (c.otherUserId === conv.otherUserId ? { ...c, unreadCount: 0 } : c)));
          await refreshUnreadMessagesCount();
        } catch {
          // Still unread on the server: the badge and the list keep showing it, which is the truth.
        }
      }
    } catch (error) {
      // Opening the conversation failed; the list stays as it was.
    }
  };

  const handleSendMessage = async () => {
    if (!messageText.trim() || !selectedConversation) return;

    const messageToSend = messageText.trim();

    try {
      // Prefer the live hub; if it is down or rejects the message, fall back to REST so the
      // message is not silently lost (the hook used to swallow the failure and report success).
      const sentViaHub = connected
        ? await sendMessage(selectedConversation.otherUserId, messageToSend)
        : false;
      if (!sentViaHub) {
        const newMsg = await messagesApi.sendMessage({
          receiverId: selectedConversation.otherUserId,
          messageText: messageToSend
        });
        setMessages(prev => (prev.some(m => m.messageId === newMsg.messageId) ? prev : [...prev, newMsg]));
        // Sent over the hub, the "MessageSent" event updates the list; without it, the reply does.
        setConversations(prev => applyLiveMessage(prev, newMsg, currentUserId, selectedConversation.otherUserId));
      }

      // Track message sent event
      await analyticsApi.trackEvent('MessageSent', 'Communication');

      setMessageText('');
    } catch (error) {
      // Send failed: leave the typed text in the input so the user can retry instead of losing it,
      // and tell the user why (too long, no tokens, blocked...).
      addNotification({ title: t('messageNotSent'), message: getApiErrorMessage(error, t), type: 'error' });
    }
  };

  const formatTime = (dateString: string) => {
    const date = parseServerDate(dateString);
    const now = new Date();
    const diffInHours = (now.getTime() - date.getTime()) / (1000 * 60 * 60);

    if (diffInHours < 24) {
      return date.toLocaleTimeString(i18n.language, { hour: '2-digit', minute: '2-digit' });
    } else if (diffInHours < 48) {
      return t('yesterday', 'Juče');
    } else {
      return date.toLocaleDateString(i18n.language, { day: 'numeric', month: 'short' });
    }
  };

  const conversationRowSx = {
                    borderRadius: 3,
                    mb: 1,
                    py: 1.5,
                    transition: 'all 0.2s ease',
                    // The selected row used the light-blue of the light theme in dark mode too, so the
                    // white name was unreadable (contrast 1.1:1). Dark mode gets a deep teal (9:1).
                    '&.Mui-selected': {
                      bgcolor: isDark ? '#164e63' : chatColors.selectedBg,
                      borderLeft: `3px solid ${isDark ? chatColors.primaryLight : chatColors.selectedBorder}`,
                      '&:hover': {
                        bgcolor: isDark ? '#155e75' : alpha(chatColors.selectedBg, 0.8),
                      }
                    },
                    '&:hover': {
                      bgcolor: isDark ? 'rgba(255,255,255,0.06)' : chatColors.hoverBg,
                    }
  };

  const filteredConversations = useMemo(() =>
    conversations.filter(conv =>
      conv.otherUserName?.toLowerCase().includes(searchQuery.toLowerCase())
    ),
    [conversations, searchQuery]
  );

  const selectedGroup = useMemo(
    () => groups.find(g => g.groupId === selectedGroupId) ?? null,
    [groups, selectedGroupId],
  );

  const filteredGroups = useMemo(() => {
    const q = searchQuery.toLowerCase();
    return groups.filter(g => groupTitle(g, currentUserId).toLowerCase().includes(q));
  }, [groups, searchQuery, currentUserId]);

  // "Add my roommate" on a conversation with someone who is not my roommate.
  const roommateGroupWithThisPerson = useMemo(() => {
    if (!partner || !selectedConversation) return undefined;
    return groups.find(g =>
      g.members.some(m => m.userId === selectedConversation.otherUserId)
      && g.members.some(m => m.userId === partner.partnerUserId));
  }, [groups, partner, selectedConversation]);
  const canAddRoommate = !!partner && !!selectedConversation
    && selectedConversation.otherUserId !== partner.partnerUserId;

  const timeline = useMemo(
    () => buildTimeline(messages, currentUserId, firstUnreadMessageId),
    [messages, currentUserId, firstUnreadMessageId],
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
            {filteredGroups.map(group => {
              const title = groupTitle(group, currentUserId);
              const last = group.lastMessage;
              return (
                <ListItemButton
                  key={`group-${group.groupId}`}
                  selected={selectedGroupId === group.groupId}
                  onClick={() => openGroup(group)}
                  sx={conversationRowSx}
                >
                  <ListItemAvatar>
                    <Badge badgeContent={group.unreadCount} color="error" overlap="circular"
                      sx={{ '& .MuiBadge-badge': { bgcolor: '#ef4444', fontWeight: 600 } }}>
                      <Avatar sx={{ width: 52, height: 52, bgcolor: chatColors.accent }}>
                        <GroupsIcon />
                      </Avatar>
                    </Badge>
                  </ListItemAvatar>
                  <ListItemText
                    sx={{ ml: 1.5 }}
                    primary={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="subtitle1" noWrap fontWeight={group.unreadCount > 0 ? 700 : 500} sx={{ color: 'text.primary' }}>
                          {title}
                        </Typography>
                        <Chip label={t('groupBadge')} size="small" sx={{ height: 18, fontSize: '0.65rem' }} />
                      </Box>
                    }
                    secondary={
                      <Typography
                        variant="body2"
                        sx={{
                          overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap',
                          fontWeight: group.unreadCount > 0 ? 500 : 400,
                          color: group.unreadCount > 0 ? 'text.primary' : 'text.secondary',
                        }}
                      >
                        {last ? `${last.senderId === currentUserId ? t('groupSenderYou') : (last.senderName ?? '').split(' ')[0]}: ${last.messageText}` : t('groupChat')}
                      </Typography>
                    }
                  />
                  {last && (
                    <Typography variant="caption" sx={{ alignSelf: 'flex-start', mt: 0.5, color: chatColors.textMuted, fontWeight: 500 }}>
                      {formatTime(last.sentAt)}
                    </Typography>
                  )}
                </ListItemButton>
              );
            })}
            {filteredConversations.length === 0 && filteredGroups.length === 0 ? (
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
                  sx={conversationRowSx}
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
          {selectedGroup ? (
            <GroupChatPanel
              group={selectedGroup}
              currentUserId={currentUserId}
              incoming={newGroupMessage}
              onRead={handleGroupRead}
              onMessage={handleGroupMessage}
              onLeft={handleGroupLeft}
            />
          ) : !selectedConversation ? (
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
                  {canAddRoommate && (
                    <Tooltip title={t('groupHistoryHint')}>
                      <span>
                        {/* Wide screens get the words, a phone gets the icon: the action is on both. */}
                        <Button
                          size="small"
                          variant="outlined"
                          startIcon={<GroupAddIcon />}
                          disabled={openingGroup}
                          onClick={() => (roommateGroupWithThisPerson
                            ? openGroup(roommateGroupWithThisPerson)
                            : startGroupWith(selectedConversation.otherUserId))}
                          sx={{ mr: 1, textTransform: 'none', display: { xs: 'none', sm: 'inline-flex' } }}
                        >
                          {roommateGroupWithThisPerson ? t('openGroupChat') : t('addRoommateToChat')}
                        </Button>
                        <IconButton
                          disabled={openingGroup}
                          aria-label={roommateGroupWithThisPerson ? t('openGroupChat') : t('addRoommateToChat')}
                          onClick={() => (roommateGroupWithThisPerson
                            ? openGroup(roommateGroupWithThisPerson)
                            : startGroupWith(selectedConversation.otherUserId))}
                          sx={{ mr: 0.5, color: chatColors.primary, display: { xs: 'inline-flex', sm: 'none' } }}
                        >
                          <GroupAddIcon />
                        </IconButton>
                      </span>
                    </Tooltip>
                  )}
                  <IconButton sx={{ color: chatColors.textMuted }}>
                    <MoreVertIcon />
                  </IconButton>
                </Box>
              </Box>

              {/* Messages List */}
              <Box sx={{ position: 'relative', flex: 1, minHeight: 0, display: 'flex' }}>
              <Box
                ref={scrollContainerRef}
                onScroll={handleMessagesScroll}
                sx={{
                  flex: 1,
                  overflow: 'auto',
                  px: { xs: 1, sm: 3 },
                  py: 2,
                  bgcolor: theme.palette.mode === 'dark' ? 'background.default' : '#f8fafc',
                }}
              >
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
                <ChatMessageList items={timeline} locale={i18n.language} />
              </Box>
              {showScrollDown && (
                <Badge
                  badgeContent={newBelowCount}
                  color="error"
                  sx={{ position: 'absolute', right: 20, bottom: 14 }}
                >
                  <IconButton
                    onClick={scrollToBottom}
                    aria-label={t('scrollToLatest')}
                    sx={{
                      width: 40,
                      height: 40,
                      bgcolor: isDark ? '#374151' : '#ffffff',
                      color: isDark ? '#f9fafb' : '#334155',
                      boxShadow: '0 2px 6px rgba(0,0,0,0.3)',
                      '&:hover': { bgcolor: isDark ? '#4b5563' : '#f1f5f9' },
                    }}
                  >
                    <ScrollDownIcon />
                  </IconButton>
                </Badge>
              )}
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
                    onKeyDown={(e) => {
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
