import { beforeEach, describe, expect, it, vi } from 'vitest';
import { act, renderHook } from '@testing-library/react';

type Handler = (...args: unknown[]) => void;
const live = vi.hoisted(() => ({
  handlers: {} as Record<string, Handler>,
  invoked: [] as Array<[string, ...unknown[]]>,
  connected: true,
  reconnects: 0,
}));
const built = vi.hoisted(() => ({ count: 0 }));

const connection = {
  on: (name: string, handler: Handler) => { live.handlers[name] = handler; },
  off: (name: string, handler: Handler) => { if (live.handlers[name] === handler) delete live.handlers[name]; },
  invoke: (method: string, ...args: unknown[]) => { live.invoked.push([method, ...args]); return Promise.resolve(); },
};

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: class { constructor() { built.count++; } },
}));
vi.mock('../shared/context/NotificationContext', () => ({
  useNotifications: () => ({ liveConnection: connection, liveConnected: live.connected, liveReconnects: live.reconnects }),
}));

import { useChatSignalR } from './useChatSignalR';

beforeEach(() => {
  live.handlers = {};
  live.invoked = [];
  live.connected = true;
  live.reconnects = 0;
  built.count = 0;
});

describe('useChatSignalR', () => {
  it('uses the tab\'s connection instead of opening its own', () => {
    renderHook(() => useChatSignalR(7));

    expect(built.count).toBe(0);
    expect(Object.keys(live.handlers)).toEqual(
      expect.arrayContaining(['ReceiveMessage', 'MessageSent', 'ReceiveGroupMessage', 'GroupCreated', 'MessageRead', 'MessagesRead', 'UserTyping']));
  });

  it('takes its handlers off the connection when the page closes, and leaves the connection open', () => {
    const view = renderHook(() => useChatSignalR(7));

    view.unmount();

    expect(live.handlers).toEqual({});
  });

  it('shows a message that arrives on the shared connection', () => {
    const { result } = renderHook(() => useChatSignalR(7));

    act(() => live.handlers.ReceiveMessage({ messageId: 3, messageText: 'Zdravo' }));

    expect(result.current.newMessage).toMatchObject({ messageId: 3 });
  });

  it('sends a message through the hub while connected, and leaves it to REST while not', async () => {
    const { result, rerender } = renderHook(() => useChatSignalR(7));
    await expect(result.current.sendMessage(2, 'Zdravo')).resolves.toBe(true);
    expect(live.invoked).toContainEqual(['SendMessage', 2, 'Zdravo']);

    live.connected = false;
    rerender();

    await expect(result.current.sendMessage(2, 'Opet')).resolves.toBe(false);
  });

  it('tells the hub only who is being typed to (the hub takes one argument)', async () => {
    const { result } = renderHook(() => useChatSignalR(7));

    await result.current.notifyTyping(7, 2);

    expect(live.invoked).toContainEqual(['UserTyping', 2]);
  });

  it('reloads the groups after the connection comes back', () => {
    const { result, rerender } = renderHook(() => useChatSignalR(7));
    const before = result.current.groupsChanged;

    live.reconnects = 1;
    rerender();

    expect(result.current.groupsChanged).toBe(before + 1);
  });
});
