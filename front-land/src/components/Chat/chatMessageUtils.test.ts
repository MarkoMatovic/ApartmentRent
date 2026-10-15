import { describe, expect, it } from 'vitest';
import type { ConversationDto, MessageDto } from '../../api/messagesApi';
import {
  applyLiveMessage,
  buildTimeline,
  dayKey,
  formatDayLabel,
  markMessagesRead,
  parseServerDate,
  type TimelineItem,
} from './chatMessageUtils';

const ME = 1;
const OTHER = 2;

let nextId = 1;
const msg = (senderId: number, sentAt: string, over: Partial<MessageDto> = {}): MessageDto => ({
  messageId: nextId++,
  senderId,
  receiverId: senderId === ME ? OTHER : ME,
  messageText: 'text',
  sentAt,
  isRead: false,
  ...over,
});

const onlyMessages = (items: TimelineItem[]) =>
  items.filter((i): i is Extract<TimelineItem, { kind: 'message' }> => i.kind === 'message');

describe('parseServerDate', () => {
  it('reads a value without a zone as UTC, not as local time', () => {
    expect(parseServerDate('2026-10-13T09:35:00').toISOString()).toBe('2026-10-13T09:35:00.000Z');
  });

  it('leaves values that already carry a zone alone', () => {
    expect(parseServerDate('2026-10-13T09:35:00Z').toISOString()).toBe('2026-10-13T09:35:00.000Z');
    expect(parseServerDate('2026-10-13T11:35:00+02:00').toISOString()).toBe('2026-10-13T09:35:00.000Z');
  });
});

describe('buildTimeline', () => {
  it('starts with a date chip and groups consecutive messages of one sender', () => {
    nextId = 1;
    const items = buildTimeline(
      [
        msg(OTHER, '2026-10-13T09:00:00Z'),
        msg(OTHER, '2026-10-13T09:01:00Z'),
        msg(ME, '2026-10-13T09:02:00Z'),
      ],
      ME,
    );

    expect(items[0].kind).toBe('day');
    const [a, b, c] = onlyMessages(items);
    expect([a.startsGroup, a.endsGroup]).toEqual([true, false]);
    expect([b.startsGroup, b.endsGroup]).toEqual([false, true]);
    expect([c.startsGroup, c.endsGroup]).toEqual([true, true]);
    expect([a.isOwn, c.isOwn]).toEqual([false, true]);
  });

  it('adds a date chip when the day changes and restarts the group there', () => {
    nextId = 1;
    const items = buildTimeline(
      [msg(OTHER, '2026-10-13T10:00:00Z'), msg(OTHER, '2026-10-15T10:00:00Z')],
      ME,
    );

    expect(items.map(i => i.kind)).toEqual(['day', 'message', 'day', 'message']);
    const [first, second] = onlyMessages(items);
    expect(first.endsGroup).toBe(true);
    expect(second.startsGroup).toBe(true);
  });

  it('puts an unread divider before the first unread message with the number of unread ones', () => {
    nextId = 1;
    const read = msg(OTHER, '2026-10-13T09:00:00Z', { isRead: true });
    const own = msg(ME, '2026-10-13T09:01:00Z');
    const firstUnread = msg(OTHER, '2026-10-13T09:02:00Z');
    const secondUnread = msg(OTHER, '2026-10-13T09:03:00Z');

    const items = buildTimeline([read, own, firstUnread, secondUnread], ME, firstUnread.messageId);

    expect(items.map(i => i.kind)).toEqual(['day', 'message', 'message', 'unread', 'message', 'message']);
    expect(items.find(i => i.kind === 'unread')).toMatchObject({ count: 2 });
    const [, , third, fourth] = onlyMessages(items);
    expect(third.startsGroup).toBe(true);
    expect(fourth.startsGroup).toBe(false);
  });

  it('a divider splits a run from the same sender into two groups', () => {
    nextId = 1;
    const olderRead = msg(OTHER, '2026-10-13T09:00:00Z', { isRead: true });
    const unread = msg(OTHER, '2026-10-13T09:01:00Z');

    const [first, second] = onlyMessages(buildTimeline([olderRead, unread], ME, unread.messageId));

    expect(first.endsGroup).toBe(true);
    expect(second.startsGroup).toBe(true);
  });

  it('shows no divider when there is nothing unread', () => {
    nextId = 1;
    const items = buildTimeline([msg(OTHER, '2026-10-13T09:00:00Z')], ME, null);

    expect(items.some(i => i.kind === 'unread')).toBe(false);
  });

  it('copes with an empty conversation', () => {
    expect(buildTimeline([], ME)).toEqual([]);
  });
});

