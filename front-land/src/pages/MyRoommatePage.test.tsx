import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import type { Pairing, SharedApartment } from '../shared/api/roommatePairing';

const api = vi.hoisted(() => ({
  accept: vi.fn(), decline: vi.fn(), cancel: vi.fn(), end: vi.fn(),
  getSharedApartments: vi.fn(), removeSharedApartment: vi.fn(),
}));
const pairing = vi.hoisted(() => ({
  state: {
    active: null, incoming: [], outgoing: [], isLoading: false, isError: false, refresh: vi.fn(),
  } as {
    active: Pairing | null; incoming: Pairing[]; outgoing: Pairing[]; isLoading: boolean; isError: boolean; refresh: () => void;
  },
}));
const notify = vi.hoisted(() => ({ addNotification: vi.fn() }));
const navigate = vi.hoisted(() => vi.fn());
const groups = vi.hoisted(() => ({ open: vi.fn() }));

vi.mock('../shared/api/roommatePairing', () => ({ roommatePairingApi: api }));
vi.mock('../shared/api/chatGroups', () => ({ chatGroupsApi: groups }));
vi.mock('../hooks/useMyPairing', () => ({ useMyPairing: () => pairing.state }));
vi.mock('../shared/context/NotificationContext', () => ({ useNotifications: () => notify }));
vi.mock('react-router-dom', async (importOriginal) => ({
  ...(await importOriginal<typeof import('react-router-dom')>()),
  useNavigate: () => navigate,
}));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, options?: Record<string, string | number>) =>
      options ? `${key}:${Object.values(options).join('|')}` : key,
    i18n: { language: 'sr' },
  }),
}));

import MyRoommatePage from './MyRoommatePage';

const pair = (over: Partial<Pairing> = {}): Pairing => ({
  pairingId: 5, status: 'Pending', partnerUserId: 2, partnerName: 'Marko Marković',
  isIncoming: true, createdDate: '2026-10-01T10:00:00Z', ...over,
});

const apartment = (over: Partial<SharedApartment> = {}): SharedApartment => ({
  apartmentId: 40, title: 'Stan na Zvezdari', city: 'Beograd', address: 'Ulica 1', rent: 450, isAvailable: true,
  addedByUserId: 2, addedByName: 'Marko Marković', addedDate: '2026-10-02T10:00:00Z', ...over,
});

const renderPage = () =>
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter><MyRoommatePage /></MemoryRouter>
    </QueryClientProvider>,
  );

beforeEach(() => {
  vi.clearAllMocks();
  pairing.state = { active: null, incoming: [], outgoing: [], isLoading: false, isError: false, refresh: vi.fn() };
  api.getSharedApartments.mockResolvedValue([]);
});

