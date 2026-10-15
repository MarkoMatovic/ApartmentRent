import { beforeEach, describe, expect, it, vi } from 'vitest';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ChatGroup, ChatGroupMessage } from '../../shared/api/chatGroups';

const api = vi.hoisted(() => ({
  getMessages: vi.fn(),
  sendMessage: vi.fn(),
  markRead: vi.fn(),
  leave: vi.fn(),
}));
const notify = vi.hoisted(() => ({ addNotification: vi.fn(), refreshUnreadMessagesCount: vi.fn() }));

vi.mock('../../shared/api/chatGroups', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../shared/api/chatGroups')>();
  return { ...actual, chatGroupsApi: api };
});
vi.mock('../../shared/context/NotificationContext', () => ({ useNotifications: () => notify }));
// Echo the key so the assertions do not depend on translation text.
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, options?: Record<string, unknown>) => (options?.names ? `${key}:${options.names}` : key),
    i18n: { language: 'sr' },
  }),
}));

import GroupChatPanel from './GroupChatPanel';

const ME = 1;
const group = (over: Partial<ChatGroup> = {}): ChatGroup => ({
  groupId: 10,
  members: [
    { userId: ME, name: 'Ana Anić' },
    { userId: 2, name: 'Marko Marković' },
    { userId: 3, name: 'Lazar Lazić' },
  ],
  unreadCount: 0,
  ...over,
});

let nextId = 1;
const msg = (senderId: number, senderName: string, messageText: string, over: Partial<ChatGroupMessage> = {}): ChatGroupMessage => ({
  messageId: nextId++,
  groupId: 10,
  senderId,
  senderName,
  messageText,
  sentAt: new Date().toISOString(),
  ...over,
});

const renderPanel = (over: Partial<React.ComponentProps<typeof GroupChatPanel>> = {}) => {
  const props = {
    group: group(),
    currentUserId: ME,
    incoming: null as ChatGroupMessage | null,
    onRead: vi.fn(),
    onMessage: vi.fn(),
    onLeft: vi.fn(),
    ...over,
  };
  return { props, ...render(<GroupChatPanel {...props} />) };
};

beforeEach(() => {
  nextId = 1;
  vi.clearAllMocks();
  Element.prototype.scrollIntoView = vi.fn();
  api.markRead.mockResolvedValue(undefined);
});

