import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import type { ChatGroup, ChatGroupMessage } from '../shared/api/chatGroups';
import type { Pairing } from '../shared/api/roommatePairing';
import type { MessageDto } from '../api/messagesApi';

const messages = vi.hoisted(() => ({
  getUserConversations: vi.fn(),
  getConversation: vi.fn(),
  markAsRead: vi.fn(),
  markConversationRead: vi.fn(),
  sendMessage: vi.fn(),
  getUnreadCount: vi.fn(),
}));
const groupsApi = vi.hoisted(() => ({
  getMine: vi.fn(), open: vi.fn(), getMessages: vi.fn(), sendMessage: vi.fn(), markRead: vi.fn(), leave: vi.fn(),
}));
const pairing = vi.hoisted(() => ({ partner: null as Pairing | null }));
const notify = vi.hoisted(() => ({ addNotification: vi.fn(), refreshUnreadMessagesCount: vi.fn() }));
const signalR = vi.hoisted(() => ({
  groupsChanged: 0,
  newMessage: null as MessageDto | null,
  newGroupMessage: null as ChatGroupMessage | null,
}));

vi.mock('../api/messagesApi', () => ({ messagesApi: messages }));
vi.mock('../shared/api/chatGroups', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../shared/api/chatGroups')>()),
  chatGroupsApi: groupsApi,
}));
vi.mock('../hooks/useMyPairing', () => ({ useMyPairing: () => ({ partner: pairing.partner }) }));
vi.mock('../hooks/useChatSignalR', () => ({
  useChatSignalR: () => ({
    connected: true,
    sendMessage: vi.fn().mockResolvedValue(true),
    markAsRead: vi.fn(),
    notifyTyping: vi.fn(),
    newMessage: signalR.newMessage,
    readMessageIds: [],
    userTyping: null,
    newGroupMessage: signalR.newGroupMessage,
    groupsChanged: signalR.groupsChanged,
  }),
}));
vi.mock('../shared/context/AuthContext', () => ({ useAuth: () => ({ user: { userId: 1 } }) }));
vi.mock('../shared/context/NotificationContext', () => ({ useNotifications: () => notify }));
vi.mock('../shared/api/analytics', () => ({ analyticsApi: { trackEvent: vi.fn() } }));
vi.mock('../shared/api/client', () => ({ default: { get: vi.fn() } }));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, options?: Record<string, unknown> | string) =>
      typeof options === 'object' && options?.names ? `${key}:${options.names}` : key,
    i18n: { language: 'sr' },
  }),
}));

import ChatPage from './ChatPage';

const ANA = 1;
const MARKO = 2; // my roommate
const LAZAR = 3; // the landlord

const landlordConversation = {
  otherUserId: LAZAR, otherUserName: 'Lazar Lazić', unreadCount: 0,
  lastMessage: { messageId: 1, senderId: LAZAR, receiverId: ANA, messageText: 'Slobodan je', sentAt: new Date().toISOString(), isRead: true },
};
const roommateConversation = { otherUserId: MARKO, otherUserName: 'Marko Marković', unreadCount: 0 };

const sharedGroup = (over: Partial<ChatGroup> = {}): ChatGroup => ({
  groupId: 10,
  members: [
    { userId: ANA, name: 'Ana Anić' },
    { userId: MARKO, name: 'Marko Marković' },
    { userId: LAZAR, name: 'Lazar Lazić' },
  ],
  lastMessage: {
    messageId: 7, groupId: 10, senderId: LAZAR, senderName: 'Lazar Lazić', messageText: 'Vidimo se u subotu',
    sentAt: new Date().toISOString(),
  },
  unreadCount: 2,
  ...over,
});

const roommate: Pairing = {
  pairingId: 5, status: 'Accepted', partnerUserId: MARKO, partnerName: 'Marko Marković',
  isIncoming: false, createdDate: '2026-10-01T10:00:00Z',
};