describe('formatDayLabel', () => {
  const labels = { today: 'Danas', yesterday: 'Juče' };
  const now = new Date(2026, 9, 13, 12, 0, 0); // 13 Oct 2026, local time

  it('says today and yesterday in words', () => {
    expect(formatDayLabel(new Date(2026, 9, 13, 8, 0), now, labels, 'sr')).toBe('Danas');
    expect(formatDayLabel(new Date(2026, 9, 12, 23, 0), now, labels, 'sr')).toBe('Juče');
  });

  it('writes older dates out in the given language, with the year only when it differs', () => {
    expect(formatDayLabel(new Date(2026, 8, 1), now, labels, 'en')).toBe('September 1');
    expect(formatDayLabel(new Date(2025, 8, 1), now, labels, 'en')).toBe('September 1, 2025');
  });

  it('counts "yesterday" across a month boundary', () => {
    const firstOfMonth = new Date(2026, 10, 1, 9, 0);
    expect(formatDayLabel(new Date(2026, 9, 31, 20, 0), firstOfMonth, labels, 'sr')).toBe('Juče');
  });
});

describe('dayKey', () => {
  it('uses the local calendar day, zero-padded', () => {
    expect(dayKey(new Date(2026, 0, 5))).toBe('2026-01-05');
  });
});

describe('markMessagesRead', () => {
  it('flips only the messages the server reported as read', () => {
    nextId = 1;
    const a = msg(ME, '2026-10-13T09:00:00Z');
    const b = msg(ME, '2026-10-13T09:01:00Z');

    const result = markMessagesRead([a, b], new Set([b.messageId]));

    expect(result.map(m => m.isRead)).toEqual([false, true]);
  });

  it('returns the same array when nothing changes, so React skips a re-render', () => {
    nextId = 1;
    const list = [msg(ME, '2026-10-13T09:00:00Z', { isRead: true })];

    expect(markMessagesRead(list, new Set([list[0].messageId]))).toBe(list);
    expect(markMessagesRead(list, new Set())).toBe(list);
  });
});

describe('applyLiveMessage', () => {
  const THIRD = 3;
  const conv = (otherUserId: number, over: Partial<ConversationDto> = {}): ConversationDto => ({
    otherUserId,
    otherUserName: `User ${otherUserId}`,
    unreadCount: 0,
    lastMessage: msg(otherUserId, '2026-10-13T08:00:00Z', { receiverId: ME }),
    ...over,
  });

  it('moves the conversation to the top with the message as its preview and one more unread', () => {
    const list = [conv(THIRD), conv(OTHER)];
    const incoming = msg(OTHER, '2026-10-13T09:00:00Z', { messageText: 'Hi' });

    const result = applyLiveMessage(list, incoming, ME, null);

    expect(result.map(c => c.otherUserId)).toEqual([OTHER, THIRD]);
    expect(result[0].lastMessage).toBe(incoming);
    expect(result[0].unreadCount).toBe(1);
  });

  it('counts nothing unread for the open conversation or for my own message', () => {
    const list = [conv(OTHER)];

    expect(applyLiveMessage(list, msg(OTHER, '2026-10-13T09:00:00Z'), ME, OTHER)[0].unreadCount).toBe(0);
    expect(applyLiveMessage(list, msg(ME, '2026-10-13T09:00:00Z'), ME, null)[0].unreadCount).toBe(0);
  });

  it('starts a conversation with someone new, named from the message', () => {
    const incoming = msg(THIRD, '2026-10-13T09:00:00Z', {
      receiverId: ME, senderName: 'Nova Osoba', senderProfilePicture: 'https://storage.test/avatars/a/b.webp',
    });

    const [first] = applyLiveMessage([conv(OTHER)], incoming, ME, null);

    expect(first).toEqual({
      otherUserId: THIRD,
      otherUserName: 'Nova Osoba',
      otherUserProfilePicture: 'https://storage.test/avatars/a/b.webp',
      lastMessage: incoming,
      unreadCount: 1,
    });
  });

  it('names a conversation I started from the receiver', () => {
    const outgoing = msg(ME, '2026-10-13T09:00:00Z', { receiverId: THIRD, receiverName: 'Primalac' });

    expect(applyLiveMessage([], outgoing, ME, null)[0]).toMatchObject({ otherUserId: THIRD, otherUserName: 'Primalac' });
  });

  it('changes nothing when the same message arrives twice', () => {
    const incoming = msg(OTHER, '2026-10-13T09:00:00Z');
    const once = applyLiveMessage([conv(OTHER)], incoming, ME, null);

    expect(applyLiveMessage(once, incoming, ME, null)).toBe(once);
  });

  it('keeps the preview hidden where the server hides it (chat history turned off)', () => {
    const [first] = applyLiveMessage([conv(OTHER, { lastMessage: undefined })], msg(OTHER, '2026-10-13T09:00:00Z'), ME, null);

    expect(first.lastMessage).toBeUndefined();
    expect(first.unreadCount).toBe(1);
  });
});