describe('GroupChatPanel', () => {
  it('shows the whole history with the name of whoever wrote each message', async () => {
    api.getMessages.mockResolvedValue({
      totalCount: 3, page: 1, pageSize: 50,
      messages: [msg(ME, 'Ana Anić', 'Zanima me stan'), msg(3, 'Lazar Lazić', 'Slobodan je'), msg(2, 'Marko Marković', 'I ja bih ga gledao')],
    });

    renderPanel();

    expect(await screen.findByText('Zanima me stan')).toBeInTheDocument();
    expect(screen.getByText('Slobodan je')).toBeInTheDocument();
    // names of the others, not of me
    expect(screen.getByText('Lazar Lazić')).toBeInTheDocument();
    expect(screen.getByText('Marko Marković', { selector: 'div' })).toBeInTheDocument();
    expect(screen.queryByText('Ana Anić', { selector: 'div.MuiTypography-caption' })).not.toBeInTheDocument();
  });

  it('shows no read ticks, because "read" would mean "read by whom"', async () => {
    api.getMessages.mockResolvedValue({ totalCount: 1, page: 1, pageSize: 50, messages: [msg(ME, 'Ana Anić', 'Zdravo')] });

    renderPanel();

    await screen.findByText('Zdravo');
    expect(screen.queryByLabelText('messageRead')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('messageNotRead')).not.toBeInTheDocument();
  });

  it('titles the conversation with everybody else and lists all participants', async () => {
    api.getMessages.mockResolvedValue({ totalCount: 0, page: 1, pageSize: 50, messages: [] });

    renderPanel();

    expect(await screen.findByText('Marko Marković, Lazar Lazić')).toBeInTheDocument();
    expect(screen.getByText(/groupMembers:Ana Anić, Marko Marković, Lazar Lazić/)).toBeInTheDocument();
    expect(screen.getByText('groupEmpty')).toBeInTheDocument();
  });

  it('marks the group read when it was opened with unread messages', async () => {
    api.getMessages.mockResolvedValue({
      totalCount: 2, page: 1, pageSize: 50,
      messages: [msg(3, 'Lazar Lazić', 'Prvi'), msg(3, 'Lazar Lazić', 'Drugi')],
    });

    const { props } = renderPanel({ group: group({ unreadCount: 2 }) });

    await waitFor(() => expect(api.markRead).toHaveBeenCalledWith(10));
    await waitFor(() => expect(props.onRead).toHaveBeenCalledWith(10));
    expect(notify.refreshUnreadMessagesCount).toHaveBeenCalled();
    // the divider is placed above the first unread message
    expect(await screen.findByText('unreadDivider')).toBeInTheDocument();
  });

  it('does not call mark-read when there is nothing unread', async () => {
    api.getMessages.mockResolvedValue({ totalCount: 1, page: 1, pageSize: 50, messages: [msg(3, 'Lazar Lazić', 'Staro')] });

    renderPanel();

    await screen.findByText('Staro');
    expect(api.markRead).not.toHaveBeenCalled();
  });

  it('sends a message through the API, shows it once and clears the box', async () => {
    api.getMessages.mockResolvedValue({ totalCount: 0, page: 1, pageSize: 50, messages: [] });
    const sent = msg(ME, 'Ana Anić', 'Stižemo u 10');
    api.sendMessage.mockResolvedValue(sent);

    const { props } = renderPanel();
    await screen.findByText('groupEmpty');

    const box = screen.getByPlaceholderText('typeMessage');
    fireEvent.change(box, { target: { value: '  Stižemo u 10  ' } });
    fireEvent.click(screen.getByLabelText('send'));

    expect(await screen.findByText('Stižemo u 10')).toBeInTheDocument();
    expect(api.sendMessage).toHaveBeenCalledWith(10, 'Stižemo u 10');
    expect(props.onMessage).toHaveBeenCalledWith(10, sent);
    expect(box).toHaveValue('');
  });

  it('keeps the typed text and says why when sending fails', async () => {
    api.getMessages.mockResolvedValue({ totalCount: 0, page: 1, pageSize: 50, messages: [] });
    api.sendMessage.mockRejectedValue({ response: { status: 500 } });

    renderPanel();
    await screen.findByText('groupEmpty');

    const box = screen.getByPlaceholderText('typeMessage');
    fireEvent.change(box, { target: { value: 'Neće proći' } });
    fireEvent.click(screen.getByLabelText('send'));

    await waitFor(() => expect(notify.addNotification).toHaveBeenCalledWith(expect.objectContaining({ type: 'error' })));
    expect(box).toHaveValue('Neće proći');
  });

  it('appends a message pushed for this group, once, and ignores one for another group', async () => {
    api.getMessages.mockResolvedValue({ totalCount: 0, page: 1, pageSize: 50, messages: [] });
    const pushed = msg(3, 'Lazar Lazić', 'Javljam se');

    const { rerender, props } = renderPanel();
    await screen.findByText('groupEmpty');

    const elsewhere = msg(3, 'Lazar Lazić', 'Druga grupa', { groupId: 99 });
    rerender(<GroupChatPanel {...props} incoming={elsewhere} />);
    expect(screen.queryByText('Druga grupa')).not.toBeInTheDocument();

    rerender(<GroupChatPanel {...props} incoming={pushed} />);
    expect(await screen.findByText('Javljam se')).toBeInTheDocument();
    // the same message delivered again (e.g. echoed to my other tab) is not doubled
    rerender(<GroupChatPanel {...props} incoming={{ ...pushed }} />);
    expect(screen.getAllByText('Javljam se')).toHaveLength(1);
    // somebody else wrote it while I look at the group: it is read at once
    await waitFor(() => expect(api.markRead).toHaveBeenCalledWith(10));
  });

  it('leaves the group after confirmation', async () => {
    api.getMessages.mockResolvedValue({ totalCount: 0, page: 1, pageSize: 50, messages: [] });
    api.leave.mockResolvedValue(undefined);

    const { props } = renderPanel();
    await screen.findByText('groupEmpty');

    fireEvent.click(screen.getByLabelText('more'));
    fireEvent.click(await screen.findByText('leaveGroup'));
    expect(api.leave).not.toHaveBeenCalled(); // asks first
    expect(await screen.findByText('leaveGroupConfirm')).toBeInTheDocument();

    await act(async () => {
      fireEvent.click(screen.getAllByText('leaveGroup').pop()!);
    });

    await waitFor(() => expect(api.leave).toHaveBeenCalledWith(10));
    expect(props.onLeft).toHaveBeenCalledWith(10);
  });
});