const renderChat = (url = '/chat') =>
  render(<MemoryRouter initialEntries={[url]}><ChatPage /></MemoryRouter>);

beforeEach(() => {
  vi.clearAllMocks();
  Element.prototype.scrollIntoView = vi.fn();
  pairing.partner = null;
  signalR.groupsChanged = 0;
  signalR.newMessage = null;
  signalR.newGroupMessage = null;
  messages.getUserConversations.mockResolvedValue([landlordConversation, roommateConversation]);
  messages.getConversation.mockResolvedValue({ totalCount: 0, page: 1, pageSize: 50, messages: [] });
  messages.markAsRead.mockResolvedValue(undefined);
  messages.markConversationRead.mockResolvedValue(0);
  groupsApi.getMine.mockResolvedValue([]);
  groupsApi.getMessages.mockResolvedValue({ totalCount: 0, page: 1, pageSize: 50, messages: [] });
  groupsApi.markRead.mockResolvedValue(undefined);
});

describe('ChatPage - shared conversations', () => {
  it('lists the shared conversation next to the one-to-one ones, with its badge and last message', async () => {
    groupsApi.getMine.mockResolvedValue([sharedGroup()]);

    renderChat();

    const title = await screen.findByText('Marko Marković, Lazar Lazić');
    expect(title).toBeInTheDocument();
    expect(screen.getByText('groupBadge')).toBeInTheDocument();
    expect(screen.getByText('Lazar: Vidimo se u subotu')).toBeInTheDocument();
    // two unread messages in the group
    expect(screen.getByText('2')).toBeInTheDocument();
    // and the one-to-one conversations are still there
    expect(screen.getByText('Lazar Lazić')).toBeInTheDocument();
  });

  it('opens the shared conversation when it is chosen, replacing the one-to-one view', async () => {
    groupsApi.getMine.mockResolvedValue([sharedGroup()]);

    renderChat();
    fireEvent.click(await screen.findByText('Marko Marković, Lazar Lazić'));

    await waitFor(() => expect(groupsApi.getMessages).toHaveBeenCalledWith(10));
    // the panel header names the participants
    expect(await screen.findByText(/groupMembers:Ana Anić, Marko Marković, Lazar Lazić/)).toBeInTheDocument();
    expect(screen.queryByPlaceholderText('typeMessage')).toBeInTheDocument();
  });

  it('opens the shared conversation a notification links to (?group=)', async () => {
    groupsApi.getMine.mockResolvedValue([sharedGroup({ unreadCount: 0 })]);

    renderChat('/chat?group=10');

    await waitFor(() => expect(groupsApi.getMessages).toHaveBeenCalledWith(10));
    expect(await screen.findByText(/groupMembers:Ana Anić, Marko Marković, Lazar Lazić/)).toBeInTheDocument();
  });

  it('ignores a ?group= link to a conversation the user is not in', async () => {
    groupsApi.getMine.mockResolvedValue([sharedGroup({ unreadCount: 0 })]);

    renderChat('/chat?group=999');

    await screen.findByText('Marko Marković, Lazar Lazić');
    expect(groupsApi.getMessages).not.toHaveBeenCalled();
  });

  it('can search the shared conversations by participant', async () => {
    groupsApi.getMine.mockResolvedValue([sharedGroup()]);

    renderChat();
    await screen.findByText('Marko Marković, Lazar Lazić');

    fireEvent.change(screen.getByPlaceholderText('searchConversations'), { target: { value: 'nikoga' } });

    expect(screen.queryByText('Marko Marković, Lazar Lazić')).not.toBeInTheDocument();
    expect(screen.getByText('noResults')).toBeInTheDocument();
  });

  it('shows no empty-state while there is a shared conversation but no one-to-one one', async () => {
    messages.getUserConversations.mockResolvedValue([]);
    groupsApi.getMine.mockResolvedValue([sharedGroup()]);

    renderChat();

    await screen.findByText('Marko Marković, Lazar Lazić');
    expect(screen.queryByText('noConversations')).not.toBeInTheDocument();
  });

  it('offers "add my roommate" in a conversation with the landlord, and opens the new shared conversation', async () => {
    pairing.partner = roommate;
    groupsApi.open.mockResolvedValue(sharedGroup({ unreadCount: 0 }));

    renderChat(`/chat?userId=${LAZAR}`);
    fireEvent.click(await screen.findByText('addRoommateToChat'));

    await waitFor(() => expect(groupsApi.open).toHaveBeenCalledWith(LAZAR));
    await waitFor(() => expect(notify.addNotification).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'groupOpened', type: 'success' })));
    expect(await screen.findByText(/groupMembers:/)).toBeInTheDocument();
  });

  it('has the same action as an icon for a narrow screen, where the words do not fit', async () => {
    pairing.partner = roommate;
    groupsApi.open.mockResolvedValue(sharedGroup({ unreadCount: 0 }));

    renderChat(`/chat?userId=${LAZAR}`);
    await screen.findByText('addRoommateToChat');

    // the icon button is always in the page; CSS shows it on a phone and the worded one on a wide screen
    fireEvent.click(screen.getByLabelText('addRoommateToChat'));

    await waitFor(() => expect(groupsApi.open).toHaveBeenCalledWith(LAZAR));
  });

  it('opens the existing shared conversation instead of creating another one', async () => {
    pairing.partner = roommate;
    groupsApi.getMine.mockResolvedValue([sharedGroup({ unreadCount: 0 })]);

    renderChat(`/chat?userId=${LAZAR}`);
    fireEvent.click(await screen.findByText('openGroupChat'));

    await waitFor(() => expect(groupsApi.getMessages).toHaveBeenCalledWith(10));
    expect(groupsApi.open).not.toHaveBeenCalled();
  });

  it('shows the reason when the shared conversation cannot be opened', async () => {
    pairing.partner = roommate;
    groupsApi.open.mockRejectedValue({ response: { status: 422, data: { code: 'GROUP_CHAT_NOT_ALLOWED' } } });

    renderChat(`/chat?userId=${LAZAR}`);
    fireEvent.click(await screen.findByText('addRoommateToChat'));

    await waitFor(() => expect(notify.addNotification).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'groupOpenFailed', type: 'error' })));
  });

  it('does not offer it without a confirmed roommate', async () => {
    renderChat(`/chat?userId=${LAZAR}`);

    await screen.findAllByText('Lazar Lazić');
    expect(screen.queryByText('addRoommateToChat')).not.toBeInTheDocument();
    expect(screen.queryByText('openGroupChat')).not.toBeInTheDocument();
  });

  it('does not offer it in the conversation with the roommate themself', async () => {
    pairing.partner = roommate;

    renderChat(`/chat?userId=${MARKO}`);

    await screen.findAllByText('Marko Marković');
    expect(screen.queryByText('addRoommateToChat')).not.toBeInTheDocument();
  });

  it('reloads the shared conversations when somebody adds me to one', async () => {
    groupsApi.getMine.mockResolvedValueOnce([]);
    const { rerender } = renderChat();
    await screen.findAllByText('Lazar Lazić');
    expect(groupsApi.getMine).toHaveBeenCalledTimes(1);

    groupsApi.getMine.mockResolvedValue([sharedGroup()]);
    signalR.groupsChanged = 1;
    rerender(<MemoryRouter initialEntries={['/chat']}><ChatPage /></MemoryRouter>);

    await waitFor(() => expect(groupsApi.getMine).toHaveBeenCalledTimes(2));
    expect(await screen.findByText('Marko Marković, Lazar Lazić')).toBeInTheDocument();
    // sidebar entry, not the header of an open panel
    expect(within(screen.getByRole('list')).getByText('groupBadge')).toBeInTheDocument();
  });
});

