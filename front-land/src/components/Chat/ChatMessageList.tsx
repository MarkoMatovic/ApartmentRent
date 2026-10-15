import React from 'react';
import { Box, Typography, useTheme } from '@mui/material';
import DoneIcon from '@mui/icons-material/Done';
import DoneAllIcon from '@mui/icons-material/DoneAll';
import { useTranslation } from 'react-i18next';
import type { TimelineItem } from './chatMessageUtils';
import { formatBubbleTime, formatDayLabel } from './chatMessageUtils';

// Own bubbles are a deep teal so that the read receipts stay legible on them
// (contrast of the green tick 5.4:1 and of the red tick 3.3:1, white text 7.6:1).
const OWN_BUBBLE = '#0a5c6e';
const READ_TICK = '#86efac';
const UNREAD_TICK = '#ff8a80';

interface ChatMessageListProps {
  items: TimelineItem[];
  /** BCP-47 language of the UI, used for times and dates. */
  locale: string;
  /** Ticks on own bubbles. Off in group conversations, where "read" would have to mean "read by whom". */
  showReceipts?: boolean;
  /** Name of the sender above the first bubble of a run from somebody else (group conversations). */
  showSenderNames?: boolean;
}

// A name colour per sender, readable on both the white and the dark received bubble.
const SENDER_COLORS_LIGHT = ['#0e7490', '#b45309', '#6d28d9', '#be185d', '#15803d'];
const SENDER_COLORS_DARK = ['#67e8f9', '#fcd34d', '#c4b5fd', '#f9a8d4', '#86efac'];

/**
 * Messages the way WhatsApp draws them: date chips, an "N unread messages" divider, bubbles
 * grouped by sender with a tail on the first one, and the time plus read receipt inside each
 * of your own bubbles (one red tick = not read yet, two green ticks = read).
 */
const ChatMessageList: React.FC<ChatMessageListProps> = ({ items, locale, showReceipts = true, showSenderNames = false }) => {
  const { t } = useTranslation('chat');
  const theme = useTheme();
  const isDark = theme.palette.mode === 'dark';

  const receivedBubble = isDark ? '#374151' : '#ffffff';
  const receivedText = isDark ? '#f9fafb' : '#111827';
  const chipBg = isDark ? 'rgba(255,255,255,0.10)' : '#e2e8f0';
  const chipText = isDark ? 'rgba(255,255,255,0.75)' : '#475569';
  const now = new Date();

  return (
    <>
      {items.map(item => {
        if (item.kind === 'day') {
          return (
            <Box key={item.key} sx={{ display: 'flex', justifyContent: 'center', my: 1.5 }}>
              <Typography
                variant="caption"
                sx={{ px: 1.5, py: 0.4, borderRadius: 2, bgcolor: chipBg, color: chipText, fontWeight: 500 }}
              >
                {formatDayLabel(item.date, now, { today: t('dateToday'), yesterday: t('dateYesterday') }, locale)}
              </Typography>
            </Box>
          );
        }

        if (item.kind === 'unread') {
          return (
            <Box key={item.key} data-unread-divider sx={{ display: 'flex', justifyContent: 'center', my: 1.5 }}>
              <Typography
                variant="caption"
                sx={{
                  px: 1.5,
                  py: 0.4,
                  borderRadius: 2,
                  bgcolor: isDark ? 'rgba(8,145,178,0.28)' : '#cffafe',
                  color: isDark ? '#a5f3fc' : '#0e7490',
                  fontWeight: 600,
                }}
              >
                {t('unreadDivider', { count: item.count })}
              </Typography>
            </Box>
          );
        }

        const { message, isOwn, startsGroup } = item;
        const bubbleColor = isOwn ? OWN_BUBBLE : receivedBubble;

        return (
          <Box
            key={item.key}
            sx={{
              display: 'flex',
              justifyContent: isOwn ? 'flex-end' : 'flex-start',
              mt: startsGroup ? 1.25 : 0.25,
              // leave room for the tail so it is not clipped by the scroll container
              px: 1,
            }}
          >
            <Box
              sx={{
                position: 'relative',
                maxWidth: { xs: '85%', md: '70%' },
                minWidth: 64,
                px: 1.25,
                pt: 0.75,
                pb: 0.5,
                bgcolor: bubbleColor,
                color: isOwn ? '#ffffff' : receivedText,
                borderRadius: startsGroup
                  ? isOwn ? '12px 0 12px 12px' : '0 12px 12px 12px'
                  : '12px',
                boxShadow: '0 1px 1px rgba(0,0,0,0.18)',
                // the "tail": a small triangle on the first bubble of a run
                ...(startsGroup && {
                  '&::after': {
                    content: '""',
                    position: 'absolute',
                    top: 0,
                    ...(isOwn
                      ? { right: -7, borderLeft: `8px solid ${bubbleColor}` }
                      : { left: -7, borderRight: `8px solid ${bubbleColor}` }),
                    borderBottom: '8px solid transparent',
                  },
                }),
              }}
            >
              {showSenderNames && !isOwn && startsGroup && message.senderName && (
                <Typography
                  component="div"
                  variant="caption"
                  sx={{
                    fontWeight: 700,
                    lineHeight: 1.3,
                    color: (isDark ? SENDER_COLORS_DARK : SENDER_COLORS_LIGHT)[message.senderId % SENDER_COLORS_LIGHT.length],
                  }}
                >
                  {message.senderName}
                </Typography>
              )}
              <Typography
                component="div"
                variant="body2"
                sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', lineHeight: 1.45, color: 'inherit' }}
              >
                {message.messageText}
                {/* floats to the bottom-right and shares the last line with short messages */}
                <Box
                  component="span"
                  sx={{
                    float: 'right',
                    display: 'inline-flex',
                    alignItems: 'center',
                    gap: 0.4,
                    ml: 1.5,
                    mt: 0.9,
                    lineHeight: 1,
                    userSelect: 'none',
                  }}
                >
                  <Box
                    component="time"
                    dateTime={message.sentAt}
                    sx={{ fontSize: '0.68rem', opacity: isOwn ? 0.85 : 0.65 }}
                  >
                    {formatBubbleTime(message.sentAt, locale)}
                  </Box>
                  {isOwn && showReceipts && (
                    message.isRead ? (
                      <DoneAllIcon
                        aria-label={t('messageRead')}
                        sx={{ fontSize: 16, color: READ_TICK }}
                      />
                    ) : (
                      <DoneIcon
                        aria-label={t('messageNotRead')}
                        sx={{ fontSize: 16, color: UNREAD_TICK }}
                      />
                    )
                  )}
                </Box>
              </Typography>
              <Box sx={{ clear: 'both' }} />
            </Box>
          </Box>
        );
      })}
    </>
  );
};

export default ChatMessageList;
