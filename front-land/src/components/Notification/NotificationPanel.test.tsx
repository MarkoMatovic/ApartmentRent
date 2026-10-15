import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';

const ctx = vi.hoisted(() => ({
  notifications: [] as Array<{
    id: string; title: string; message: string; type: 'info'; timestamp: Date; read: boolean;
    descriptor?: { key?: string; params?: Record<string, string>; link?: string };
  }>,
  markAsRead: vi.fn(),
  markAllAsRead: vi.fn(),
  clearNotification: vi.fn(),
}));
const navigate = vi.hoisted(() => vi.fn());

vi.mock('../../shared/context/NotificationContext', () => ({
  useNotifications: () => ({
    notifications: ctx.notifications,
    unreadCount: ctx.notifications.filter(n => !n.read).length,
    markAsRead: ctx.markAsRead,
    markAllAsRead: ctx.markAllAsRead,
    clearNotification: ctx.clearNotification,
  }),
}));
vi.mock('react-router-dom', () => ({ useNavigate: () => navigate }));
// The translation of a key is "<language>:<key>:<name>", the default is the server text.
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    i18n: { language: 'sr' },
    t: (key: string, options?: { defaultValue?: string; name?: string }) => {
      if (key.startsWith('notifications:events.')) {
        return key === 'notifications:events.pairing_accepted.title' || key === 'notifications:events.pairing_accepted.message'
          ? `EN ${key.split('.').pop()} ${options?.name}`
          : options?.defaultValue;
      }
      return key;
    },
  }),
}));

import NotificationPanel from './NotificationPanel';

const item = (over: Partial<(typeof ctx.notifications)[number]> = {}) => ({
  id: '1', title: 'Cimerstvo potvrđeno', message: 'Marko je prihvatio/la.', type: 'info' as const,
  timestamp: new Date('2026-10-13T10:00:00Z'), read: false, ...over,
});

const renderPanel = (onClose = vi.fn()) => {
  const anchor = document.createElement('div');
  document.body.appendChild(anchor);
  render(<NotificationPanel anchorEl={anchor} open onClose={onClose} />);
  return onClose;
};

beforeEach(() => {
  vi.clearAllMocks();
  ctx.notifications = [];
});

describe('NotificationPanel', () => {
  it('shows the translation of a notification that has a key, not the Serbian text from the server', () => {
    ctx.notifications = [item({ descriptor: { key: 'pairing_accepted', params: { name: 'Marko' } } })];

    renderPanel();

    expect(screen.getByText('EN title Marko')).toBeInTheDocument();
    expect(screen.getByText('EN message Marko')).toBeInTheDocument();
    expect(screen.queryByText('Cimerstvo potvrđeno')).not.toBeInTheDocument();
  });

  it('shows the server text when the key has no translation', () => {
    ctx.notifications = [item({ descriptor: { key: 'unknown_event' } })];

    renderPanel();

    expect(screen.getByText('Cimerstvo potvrđeno')).toBeInTheDocument();
    expect(screen.getByText('Marko je prihvatio/la.')).toBeInTheDocument();
  });

  it('shows the plain text of a notification without a descriptor', () => {
    ctx.notifications = [item()];

    renderPanel();

    expect(screen.getByText('Cimerstvo potvrđeno')).toBeInTheDocument();
  });

  it('opens the page of a notification with a route when it is clicked, marks it read and closes the bell', () => {
    ctx.notifications = [item({ descriptor: { key: 'pairing_accepted', params: { name: 'Marko' }, link: '/my-roommate' } })];
    const onClose = renderPanel();

    fireEvent.click(screen.getByText('EN title Marko'));

    expect(navigate).toHaveBeenCalledWith('/my-roommate');
    expect(ctx.markAsRead).toHaveBeenCalledWith('1');
    expect(onClose).toHaveBeenCalled();
  });

  it('opens it from the keyboard too', () => {
    ctx.notifications = [item({ descriptor: { link: '/chat?userId=5' } })];
    renderPanel();

    fireEvent.keyDown(screen.getByRole('link'), { key: 'Enter' });

    expect(navigate).toHaveBeenCalledWith('/chat?userId=5');
  });

  it('does nothing when a notification without a route is clicked', () => {
    ctx.notifications = [item()];
    const onClose = renderPanel();

    fireEvent.click(screen.getByText('Cimerstvo potvrđeno'));

    expect(navigate).not.toHaveBeenCalled();
    expect(onClose).not.toHaveBeenCalled();
    expect(screen.queryByRole('link')).not.toBeInTheDocument();
  });

  it('dismissing a notification does not open its page', () => {
    ctx.notifications = [item({ descriptor: { link: '/my-roommate' } })];
    renderPanel();

    fireEvent.click(screen.getByLabelText('common:close'));

    expect(navigate).not.toHaveBeenCalled();
    expect(ctx.markAsRead).toHaveBeenCalledWith('1'); // the existing behaviour of the X on an unread item
  });
});
