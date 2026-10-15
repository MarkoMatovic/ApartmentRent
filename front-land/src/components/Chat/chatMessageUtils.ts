import type { ConversationDto, MessageDto } from '../../api/messagesApi';

const HAS_TIME_ZONE = /(Z|[+-]\d{2}:?\d{2})$/i;

/**
 * The API stores UTC but, depending on how the value left the database, may serialize it without a
 * zone suffix; `new Date('2026-10-13T09:35:00')` would then be read as *local* time and shown
 * hours off. Treat a value with no zone as UTC.
 */
export function parseServerDate(value: string): Date {
  return new Date(HAS_TIME_ZONE.test(value) ? value : `${value}Z`);
}

/** Local calendar day, e.g. "2026-10-13": messages are grouped by the day the user sees. */
export function dayKey(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

export type TimelineItem =
  | { kind: 'day'; key: string; date: Date }
  | { kind: 'unread'; key: string; count: number }
  | {
      kind: 'message';
      key: string;
      message: MessageDto;
      isOwn: boolean;
      /** First bubble of a run from one sender: gets the "tail" and more space above. */
      startsGroup: boolean;
      /** Last bubble of a run from one sender. */
      endsGroup: boolean;
    };

/**
 * Turns a flat message list into what a chat screen shows: a date chip whenever the day changes,
 * an "N unread messages" divider before the first message the user had not read, and bubbles
 * grouped by consecutive sender (a new day or a divider always starts a new group).
 */
export function buildTimeline(
  messages: MessageDto[],
  currentUserId: number,
  firstUnreadMessageId: number | null = null,
): TimelineItem[] {
  const items: TimelineItem[] = [];

  const unreadCount = firstUnreadMessageId === null
    ? 0
    : messages.filter(m => m.messageId >= firstUnreadMessageId && m.senderId !== currentUserId).length;

  let previousDay: string | null = null;
  let previousSender: number | null = null;
  let dividerShown = false;

  messages.forEach((message, index) => {
    const sentAt = parseServerDate(message.sentAt);
    const day = dayKey(sentAt);

    if (day !== previousDay) {
      items.push({ kind: 'day', key: `day-${day}`, date: sentAt });
      previousDay = day;
      previousSender = null;
    }

    if (!dividerShown && firstUnreadMessageId !== null && message.messageId === firstUnreadMessageId && unreadCount > 0) {
      items.push({ kind: 'unread', key: `unread-${message.messageId}`, count: unreadCount });
      dividerShown = true;
      previousSender = null;
    }

    const next = messages[index + 1];
    const nextIsSameGroup =
      next !== undefined &&
      next.senderId === message.senderId &&
      dayKey(parseServerDate(next.sentAt)) === day &&
      !(next.messageId === firstUnreadMessageId && !dividerShown);

    items.push({
      kind: 'message',
      key: `msg-${message.messageId}`,
      message,
      isOwn: message.senderId === currentUserId,
      startsGroup: previousSender !== message.senderId,
      endsGroup: !nextIsSameGroup,
    });
    previousSender = message.senderId;
  });

  return items;
}

/**
 * "Today" / "Yesterday" for the last two days, otherwise the full date in the user's language.
 */
export function formatDayLabel(
  date: Date,
  now: Date,
  labels: { today: string; yesterday: string },
  locale: string,
): string {
  const day = dayKey(date);
  if (day === dayKey(now)) return labels.today;

  const yesterday = new Date(now);
  yesterday.setDate(now.getDate() - 1);
  if (day === dayKey(yesterday)) return labels.yesterday;

  const sameYear = date.getFullYear() === now.getFullYear();
  return date.toLocaleDateString(locale, {
    day: 'numeric',
    month: 'long',
    ...(sameYear ? {} : { year: 'numeric' }),
  });
}

/** Clock time inside a bubble, e.g. "09:35". */
export function formatBubbleTime(value: string, locale: string): string {
  return parseServerDate(value).toLocaleTimeString(locale, { hour: '2-digit', minute: '2-digit' });
}

/**
 * A message pushed live (received or sent) applied to the conversation list without asking the server
 * for the whole list again: the conversation gets it as its preview, moves to the top and, when it is
 * not the one open, counts one more unread. A message from someone new starts a conversation, named
 * from the message itself. The same message twice changes nothing.
 */
export function applyLiveMessage(
  conversations: ConversationDto[],
  message: MessageDto,
  currentUserId: number,
  openOtherUserId: number | null,
): ConversationDto[] {
  const incoming = message.receiverId === currentUserId;
  const otherUserId = incoming ? message.senderId : message.receiverId;
  const addsUnread = incoming && otherUserId !== openOtherUserId ? 1 : 0;
  const existing = conversations.find(c => c.otherUserId === otherUserId);

  if (existing?.lastMessage?.messageId === message.messageId) return conversations;

  const updated: ConversationDto = existing
    ? {
      ...existing,
      // No preview means the server hides it (chat history turned off by one of the two): keep it hidden.
      lastMessage: existing.lastMessage ? message : undefined,
      unreadCount: existing.unreadCount + addsUnread,
    }
    : {
      otherUserId,
      otherUserName: incoming ? message.senderName : message.receiverName,
      otherUserProfilePicture: incoming ? message.senderProfilePicture : message.receiverProfilePicture,
      lastMessage: message,
      unreadCount: addsUnread,
    };

  return [updated, ...conversations.filter(c => c.otherUserId !== otherUserId)];
}

/** A message the other person has seen: ids reported by the server's "MessageRead" event. */
export function markMessagesRead(messages: MessageDto[], readIds: ReadonlySet<number>): MessageDto[] {
  if (readIds.size === 0) return messages;
  let changed = false;
  const updated = messages.map(m => {
    if (!m.isRead && readIds.has(m.messageId)) {
      changed = true;
      return { ...m, isRead: true };
    }
    return m;
  });
  return changed ? updated : messages;
}
