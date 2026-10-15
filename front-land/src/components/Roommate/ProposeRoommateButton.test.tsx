import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { Pairing } from '../../shared/api/roommatePairing';

const api = vi.hoisted(() => ({ propose: vi.fn(), accept: vi.fn() }));
const pairing = vi.hoisted(() => ({
  state: { active: null, incoming: [], outgoing: [], refresh: vi.fn() } as {
    active: Pairing | null; incoming: Pairing[]; outgoing: Pairing[]; refresh: () => void;
  },
}));
const notify = vi.hoisted(() => ({ addNotification: vi.fn() }));
const auth = vi.hoisted(() => ({ user: { userId: 1 } as { userId: number } | null }));

vi.mock('../../shared/api/roommatePairing', () => ({ roommatePairingApi: api }));
vi.mock('../../hooks/useMyPairing', () => ({ useMyPairing: () => pairing.state }));
vi.mock('../../shared/context/AuthContext', () => ({ useAuth: () => ({ user: auth.user }) }));
vi.mock('../../shared/context/NotificationContext', () => ({ useNotifications: () => notify }));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, options?: { name?: string }) => (options?.name ? `${key}:${options.name}` : key),
  }),
}));

import ProposeRoommateButton from './ProposeRoommateButton';

const pair = (over: Partial<Pairing>): Pairing => ({
  pairingId: 5, status: 'Pending', partnerUserId: 2, partnerName: 'Marko Marković',
  isIncoming: false, createdDate: new Date().toISOString(), ...over,
});

const renderButton = (props: Partial<React.ComponentProps<typeof ProposeRoommateButton>> = {}) =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <ProposeRoommateButton userId={2} firstName="Marko" {...props} />
    </QueryClientProvider>,
  );

beforeEach(() => {
  vi.clearAllMocks();
  auth.user = { userId: 1 };
  pairing.state = { active: null, incoming: [], outgoing: [], refresh: vi.fn() };
});

describe('ProposeRoommateButton', () => {
  it('offers to propose and sends the proposal', async () => {
    api.propose.mockResolvedValue(pair({}));

    renderButton();
    fireEvent.click(screen.getByText('proposeButton'));

    await waitFor(() => expect(api.propose).toHaveBeenCalledWith(2));
    await waitFor(() => expect(notify.addNotification).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'proposalSent', type: 'success' })));
    expect(pairing.state.refresh).toHaveBeenCalled();
  });

  it('says so when proposing back accepted their earlier proposal at once', async () => {
    api.propose.mockResolvedValue(pair({ status: 'Accepted' }));

    renderButton();
    fireEvent.click(screen.getByText('proposeButton'));

    await waitFor(() => expect(notify.addNotification).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'pairingConfirmed' })));
  });

  it('shows why it failed, in the language of the user', async () => {
    api.propose.mockRejectedValue({ response: { status: 422, data: { code: 'ROOMMATE_ALREADY_TAKEN' } } });

    renderButton();
    fireEvent.click(screen.getByText('proposeButton'));

    await waitFor(() => expect(notify.addNotification).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'error', message: 'validation:ROOMMATE_ALREADY_TAKEN' })));
  });

  it('shows nothing on my own profile or when signed out', () => {
    const own = renderButton({ userId: 1 });
    expect(own.container).toBeEmptyDOMElement();
    own.unmount();

    auth.user = null;
    const out = renderButton();
    expect(out.container).toBeEmptyDOMElement();
  });

  it('does not offer anything for a person who is already taken', () => {
    renderButton({ isTaken: true });

    expect(screen.getByText('takenHint')).toBeInTheDocument();
    expect(screen.queryByText('proposeButton')).not.toBeInTheDocument();
  });

  it('tells someone who already has a roommate, and recognises that roommate', () => {
    pairing.state.active = pair({ status: 'Accepted', partnerUserId: 9 });
    const other = renderButton();
    expect(other.getByText('youAreTaken')).toBeInTheDocument();
    other.unmount();

    pairing.state.active = pair({ status: 'Accepted', partnerUserId: 2 });
    renderButton();
    expect(screen.getByText('activeTitle')).toBeInTheDocument();
    expect(screen.queryByText('youAreTaken')).not.toBeInTheDocument();
  });

  it('offers to accept when they already proposed to me', async () => {
    pairing.state.incoming = [pair({ pairingId: 7, isIncoming: true })];
    api.accept.mockResolvedValue(pair({ pairingId: 7, status: 'Accepted' }));

    renderButton();
    expect(screen.getByText('proposesToYou:Marko')).toBeInTheDocument();
    fireEvent.click(screen.getByText('accept'));

    await waitFor(() => expect(api.accept).toHaveBeenCalledWith(7));
  });

  describe('as a small action on a card', () => {
    it('proposes with one click', async () => {
      api.propose.mockResolvedValue(pair({}));

      renderButton({ compact: true });
      fireEvent.click(screen.getByText('proposeButton'));

      await waitFor(() => expect(api.propose).toHaveBeenCalledWith(2));
    });

    it('offers to accept a proposal that waits for me', async () => {
      pairing.state.incoming = [pair({ pairingId: 9, isIncoming: true })];
      api.accept.mockResolvedValue(pair({ pairingId: 9, status: 'Accepted' }));

      renderButton({ compact: true });
      fireEvent.click(screen.getByText('acceptProposal'));

      await waitFor(() => expect(api.accept).toHaveBeenCalledWith(9));
    });

    it('shows that the proposal was sent, and cannot be sent again', () => {
      pairing.state.outgoing = [pair({})];

      renderButton({ compact: true });

      expect(screen.getByText('proposalSent').closest('button')).toBeDisabled();
    });

    it('says nothing for a taken person, for my own roommate, or when I have one', () => {
      const taken = renderButton({ compact: true, isTaken: true });
      expect(taken.container).toBeEmptyDOMElement();
      taken.unmount();

      pairing.state.active = pair({ status: 'Accepted', partnerUserId: 2 });
      const mine = renderButton({ compact: true });
      expect(mine.container).toBeEmptyDOMElement();
      mine.unmount();

      pairing.state.active = pair({ status: 'Accepted', partnerUserId: 9 });
      expect(renderButton({ compact: true }).container).toBeEmptyDOMElement();
    });
  });

  it('shows a disabled waiting button after I proposed', () => {
    pairing.state.outgoing = [pair({})];

    renderButton();

    expect(screen.getByText('waitingFor:Marko').closest('button')).toBeDisabled();
    expect(api.propose).not.toHaveBeenCalled();
  });
});
