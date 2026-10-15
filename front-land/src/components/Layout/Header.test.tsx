import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import type { User } from '../../shared/types/user';

const session = vi.hoisted(() => ({ user: null as User | null, unreadMessages: 0, proposals: 0, billsToPay: 0, billsToConfirm: 0 }));

vi.mock('../../shared/context/AuthContext', () => ({
  useAuth: () => ({ user: session.user, isAuthenticated: session.user !== null, logout: vi.fn() }),
}));
vi.mock('../../shared/context/ThemeContext', () => ({ useTheme: () => ({ mode: 'light', toggleTheme: vi.fn() }) }));
vi.mock('../../shared/context/NotificationContext', () => ({
  useNotifications: () => ({ unreadCount: 0, unreadMessagesCount: session.unreadMessages }),
}));
vi.mock('../../hooks/useBillsSummary', () => ({
  useBillsSummary: () => ({ toPay: session.billsToPay, toConfirm: session.billsToConfirm, amountToPay: 0 }),
}));
vi.mock('../../hooks/useMyPairing', () => ({
  useMyPairing: () => ({ incoming: Array.from({ length: session.proposals }, (_, i) => ({ pairingId: i + 1 })) }),
}));
vi.mock('./LanguageSwitcher', () => ({ default: () => null }));
vi.mock('../Notification/NotificationPanel', () => ({ default: () => null }));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import Header from './Header';

const PICTURE = 'data:image/png;base64,AAAA';

const signedIn = (over: Partial<User> = {}): User => ({
  userId: 7, userGuid: 'g', firstName: 'Marko', lastName: 'Matovic', email: 'm@test.com', isActive: true, tokenBalance: 3, ...over,
});

const renderHeader = () => render(<MemoryRouter><Header /></MemoryRouter>);

beforeEach(() => {
  session.user = null;
  session.unreadMessages = 0;
  session.proposals = 0;
  session.billsToPay = 0;
  session.billsToConfirm = 0;
});

describe('Header - the profile button', () => {
  it("shows the user's own picture, not a generic icon", () => {
    session.user = signedIn({ profilePicture: PICTURE });

    renderHeader();

    const profileButton = screen.getByLabelText('common:profile');
    const photo = profileButton.querySelector('img');
    expect(photo).not.toBeNull();
    expect(photo).toHaveAttribute('src', PICTURE);
    expect(profileButton.querySelector('[data-testid="AccountCircleIcon"]')).toBeNull();
  });

  it('shows the initials while there is no picture (for instance until the profile has loaded)', () => {
    session.user = signedIn();

    renderHeader();

    const profileButton = screen.getByLabelText('common:profile');
    expect(profileButton.querySelector('img')).toBeNull();
    expect(profileButton).toHaveTextContent('MM');
  });

  it('is not there for a visitor who has not signed in', () => {
    renderHeader();

    expect(screen.queryByLabelText('common:profile')).not.toBeInTheDocument();
  });
});

describe('Header - the main navigation', () => {
  const navButton = (name: string) => screen.getByRole('button', { name: new RegExp(name) });

  it('has "my roommate" next to chat and roommates for somebody who is signed in', () => {
    session.user = signedIn();

    renderHeader();

    expect(navButton('pairing:pageTitle')).toBeInTheDocument();
    expect(navButton('roommates')).toBeInTheDocument();
  });

  it('does not offer "my roommate" to a visitor', () => {
    renderHeader();

    expect(screen.queryByRole('button', { name: /pairing:pageTitle/ })).not.toBeInTheDocument();
  });

  it('shows how many proposals to be roommates wait for an answer, like unread messages on chat', () => {
    session.user = signedIn();
    session.proposals = 2;
    session.unreadMessages = 5;

    renderHeader();

    expect(navButton('pairing:pageTitle')).toHaveTextContent('2');
    expect(navButton('chat:chat')).toHaveTextContent('5');
  });

  it('shows no number while nothing waits', () => {
    session.user = signedIn();

    renderHeader();

    expect(navButton('pairing:pageTitle')).not.toHaveTextContent(/\d/);
    expect(navButton('chat:chat')).not.toHaveTextContent(/\d/);
  });
});

describe('Header - administration', () => {
  const openMenu = async () => {
    const { fireEvent } = await import('@testing-library/react');
    fireEvent.click(screen.getByLabelText('common:moreOptions'));
  };

  it('offers the administration pages in a session confirmed with the authenticator app', async () => {
    session.user = signedIn({ roleName: 'Admin', twoFactorVerified: true });
    renderHeader();

    await openMenu();

    expect(screen.getByText('dashboard:analyticsDashboard')).toBeInTheDocument();
    expect(screen.queryByText('profile:twoFactorMenu')).not.toBeInTheDocument();
  });

  it('asks to turn on two-step sign-in instead, because the server refuses administration without it', async () => {
    session.user = signedIn({ roleName: 'Admin', twoFactorVerified: false });
    renderHeader();

    await openMenu();

    expect(screen.queryByText('dashboard:analyticsDashboard')).not.toBeInTheDocument();
    expect(screen.getByText('profile:twoFactorMenu')).toBeInTheDocument();
  });

  it('shows neither to other accounts', async () => {
    session.user = signedIn({ roleName: 'Tenant' });
    renderHeader();

    await openMenu();

    expect(screen.queryByText('dashboard:analyticsDashboard')).not.toBeInTheDocument();
    expect(screen.queryByText('profile:twoFactorMenu')).not.toBeInTheDocument();
  });
});

describe('Header - bills', () => {
  const navButton = (name: string) => screen.getByRole('button', { name: new RegExp(name) });

  it('counts the bills to pay and the payments to confirm next to "Bills"', () => {
    session.user = signedIn();
    session.billsToPay = 2;
    session.billsToConfirm = 1;

    renderHeader();

    expect(navButton('bills:navTitle')).toHaveTextContent('3');
  });

  it('is not offered to visitors who are not signed in', () => {
    renderHeader();

    expect(screen.queryByRole('button', { name: /bills:navTitle/ })).not.toBeInTheDocument();
  });
});
