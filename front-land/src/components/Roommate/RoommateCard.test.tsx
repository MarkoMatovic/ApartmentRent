import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { Pairing } from '../../shared/api/roommatePairing';
import type { Roommate } from '../../shared/types/roommate';

const api = vi.hoisted(() => ({ propose: vi.fn(), accept: vi.fn() }));
const pairing = vi.hoisted(() => ({
  state: { active: null, incoming: [], outgoing: [], refresh: vi.fn() } as {
    active: Pairing | null; incoming: Pairing[]; outgoing: Pairing[]; refresh: () => void;
  },
}));

vi.mock('../../shared/api/roommatePairing', () => ({ roommatePairingApi: api }));
vi.mock('../../hooks/useMyPairing', () => ({ useMyPairing: () => pairing.state }));
vi.mock('../../shared/context/AuthContext', () => ({ useAuth: () => ({ user: { userId: 1 } }) }));
vi.mock('../../shared/context/NotificationContext', () => ({ useNotifications: () => ({ addNotification: vi.fn() }) }));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import RoommateCard from './RoommateCard';

const roommate = (over: Partial<Roommate> = {}): Roommate => ({
  roommateId: 12, userId: 34, firstName: 'Jovana', lastName: 'Jović', ...over,
});

const renderCard = (props: Partial<React.ComponentProps<typeof RoommateCard>> = {}) =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter>
        <RoommateCard roommate={roommate()} {...props} />
      </MemoryRouter>
    </QueryClientProvider>,
  );

beforeEach(() => {
  vi.clearAllMocks();
  pairing.state = { active: null, incoming: [], outgoing: [], refresh: vi.fn() };
});

describe('RoommateCard - proposing to be roommates', () => {
  it('lets me propose straight from the list, without opening the profile', async () => {
    api.propose.mockResolvedValue({ pairingId: 1, status: 'Pending' });

    renderCard();
    fireEvent.click(screen.getByText('proposeButton'));

    await waitFor(() => expect(api.propose).toHaveBeenCalledWith(34));
  });

  it('keeps the button outside the clickable area that opens the profile', () => {
    renderCard();

    const button = screen.getByText('proposeButton').closest('button')!;
    expect(button.closest('.MuiCardActionArea-root')).toBeNull();
  });

  it('has no button on my own card', () => {
    renderCard({ isOwn: true });

    expect(screen.queryByText('proposeButton')).not.toBeInTheDocument();
  });

  it('marks a person who is taken, and offers nothing for them', () => {
    renderCard({ roommate: roommate({ isTaken: true }) });

    expect(screen.getByText('pairing:taken')).toBeInTheDocument();
    expect(screen.queryByText('proposeButton')).not.toBeInTheDocument();
  });

  it('offers nothing to somebody who already has a roommate', () => {
    pairing.state.active = {
      pairingId: 3, status: 'Accepted', partnerUserId: 99, partnerName: 'Neko', isIncoming: false, createdDate: '2026-10-01T10:00:00Z',
    };

    renderCard();

    expect(screen.queryByText('proposeButton')).not.toBeInTheDocument();
  });
});

describe('RoommateCard - badges', () => {
  it('puts "my profile" and "taken" in the top-right corner of the card, not over the photo', () => {
    const { container } = renderCard({ isOwn: true, roommate: roommate({ isTaken: true, profilePicture: 'data:image/png;base64,AAAA' }) });

    const badges = container.querySelector('[data-card-badges]') as HTMLElement;
    expect(badges).not.toBeNull();
    expect(badges).toHaveTextContent('myProfileBadge');
    expect(badges).toHaveTextContent('pairing:taken');

    const style = getComputedStyle(badges);
    expect(style.position).toBe('absolute');
    expect(style.right).toBe('10px');
    expect(style.left).not.toBe('10px');

    // and not inside the header that holds the photo
    const avatar = container.querySelector('.MuiAvatar-root')!;
    expect(avatar.parentElement!.contains(badges)).toBe(false);
  });

  it('keeps the match score in the same corner', () => {
    const { container } = renderCard({ matchScore: 82 });

    expect(container.querySelector('[data-card-badges]')).toHaveTextContent('82%');
  });

  it('has no badge corner when there is nothing to say', () => {
    const { container } = renderCard();

    expect(container.querySelector('[data-card-badges]')).toBeNull();
  });
});