describe('MyRoommatePage', () => {
  it('explains what a roommate agreement gives, and links to the search, when there is none', () => {
    renderPage();

    expect(screen.getByText('noRoommateTitle')).toBeInTheDocument();
    fireEvent.click(screen.getByText('findRoommate'));
    expect(navigate).toHaveBeenCalledWith('/roommates');
  });

  it('lets me accept or decline a proposal I received', async () => {
    pairing.state.incoming = [pair({ pairingId: 7 }), pair({ pairingId: 8, partnerName: 'Jovana Jović', partnerUserId: 3 })];
    api.accept.mockResolvedValue(pair({ status: 'Accepted' }));
    api.decline.mockResolvedValue(undefined);

    renderPage();
    expect(screen.getByText('proposesToYou:Marko Marković')).toBeInTheDocument();

    fireEvent.click(screen.getAllByText('accept')[0]);
    await waitFor(() => expect(api.accept).toHaveBeenCalledWith(7));
    expect(pairing.state.refresh).toHaveBeenCalled();

    fireEvent.click(screen.getAllByText('decline')[1]);
    await waitFor(() => expect(api.decline).toHaveBeenCalledWith(8));
  });

  it('lets me withdraw a proposal I sent', async () => {
    pairing.state.outgoing = [pair({ pairingId: 9, isIncoming: false })];
    api.cancel.mockResolvedValue(undefined);

    renderPage();
    expect(screen.getByText('waitingFor:Marko Marković')).toBeInTheDocument();
    fireEvent.click(screen.getByText('withdraw'));

    await waitFor(() => expect(api.cancel).toHaveBeenCalledWith(9));
  });

  it('shows the roommate, the shared apartments and who added each', async () => {
    pairing.state.active = pair({ status: 'Accepted', isIncoming: false });
    api.getSharedApartments.mockResolvedValue([
      apartment({ note: 'Blizu posla' }),
      apartment({ apartmentId: 41, title: 'Garsonjera', isAvailable: false }),
    ]);

    renderPage();

    expect(screen.getByText('Marko Marković')).toBeInTheDocument();
    expect(screen.getByText('taken')).toBeInTheDocument();
    expect(await screen.findByText('Stan na Zvezdari')).toBeInTheDocument();
    expect(screen.getByText(/Blizu posla/)).toBeInTheDocument();
    expect(screen.getAllByText('addedBy:Marko Marković')).toHaveLength(2);
    // a listing that is gone is marked, and cannot be opened
    expect(screen.getByText('Garsonjera')).toBeInTheDocument();
    expect(screen.getAllByText('listingUnavailable').length).toBeGreaterThan(0);
    expect(screen.getAllByLabelText('openListing')).toHaveLength(1);
  });

  it('opens a shared apartment and removes one from the list', async () => {
    pairing.state.active = pair({ status: 'Accepted', isIncoming: false });
    api.getSharedApartments.mockResolvedValue([apartment()]);
    api.removeSharedApartment.mockResolvedValue(undefined);

    renderPage();
    await screen.findByText('Stan na Zvezdari');

    fireEvent.click(screen.getByLabelText('openListing'));
    expect(navigate).toHaveBeenCalledWith('/apartments/40');

    fireEvent.click(screen.getByLabelText('removeFromList'));
    await waitFor(() => expect(api.removeSharedApartment).toHaveBeenCalledWith(40));
  });

  it('opens a shared conversation with the landlord of a listed apartment, and goes to it', async () => {
    pairing.state.active = pair({ status: 'Accepted', isIncoming: false });
    api.getSharedApartments.mockResolvedValue([apartment({ landlordUserId: 77, landlordName: 'Lazar Lazić' })]);
    groups.open.mockResolvedValue({ groupId: 31, members: [], unreadCount: 0 });

    renderPage();
    fireEvent.click(await screen.findByText('talkToLandlordNamed:Lazar Lazić'));

    await waitFor(() => expect(groups.open).toHaveBeenCalledWith(77));
    await waitFor(() => expect(navigate).toHaveBeenCalledWith('/chat?group=31'));
  });

  it('has no conversation button for a listing that is gone, or when the landlord is unknown', async () => {
    pairing.state.active = pair({ status: 'Accepted', isIncoming: false });
    api.getSharedApartments.mockResolvedValue([
      apartment({ apartmentId: 1, title: 'Obrisan', isAvailable: false, landlordUserId: 77, landlordName: 'Lazar' }),
      apartment({ apartmentId: 2, title: 'Bez stanodavca', landlordUserId: null }),
    ]);

    renderPage();
    await screen.findByText('Bez stanodavca');

    expect(screen.queryByText(/talkToLandlord/)).not.toBeInTheDocument();
  });

  it('says why the conversation could not be opened', async () => {
    pairing.state.active = pair({ status: 'Accepted', isIncoming: false });
    api.getSharedApartments.mockResolvedValue([apartment({ landlordUserId: 77, landlordName: 'Lazar' })]);
    groups.open.mockRejectedValue({ response: { status: 422, data: { code: 'MESSAGING_NOT_ALLOWED' } } });

    renderPage();
    fireEvent.click(await screen.findByText('talkToLandlordNamed:Lazar'));

    await waitFor(() => expect(notify.addNotification).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'error', message: expect.stringContaining('MESSAGING_NOT_ALLOWED') })));
    expect(navigate).not.toHaveBeenCalledWith(expect.stringContaining('/chat?group'));
  });

  it('shows an empty-list hint when nothing was shared yet', async () => {
    pairing.state.active = pair({ status: 'Accepted', isIncoming: false });

    renderPage();

    expect(await screen.findByText('sharedEmpty')).toBeInTheDocument();
  });

  it('asks before ending the roommate agreement', async () => {
    pairing.state.active = pair({ pairingId: 11, status: 'Accepted', isIncoming: false });
    api.end.mockResolvedValue(undefined);

    renderPage();
    fireEvent.click(screen.getByText('endPairing'));
    expect(api.end).not.toHaveBeenCalled();
    expect(await screen.findByText('endConfirmText')).toBeInTheDocument();

    fireEvent.click(screen.getAllByText('endPairing').pop()!);
    await waitFor(() => expect(api.end).toHaveBeenCalledWith(11));
  });

  it('keeps the agreement when the user changes their mind', async () => {
    pairing.state.active = pair({ pairingId: 11, status: 'Accepted', isIncoming: false });

    renderPage();
    fireEvent.click(screen.getByText('endPairing'));
    fireEvent.click(await screen.findByText('keepPairing'));

    expect(api.end).not.toHaveBeenCalled();
  });

  it('reports a failed answer in the user language', async () => {
    pairing.state.incoming = [pair({ pairingId: 7 })];
    api.accept.mockRejectedValue({ response: { status: 422, data: { code: 'YOU_ALREADY_HAVE_ROOMMATE' } } });

    renderPage();
    fireEvent.click(screen.getByText('accept'));

    await waitFor(() => expect(notify.addNotification).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'error', message: expect.stringContaining('YOU_ALREADY_HAVE_ROOMMATE') })));
  });
});