describe('ChatPage - one-to-one conversations', () => {
  const unreadFromLazar = (id: number): MessageDto => ({
    messageId: id, senderId: LAZAR, receiverId: ANA, messageText: `Poruka ${id}`,
    sentAt: new Date().toISOString(), isRead: false,
  });

  it('reads the whole conversation with one request when it is opened', async () => {
    messages.getUserConversations.mockResolvedValue([{ ...landlordConversation, unreadCount: 3 }, roommateConversation]);
    messages.getConversation.mockResolvedValue({
      totalCount: 3, page: 1, pageSize: 50, messages: [unreadFromLazar(11), unreadFromLazar(12), unreadFromLazar(13)],
    });
    messages.markConversationRead.mockResolvedValue(3);

    renderChat();
    expect(await screen.findByText('3')).toBeInTheDocument();
    fireEvent.click(screen.getByText('Lazar Lazić'));

    await waitFor(() => expect(messages.markConversationRead).toHaveBeenCalledTimes(1));
    expect(messages.markConversationRead).toHaveBeenCalledWith(LAZAR);
    expect(messages.markAsRead).not.toHaveBeenCalled();
    // the badge goes (MUI keeps the old number in the hidden badge) without fetching the whole list again
    await waitFor(() => expect(screen.getByText('3')).toHaveClass('MuiBadge-invisible'));
    expect(messages.getUserConversations).toHaveBeenCalledTimes(1);
    expect(notify.refreshUnreadMessagesCount).toHaveBeenCalled();
  });

  it('sends no read request for a conversation with nothing unread', async () => {
    renderChat();
    fireEvent.click(await screen.findByText('Lazar Lazić'));

    await waitFor(() => expect(messages.getConversation).toHaveBeenCalled());
    expect(messages.markConversationRead).not.toHaveBeenCalled();
  });

  it('updates the list from a live message instead of reloading it', async () => {
    const { rerender } = renderChat();
    await screen.findByText('Slobodan je');

    signalR.newMessage = { ...unreadFromLazar(20), messageText: 'Može i sutra' };
    rerender(<MemoryRouter initialEntries={['/chat']}><ChatPage /></MemoryRouter>);

    expect(await screen.findByText('Može i sutra')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument(); // one unread, the conversation is not open
    expect(messages.getUserConversations).toHaveBeenCalledTimes(1);
  });
});

describe('ChatPage - live messages in a shared conversation', () => {
  const groupMessage = (over: Partial<ChatGroupMessage> = {}): ChatGroupMessage => ({
    messageId: 50, groupId: 10, senderId: MARKO, senderName: 'Marko Marković', messageText: 'Može u 18h',
    sentAt: new Date().toISOString(), ...over,
  });

  it('updates the preview and the badge without reloading the list', async () => {
    groupsApi.getMine.mockResolvedValue([sharedGroup()]);
    const { rerender } = renderChat();
    await screen.findByText('Lazar: Vidimo se u subotu');

    signalR.newGroupMessage = groupMessage();
    rerender(<MemoryRouter initialEntries={['/chat']}><ChatPage /></MemoryRouter>);

    expect(await screen.findByText('Marko: Može u 18h')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument(); // two unread before, one more now
    expect(groupsApi.getMine).toHaveBeenCalledTimes(1);
  });

  it('reloads the list for a conversation it does not know yet', async () => {
    groupsApi.getMine.mockResolvedValue([sharedGroup()]);
    const { rerender } = renderChat();
    await screen.findByText('Lazar: Vidimo se u subotu');

    signalR.newGroupMessage = groupMessage({ groupId: 11 });
    rerender(<MemoryRouter initialEntries={['/chat']}><ChatPage /></MemoryRouter>);

    await waitFor(() => expect(groupsApi.getMine).toHaveBeenCalledTimes(2));
  });
});
