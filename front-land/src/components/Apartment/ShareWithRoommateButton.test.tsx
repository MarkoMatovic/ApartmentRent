import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { Pairing, SharedApartment } from '../../shared/api/roommatePairing';

const api = vi.hoisted(() => ({ getSharedApartments: vi.fn(), shareApartment: vi.fn() }));
const pairing = vi.hoisted(() => ({ partner: null as Pairing | null }));
const notify = vi.hoisted(() => ({ addNotification: vi.fn() }));

vi.mock('../../shared/api/roommatePairing', () => ({ roommatePairingApi: api }));
vi.mock('../../hooks/useMyPairing', () => ({ useMyPairing: () => ({ partner: pairing.partner }) }));
vi.mock('../../shared/context/NotificationContext', () => ({ useNotifications: () => notify }));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, options?: { name?: string }) => (options?.name ? `${key}:${options.name}` : key),
  }),
}));

import ShareWithRoommateButton from './ShareWithRoommateButton';

const partner: Pairing = {
  pairingId: 5, status: 'Accepted', partnerUserId: 2, partnerName: 'Marko Marković',
  isIncoming: false, createdDate: '2026-10-01T10:00:00Z',
};
const shared = (apartmentId: number): SharedApartment => ({
  apartmentId, title: 'Stan', rent: 400, isAvailable: true, addedByUserId: 1, addedByName: 'Ana', addedDate: '2026-10-02T10:00:00Z',
});

const renderButton = () =>
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <ShareWithRoommateButton apartmentId={40} />
    </QueryClientProvider>,
  );

beforeEach(() => {
  vi.clearAllMocks();
  pairing.partner = partner;
  api.getSharedApartments.mockResolvedValue([]);
});

describe('ShareWithRoommateButton', () => {
  it('is not shown to someone without a confirmed roommate', () => {
    pairing.partner = null;

    const { container } = renderButton();

    expect(container).toBeEmptyDOMElement();
    expect(api.getSharedApartments).not.toHaveBeenCalled();
  });

  it('sends the apartment with an optional note and tells the user', async () => {
    api.shareApartment.mockResolvedValue(shared(40));

    renderButton();
    fireEvent.click(await screen.findByText('sendToRoommate'));
    expect(screen.getByText('sendDialogTitle:Marko Marković')).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('noteLabel'), { target: { value: 'Blizu posla' } });
    fireEvent.click(screen.getByText('sendConfirm'));

    await waitFor(() => expect(api.shareApartment).toHaveBeenCalledWith(40, 'Blizu posla'));
    await waitFor(() => expect(notify.addNotification).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'apartmentShared', type: 'success' })));
  });

  it('shows that the apartment is already on the shared list, and cannot be sent twice', async () => {
    api.getSharedApartments.mockResolvedValue([shared(40)]);

    renderButton();

    const button = (await screen.findByText('onSharedList')).closest('button');
    expect(button).toBeDisabled();
  });

  it('explains a refusal from the server', async () => {
    api.shareApartment.mockRejectedValue({ response: { status: 422, data: { code: 'PAIRING_REQUIRED' } } });

    renderButton();
    fireEvent.click(await screen.findByText('sendToRoommate'));
    fireEvent.click(screen.getByText('sendConfirm'));

    await waitFor(() => expect(notify.addNotification).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'error', message: 'validation:PAIRING_REQUIRED' })));
  });
});
