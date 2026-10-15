import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import type { MessageDto } from '../../api/messagesApi';
import ChatMessageList from './ChatMessageList';
import { buildTimeline } from './chatMessageUtils';

// Echo the key (and the count) so the assertions do not depend on translation text.
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, options?: { count?: number }) =>
      options?.count !== undefined ? `${key}:${options.count}` : key,
  }),
}));

const ME = 1;
const OTHER = 2;
let id = 1;
const msg = (senderId: number, text: string, over: Partial<MessageDto> = {}): MessageDto => ({
  messageId: id++,
  senderId,
  receiverId: senderId === ME ? OTHER : ME,
  messageText: text,
  sentAt: new Date().toISOString(),
  isRead: false,
  ...over,
});

const renderList = (
  messages: MessageDto[],
  firstUnread: number | null = null,
  options: { showReceipts?: boolean; showSenderNames?: boolean } = {},
) => render(<ChatMessageList items={buildTimeline(messages, ME, firstUnread)} locale="sr" {...options} />);

describe('ChatMessageList', () => {
  it('shows one red tick on my message until it is read', () => {
    id = 1;
    renderList([msg(ME, 'zdravo', { isRead: false })]);

    expect(screen.getByLabelText('messageNotRead')).toBeInTheDocument();
    expect(screen.queryByLabelText('messageRead')).not.toBeInTheDocument();
  });

  it('shows two green ticks once my message is read', () => {
    id = 1;
    renderList([msg(ME, 'zdravo', { isRead: true })]);

    expect(screen.getByLabelText('messageRead')).toBeInTheDocument();
    expect(screen.queryByLabelText('messageNotRead')).not.toBeInTheDocument();
  });

  it('puts the receipt on every one of my messages, not only the last of a run', () => {
    id = 1;
    renderList([msg(ME, 'a', { isRead: true }), msg(ME, 'b', { isRead: false }), msg(ME, 'c', { isRead: false })]);

    expect(screen.getAllByLabelText('messageRead')).toHaveLength(1);
    expect(screen.getAllByLabelText('messageNotRead')).toHaveLength(2);
  });

  it('shows no receipt on messages I received', () => {
    id = 1;
    renderList([msg(OTHER, 'ćao')]);

    expect(screen.getByText('ćao')).toBeInTheDocument();
    expect(screen.queryByLabelText('messageRead')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('messageNotRead')).not.toBeInTheDocument();
  });

  it('shows the time inside every bubble', () => {
    id = 1;
    const { container } = renderList([msg(ME, 'a'), msg(OTHER, 'b')]);

    expect(container.querySelectorAll('time')).toHaveLength(2);
  });

  it('shows a date chip for the day of the conversation', () => {
    id = 1;
    renderList([msg(OTHER, 'danas')]);

    expect(screen.getByText('dateToday')).toBeInTheDocument();
  });

  it('marks the unread divider with the number of unread messages', () => {
    id = 1;
    const read = msg(OTHER, 'staro', { isRead: true });
    const first = msg(OTHER, 'novo 1');
    const second = msg(OTHER, 'novo 2');
    const { container } = renderList([read, first, second], first.messageId);

    expect(screen.getByText('unreadDivider:2')).toBeInTheDocument();
    expect(container.querySelector('[data-unread-divider]')).not.toBeNull();
  });

  it('keeps line breaks of a multi-line message', () => {
    id = 1;
    renderList([msg(OTHER, 'prvi red\ndrugi red')]);

    const bubble = screen.getByText(/prvi red/);
    expect(getComputedStyle(bubble).whiteSpace).toBe('pre-wrap');
  });

  it('can leave the read ticks out (group conversations)', () => {
    id = 1;
    renderList([msg(ME, 'zdravo', { isRead: true }), msg(ME, 'opet', { isRead: false })], null, { showReceipts: false });

    expect(screen.queryByLabelText('messageRead')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('messageNotRead')).not.toBeInTheDocument();
    expect(screen.getByText('zdravo')).toBeInTheDocument();
  });

  it('names the sender above the first bubble of a run from somebody else, but never above mine', () => {
    id = 1;
    renderList(
      [
        msg(OTHER, 'prva', { senderName: 'Lazar Lazić' }),
        msg(OTHER, 'druga', { senderName: 'Lazar Lazić' }),
        msg(ME, 'moja', { senderName: 'Ana Anić' }),
        msg(3, 'treća', { senderName: 'Marko Marković' }),
      ],
      null,
      { showSenderNames: true },
    );

    expect(screen.getAllByText('Lazar Lazić')).toHaveLength(1);
    expect(screen.getByText('Marko Marković')).toBeInTheDocument();
    expect(screen.queryByText('Ana Anić')).not.toBeInTheDocument();
  });

  it('shows no sender names in a one-to-one conversation', () => {
    id = 1;
    renderList([msg(OTHER, 'ćao', { senderName: 'Lazar Lazić' })]);

    expect(screen.queryByText('Lazar Lazić')).not.toBeInTheDocument();
  });
});
