import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Avatar, AvatarGroup, Box, Button, Dialog, DialogActions, DialogContent, DialogContentText,
  DialogTitle, IconButton, Menu, MenuItem, TextField, Tooltip, Typography, useTheme,
} from '@mui/material';
import ScrollDownIcon from '@mui/icons-material/KeyboardArrowDown';
import MoreVertIcon from '@mui/icons-material/MoreVert';
import SendIcon from '@mui/icons-material/Send';
import { useTranslation } from 'react-i18next';
import type { MessageDto } from '../../api/messagesApi';
import { ChatGroup, ChatGroupMessage, chatGroupsApi, groupTitle } from '../../shared/api/chatGroups';
import { getApiErrorMessage } from '../../shared/utils/apiError';
import { useNotifications } from '../../shared/context/NotificationContext';
import ChatMessageList from './ChatMessageList';
import { buildTimeline } from './chatMessageUtils';

const ACCENT = '#0891b2';
const ACCENT_DARK = '#0e7490';
const MUTED = '#64748b';
const BORDER = '#e2e8f0';

/** The list component draws one-to-one messages; a group message is one with no single receiver. */
const toListMessage = (m: ChatGroupMessage): MessageDto => ({
  messageId: m.messageId,
  senderId: m.senderId,
  receiverId: 0,
  messageText: m.messageText,
  sentAt: m.sentAt,
  // Never shown: receipts are off in groups.
  isRead: true,
  senderName: m.senderName ?? undefined,
  senderProfilePicture: m.senderProfilePicture ?? undefined,
});

interface Props {
  group: ChatGroup;
  currentUserId: number;
  /** The newest group message pushed to this user over SignalR (any group). */
  incoming: ChatGroupMessage | null;
  /** The group was opened or its messages arrived while it was open: nothing is unread any more. */
  onRead: (groupId: number) => void;
  /** Somebody (possibly me) wrote in this group: the sidebar preview follows. */
  onMessage: (groupId: number, message: ChatGroupMessage) => void;
  onLeft: (groupId: number) => void;
}

/**
 * A conversation of more than two people: two roommates and the landlord. Everyone sees the same
 * history, so what was said to the landlord before the roommate joined does not have to be retold.
 */
