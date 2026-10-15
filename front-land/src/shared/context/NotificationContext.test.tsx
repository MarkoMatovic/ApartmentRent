import { beforeEach, describe, expect, it, vi } from 'vitest';
import { act, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { StoredNotification } from '../api/notifications';

type Handler = (...args: unknown[]) => void;
interface FakeConnection {
  url: string;
  handlers: Record<string, Handler>;
  started: boolean;
  /** Which handlers were already registered at the moment the connection was started. */
  handlersAtStart: string[];
  stopped: boolean;
  invoked: Array<[string, ...unknown[]]>;
  onReconnected?: () => void;
}

const hub = vi.hoisted(() => ({ connections: [] as FakeConnection[] }));
const messages = vi.hoisted(() => ({ getUnreadCount: vi.fn() }));
const stored = vi.hoisted(() => ({
  getUserNotifications: vi.fn(),
  markAsRead: vi.fn(),
  markAllAsRead: vi.fn(),
}));
const session = vi.hoisted(() => ({
  value: { user: null as { userId: number } | null, token: null as string | null },
}));

vi.mock('@microsoft/signalr', () => {
  class HubConnectionBuilder {
    private url = '';
    withUrl(url: string) { this.url = url; return this; }
    withAutomaticReconnect() { return this; }
    build() {
      const connection: FakeConnection = { url: this.url, handlers: {}, started: false, handlersAtStart: [], stopped: false, invoked: [] };
      hub.connections.push(connection);
      return {
        on: (name: string, handler: Handler) => { connection.handlers[name] = handler; },
        onreconnected: (callback: () => void) => { connection.onReconnected = callback; },
        start: () => { connection.started = true; connection.handlersAtStart = Object.keys(connection.handlers); return Promise.resolve(); },
        stop: () => { connection.stopped = true; return Promise.resolve(); },
        invoke: (method: string, ...args: unknown[]) => { connection.invoked.push([method, ...args]); return Promise.resolve(); },
      };
    }
  }
  return { HubConnectionBuilder };
});
vi.mock('../../api/messagesApi', () => ({ messagesApi: messages }));
vi.mock('../api/notifications', () => ({ notificationsApi: stored }));
vi.mock('../../components/Dialogs/RejectionDialog', () => ({ default: () => null }));
vi.mock('../api/client', () => ({ apiBaseUrl: 'http://api.test' }));
vi.mock('../api/tokenStore', () => ({ getAccessToken: () => session.value.token }));
vi.mock('./AuthContext', () => ({ useAuth: () => session.value }));

import { NotificationProvider, useNotifications } from './NotificationContext';
import { MY_PAIRING_KEY, SHARED_APARTMENTS_KEY } from '../api/queryKeys';

let api: ReturnType<typeof useNotifications>;
const Probe = () => {
  api = useNotifications();
  return (
    <div>
      <span data-testid="unread-messages">{api.unreadMessagesCount}</span>
      <span data-testid="unread">{api.unreadCount}</span>
      <ul>
        {api.notifications.map(n => (
          <li key={n.id} data-id={n.id} data-key={n.descriptor?.key ?? ''} data-link={n.descriptor?.link ?? ''}
            data-name={n.descriptor?.params?.name ?? ''} data-read={String(n.read)}>
            {n.title}|{n.message}
          </li>
        ))}
      </ul>
    </div>
  );
};

const client = new QueryClient();
const tree = () => (
  <QueryClientProvider client={client}>
    <NotificationProvider><Probe /></NotificationProvider>
  </QueryClientProvider>
);

const signIn = (userId = 7) => { session.value = { user: { userId }, token: `token-${userId}` }; };
const signOut = () => { session.value = { user: null, token: null }; };

const notificationHub = () => hub.connections.filter(c => c.url.endsWith('/notificationHub')).at(-1)!;
const chatHub = () => hub.connections.filter(c => c.url.endsWith('/chatHub')).at(-1)!;
const push = (...args: unknown[]) => act(() => { notificationHub().handlers.ReceiveNotification(...args); });

const storedItem = (over: Partial<StoredNotification> = {}): StoredNotification => ({
  id: 11, title: 'Cimerstvo potvrđeno', message: 'Marko je prihvatio/la.', actionType: 'pairing_accepted',
  actionTarget: '/my-roommate', key: 'pairing_accepted', params: { name: 'Marko' }, isRead: false,
  createdDate: '2026-10-13T10:00:00', senderUserId: 2, recipientUserId: 7, ...over,
});

beforeEach(() => {
  vi.clearAllMocks();
  hub.connections = [];
  signOut();
  messages.getUnreadCount.mockResolvedValue(0);
  stored.getUserNotifications.mockResolvedValue([]);
  stored.markAsRead.mockResolvedValue(undefined);
  stored.markAllAsRead.mockResolvedValue(true);
});

describe('NotificationProvider - connecting', () => {
  it('connects nothing while nobody is signed in', () => {
    render(tree());

    expect(hub.connections).toHaveLength(0);
    expect(stored.getUserNotifications).not.toHaveBeenCalled();
  });

  it('connects as soon as the session is restored AFTER the app started (the usual order on a page load)', async () => {
    const view = render(tree());
    expect(hub.connections).toHaveLength(0);

    signIn(7);
    view.rerender(tree());

    await waitFor(() => expect(hub.connections).toHaveLength(2));
    expect(notificationHub().started).toBe(true);
    expect(chatHub().started).toBe(true);
    expect(notificationHub().invoked).toContainEqual(['JoinNotificationGroup', 7]);
    expect(chatHub().invoked).toContainEqual(['JoinChatRoom', 7]);
    expect(stored.getUserNotifications).toHaveBeenCalledWith(7);
  });

  it('connects at once when the user is already signed in', async () => {
    signIn(7);

    render(tree());

    await waitFor(() => expect(notificationHub()?.started).toBe(true));
  });

  it('registers its handlers before it starts, so nothing arriving right after the connection opens is lost', () => {
    signIn(7);

    render(tree());

    expect(notificationHub().handlersAtStart).toContain('ReceiveNotification');
    expect(chatHub().handlersAtStart).toEqual(expect.arrayContaining(['ReceiveMessage', 'MessageSent', 'ReceiveGroupMessage']));
  });

  it('disconnects and forgets the notifications when the user signs out', async () => {
    signIn(7);
    const view = render(tree());
    await waitFor(() => expect(hub.connections).toHaveLength(2));
    await push('Naslov', 'Tekst', 'info');
    expect(screen.getByText('Naslov|Tekst')).toBeInTheDocument();
    const [first, second] = hub.connections;

    signOut();
    view.rerender(tree());

    await waitFor(() => expect(first.stopped && second.stopped).toBe(true));
    expect(screen.queryByText('Naslov|Tekst')).not.toBeInTheDocument();
    expect(screen.getByTestId('unread-messages')).toHaveTextContent('0');
  });

  it('connects again, and shows nothing of the previous user, when somebody else signs in', async () => {
    signIn(7);
    const view = render(tree());
    await waitFor(() => expect(hub.connections).toHaveLength(2));
    await push('Za prvog korisnika', 'Tekst', 'info');

    signIn(8);
    view.rerender(tree());

    await waitFor(() => expect(hub.connections).toHaveLength(4));
    expect(notificationHub().invoked).toContainEqual(['JoinNotificationGroup', 8]);
    expect(stored.getUserNotifications).toHaveBeenLastCalledWith(8);
    expect(screen.queryByText('Za prvog korisnika|Tekst')).not.toBeInTheDocument();
  });

  it('fetches what it missed when the connection comes back', async () => {
    signIn(7);
    render(tree());
    await waitFor(() => expect(notificationHub()?.onReconnected).toBeDefined());
    stored.getUserNotifications.mockClear();
    stored.getUserNotifications.mockResolvedValue([storedItem({ id: 55, title: 'Stiglo dok je veza bila prekinuta' })]);

    act(() => notificationHub().onReconnected!());

    expect(await screen.findByText('Stiglo dok je veza bila prekinuta|Marko je prihvatio/la.')).toBeInTheDocument();
    expect(notificationHub().invoked.filter(([m]) => m === 'JoinNotificationGroup')).toHaveLength(2);
  });
});

describe('NotificationProvider - the notifications', () => {
  beforeEach(() => signIn(7));

  it('shows the stored ones the user missed while away, with their key, values and route', async () => {
    stored.getUserNotifications.mockResolvedValue([storedItem()]);

    render(tree());

    const item = await screen.findByText('Cimerstvo potvrđeno|Marko je prihvatio/la.');
    expect(item).toHaveAttribute('data-key', 'pairing_accepted');
    expect(item).toHaveAttribute('data-name', 'Marko');
    expect(item).toHaveAttribute('data-link', '/my-roommate');
    expect(item).toHaveAttribute('data-read', 'false');
    expect(screen.getByTestId('unread')).toHaveTextContent('1');
  });

  it('lists the stored ones newest first, and keeps an old one that has no key as plain text', async () => {
    stored.getUserNotifications.mockResolvedValue([
      storedItem({ id: 1, title: 'Staro', message: 'bez ključa', key: null, params: null, actionTarget: '42', createdDate: '2026-10-01T10:00:00' }),
      storedItem({ id: 2, title: 'Novo', message: 'sa ključem', createdDate: '2026-10-13T10:00:00' }),
    ]);

    render(tree());

    await screen.findByText('Novo|sa ključem');
    const order = screen.getAllByRole('listitem').map(li => li.textContent);
    expect(order).toEqual(['Novo|sa ključem', 'Staro|bez ključa']);
    const old = screen.getByText('Staro|bez ključa');
    expect(old).toHaveAttribute('data-key', '');
    // an id is not a route, so the old one is simply not clickable
    expect(old).toHaveAttribute('data-link', '');
  });

  it('keeps the key, the values and the route that come with a live notification', async () => {
    render(tree());
    await waitFor(() => expect(notificationHub()?.handlers.ReceiveNotification).toBeDefined());

    push(
      'Novi stan na zajedničkoj listi', 'Ana je podelila stan.', 'info', '/my-roommate',
      JSON.stringify({ id: 31, key: 'pairing_apartment', params: { name: 'Ana Anić', title: 'Stan' }, link: '/my-roommate' }),
    );

    const item = await screen.findByText('Novi stan na zajedničkoj listi|Ana je podelila stan.');
    expect(item).toHaveAttribute('data-key', 'pairing_apartment');
    expect(item).toHaveAttribute('data-link', '/my-roommate');
    expect(item).toHaveAttribute('data-name', 'Ana Anić');
    expect(item).toHaveAttribute('data-id', 'server-31');
  });

  it('shows a notification once, whether it arrives live, from the stored list, or both', async () => {
    stored.getUserNotifications.mockResolvedValue([storedItem({ id: 31 })]);
    render(tree());
    await screen.findByText('Cimerstvo potvrđeno|Marko je prihvatio/la.');

    push('Cimerstvo potvrđeno', 'Marko je prihvatio/la.', 'info', '/my-roommate',
      JSON.stringify({ id: 31, key: 'pairing_accepted', params: { name: 'Marko' }, link: '/my-roommate' }));
    // and a reconnect fetches the same list again
    stored.getUserNotifications.mockResolvedValue([storedItem({ id: 31 })]);
    act(() => notificationHub().onReconnected!());

    await waitFor(() => expect(stored.getUserNotifications).toHaveBeenCalledTimes(2));
    expect(screen.getAllByRole('listitem')).toHaveLength(1);
  });

  it('still shows a notification from an older server that sends no descriptor', async () => {
    render(tree());
    await waitFor(() => expect(notificationHub()?.handlers.ReceiveNotification).toBeDefined());

    push('Nova poruka', 'Imate novu poruku', 'info');

    const item = await screen.findByText('Nova poruka|Imate novu poruku');
    expect(item).toHaveAttribute('data-key', '');
    expect(item).toHaveAttribute('data-link', '');
  });

  it('drops a link that is not an in-app route', async () => {
    render(tree());
    await waitFor(() => expect(notificationHub()?.handlers.ReceiveNotification).toBeDefined());

    push('Naslov', 'Tekst', 'info', '', JSON.stringify({ key: 'new_message', link: 'https://evil.example' }));

    expect(await screen.findByText('Naslov|Tekst')).toHaveAttribute('data-link', '');
  });

  it('tells the server when a stored notification is read, and when everything is', async () => {
    stored.getUserNotifications.mockResolvedValue([storedItem({ id: 11 }), storedItem({ id: 12, title: 'Drugo' })]);
    render(tree());
    await screen.findByText('Drugo|Marko je prihvatio/la.');

    act(() => api.markAsRead('server-11'));
    expect(stored.markAsRead).toHaveBeenCalledWith(11);
    expect(screen.getByTestId('unread')).toHaveTextContent('1');

    act(() => api.markAllAsRead());
    expect(stored.markAllAsRead).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId('unread')).toHaveTextContent('0');
  });

  it('does not bother the server about a notification that was never stored', async () => {
    render(tree());
    await waitFor(() => expect(notificationHub()?.handlers.ReceiveNotification).toBeDefined());
    act(() => api.addNotification({ title: 'Lokalno', message: 'samo u pregledaču', type: 'success' }));

    const local = api.notifications[0];
    act(() => api.markAsRead(local.id));

    expect(stored.markAsRead).not.toHaveBeenCalled();
  });

  it('dismissing a read notification only removes it from the bell', async () => {
    stored.getUserNotifications.mockResolvedValue([storedItem({ id: 11 })]);
    render(tree());
    await screen.findByText('Cimerstvo potvrđeno|Marko je prihvatio/la.');

    act(() => api.clearNotification('server-11'));

    expect(screen.queryAllByRole('listitem')).toHaveLength(0);
    expect(stored.markAsRead).not.toHaveBeenCalled();
  });

  it('keeps working when the stored ones cannot be loaded', async () => {
    stored.getUserNotifications.mockRejectedValue(new Error('network'));
    render(tree());
    await waitFor(() => expect(notificationHub()?.handlers.ReceiveNotification).toBeDefined());

    push('Uživo', 'ipak stiže', 'info');

    expect(await screen.findByText('Uživo|ipak stiže')).toBeInTheDocument();
  });

  it('refreshes the roommate state and the shared list when the other person answers or shares', async () => {
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    render(tree());
    await waitFor(() => expect(notificationHub()?.handlers.ReceiveNotification).toBeDefined());

    push('Cimerstvo potvrđeno', 'Marko je prihvatio.', 'info', '/my-roommate',
      JSON.stringify({ id: 5, key: 'pairing_accepted', params: { name: 'Marko' }, link: '/my-roommate' }));

    expect(invalidate).toHaveBeenCalledWith({ queryKey: [MY_PAIRING_KEY] });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: [SHARED_APARTMENTS_KEY] });
  });

  it('leaves the roommate state alone for other notifications', async () => {
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    render(tree());
    await waitFor(() => expect(notificationHub()?.handlers.ReceiveNotification).toBeDefined());
    invalidate.mockClear();

    push('Nova poruka', 'Tekst', 'info', '', JSON.stringify({ key: 'new_message', params: { name: 'Ana' }, link: '/chat?userId=3' }));

    expect(invalidate).not.toHaveBeenCalled();
  });
});

describe('NotificationProvider - the unread message badge', () => {
  beforeEach(() => signIn(7));

  it('counts a message in a shared conversation towards the badge', async () => {
    render(tree());
    await waitFor(() => expect(chatHub()?.handlers.ReceiveGroupMessage).toBeDefined());
    messages.getUnreadCount.mockClear();
    messages.getUnreadCount.mockResolvedValue(3);

    act(() => { chatHub().handlers.ReceiveGroupMessage({ messageId: 1, groupId: 2 }); });

    await waitFor(() => expect(screen.getByTestId('unread-messages')).toHaveTextContent('3'));
    expect(messages.getUnreadCount).toHaveBeenCalledWith(7);
  });

  it('refreshes for one-to-one messages too, and loads the count at the start', async () => {
    messages.getUnreadCount.mockResolvedValue(2);
    render(tree());

    await waitFor(() => expect(screen.getByTestId('unread-messages')).toHaveTextContent('2'));

    messages.getUnreadCount.mockResolvedValue(4);
    act(() => { chatHub().handlers.ReceiveMessage({ messageId: 9 }); });
    await waitFor(() => expect(screen.getByTestId('unread-messages')).toHaveTextContent('4'));
  });
});