const GroupChatPanel: React.FC<Props> = ({ group, currentUserId, incoming, onRead, onMessage, onLeft }) => {
  const { t, i18n } = useTranslation(['chat', 'validation']);
  const theme = useTheme();
  const isDark = theme.palette.mode === 'dark';
  const { addNotification, refreshUnreadMessagesCount } = useNotifications();

  const [messages, setMessages] = useState<ChatGroupMessage[]>([]);
  const [text, setText] = useState('');
  const [sending, setSending] = useState(false);
  const [firstUnreadId, setFirstUnreadId] = useState<number | null>(null);
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null);
  const [confirmLeave, setConfirmLeave] = useState(false);
  const [showScrollDown, setShowScrollDown] = useState(false);

  const scrollRef = useRef<HTMLDivElement>(null);
  const stickToBottom = useRef(true);
  const lastIdRef = useRef<number | null>(null);
  const unreadAtOpenRef = useRef(group.unreadCount);

  const others = useMemo(() => group.members.filter(m => m.userId !== currentUserId), [group.members, currentUserId]);

  const scrollToBottom = () => {
    const el = scrollRef.current;
    if (el) el.scrollTop = el.scrollHeight;
  };

  const markRead = useCallback(async () => {
    try {
      await chatGroupsApi.markRead(group.groupId);
      onRead(group.groupId);
      refreshUnreadMessagesCount();
    } catch {
      // Best effort: the unread count is re-synced from the server on the next load.
    }
  }, [group.groupId, onRead, refreshUnreadMessagesCount]);

  // Opening a group: load the history, find the first unread message, mark it read.
  useEffect(() => {
    let cancelled = false;
    stickToBottom.current = true;
    lastIdRef.current = null;
    unreadAtOpenRef.current = group.unreadCount;
    setMessages([]);
    setFirstUnreadId(null);

    chatGroupsApi.getMessages(group.groupId)
      .then(page => {
        if (cancelled) return;
        setMessages(page.messages);
        const unread = unreadAtOpenRef.current;
        if (unread > 0) {
          const theirs = page.messages.filter(m => m.senderId !== currentUserId);
          setFirstUnreadId(theirs.length >= unread ? theirs[theirs.length - unread].messageId : theirs[0]?.messageId ?? null);
        }
        if (unread > 0) markRead();
      })
      .catch(error => {
        if (!cancelled) addNotification({ title: t('errorLoadingMessages'), message: getApiErrorMessage(error, t), type: 'error' });
      });

    return () => { cancelled = true; };
    // Only a different group reloads the history; unread counts and callbacks change while it is open.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [group.groupId]);

  // A message pushed over SignalR: append it when it is for this group, once.
  useEffect(() => {
    if (!incoming || incoming.groupId !== group.groupId) return;
    setMessages(prev => (prev.some(m => m.messageId === incoming.messageId) ? prev : [...prev, incoming]));
    if (incoming.senderId !== currentUserId) markRead();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [incoming]);

  // Keep the view glued to the bottom while the user is there; otherwise leave it and offer the arrow.
  useEffect(() => {
    const last = messages[messages.length - 1];
    if (!last || last.messageId === lastIdRef.current) return;
    const firstLoad = lastIdRef.current === null;
    lastIdRef.current = last.messageId;

    if (firstLoad) {
      const divider = scrollRef.current?.querySelector('[data-unread-divider]');
      if (divider) divider.scrollIntoView({ block: 'center' });
      else scrollToBottom();
    } else if (stickToBottom.current || last.senderId === currentUserId) {
      scrollToBottom();
    }
  }, [messages, currentUserId]);

  const handleScroll = () => {
    const el = scrollRef.current;
    if (!el) return;
    const nearBottom = el.scrollHeight - el.scrollTop - el.clientHeight < 80;
    stickToBottom.current = nearBottom;
    setShowScrollDown(!nearBottom);
  };

  const send = async () => {
    const body = text.trim();
    if (!body || sending) return;
    setSending(true);
    try {
      const sent = await chatGroupsApi.sendMessage(group.groupId, body);
      setMessages(prev => (prev.some(m => m.messageId === sent.messageId) ? prev : [...prev, sent]));
      onMessage(group.groupId, sent);
      setText('');
    } catch (error) {
      // The text stays in the box so the user can try again.
      addNotification({ title: t('messageNotSent'), message: getApiErrorMessage(error, t), type: 'error' });
    } finally {
      setSending(false);
    }
  };

  const leave = async () => {
    setConfirmLeave(false);
    try {
      await chatGroupsApi.leave(group.groupId);
      addNotification({ title: t('leftGroup'), message: '', type: 'success' });
      onLeft(group.groupId);
    } catch (error) {
      addNotification({ title: t('leaveGroup'), message: getApiErrorMessage(error, t), type: 'error' });
    }
  };

  const timeline = useMemo(
    () => buildTimeline(messages.map(toListMessage), currentUserId, firstUnreadId),
    [messages, currentUserId, firstUnreadId],
  );

  const title = groupTitle(group, currentUserId);

  return (
    <>
      <Box sx={{ px: 3, py: 2, borderBottom: `1px solid ${BORDER}`, bgcolor: isDark ? 'background.paper' : '#ffffff' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', minWidth: 0 }}>
            <AvatarGroup max={3} sx={{ '& .MuiAvatar-root': { width: 40, height: 40, fontSize: '1rem', bgcolor: ACCENT } }}>
              {others.map(m => (
                <Avatar key={m.userId} src={m.profilePicture || undefined} alt={m.name}>
                  {m.name?.[0]?.toUpperCase()}
                </Avatar>
              ))}
            </AvatarGroup>
            <Box sx={{ ml: 2, minWidth: 0 }}>
              <Typography variant="h6" fontWeight={600} noWrap color="text.primary">{title}</Typography>
              <Typography variant="caption" sx={{ color: MUTED }} noWrap component="div">
                {t('groupChat')} · {t('groupMembers', { names: group.members.map(m => m.name).join(', ') })}
              </Typography>
            </Box>
          </Box>
          <Tooltip title={t('more')}>
            <IconButton sx={{ color: MUTED }} onClick={e => setMenuAnchor(e.currentTarget)} aria-label={t('more')}>
              <MoreVertIcon />
            </IconButton>
          </Tooltip>
          <Menu anchorEl={menuAnchor} open={!!menuAnchor} onClose={() => setMenuAnchor(null)}>
            <MenuItem onClick={() => { setMenuAnchor(null); setConfirmLeave(true); }}>{t('leaveGroup')}</MenuItem>
          </Menu>
        </Box>
      </Box>

      <Box sx={{ position: 'relative', flex: 1, minHeight: 0, display: 'flex' }}>
        <Box
          ref={scrollRef}
          onScroll={handleScroll}
          sx={{ flex: 1, overflow: 'auto', px: { xs: 1, sm: 3 }, py: 2, bgcolor: isDark ? 'background.default' : '#f8fafc' }}
        >
          {messages.length === 0 && (
            <Typography color="text.secondary" textAlign="center" sx={{ py: 8 }}>{t('groupEmpty')}</Typography>
          )}
          <ChatMessageList items={timeline} locale={i18n.language} showReceipts={false} showSenderNames />
        </Box>
        {showScrollDown && (
          <Box sx={{ position: 'absolute', right: 20, bottom: 14 }}>
            <IconButton
              onClick={scrollToBottom}
              aria-label={t('scrollToLatest')}
              sx={{
                width: 40, height: 40,
                bgcolor: isDark ? '#374151' : '#ffffff', color: isDark ? '#f9fafb' : '#334155',
                boxShadow: '0 2px 6px rgba(0,0,0,0.3)',
                '&:hover': { bgcolor: isDark ? '#4b5563' : '#f1f5f9' },
              }}
            >
              <ScrollDownIcon />
            </IconButton>
          </Box>
        )}
      </Box>

      <Box sx={{ p: 2.5, borderTop: `1px solid ${BORDER}`, bgcolor: isDark ? 'background.paper' : '#ffffff' }}>
        <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'flex-end' }}>
          <TextField
            fullWidth multiline maxRows={4}
            placeholder={t('typeMessage')}
            value={text}
            onChange={e => setText(e.target.value)}
            onKeyDown={e => {
              if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                send();
              }
            }}
            inputProps={{ maxLength: 2000 }}
            sx={{
              '& .MuiOutlinedInput-root': {
                borderRadius: 3,
                bgcolor: isDark ? 'background.default' : '#f1f5f9',
                '& fieldset': { borderColor: 'transparent' },
                '&:hover fieldset, &.Mui-focused fieldset': { borderColor: ACCENT },
              },
            }}
          />
          <IconButton
            onClick={send}
            disabled={!text.trim() || sending}
            aria-label={t('send')}
            sx={{
              width: 48, height: 48,
              bgcolor: text.trim() ? ACCENT : 'transparent',
              color: text.trim() ? '#ffffff' : MUTED,
              '&:hover': { bgcolor: text.trim() ? ACCENT_DARK : 'transparent' },
              '&:disabled': { bgcolor: 'transparent', color: MUTED },
            }}
          >
            <SendIcon />
          </IconButton>
        </Box>
      </Box>

      <Dialog open={confirmLeave} onClose={() => setConfirmLeave(false)}>
        <DialogTitle>{t('leaveGroup')}</DialogTitle>
        <DialogContent><DialogContentText>{t('leaveGroupConfirm')}</DialogContentText></DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmLeave(false)}>{t('cancel')}</Button>
          <Button color="error" variant="contained" onClick={leave}>{t('leaveGroup')}</Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default GroupChatPanel;
