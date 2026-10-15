import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import type { Bill, MyBills, Tenancy } from '../shared/api/bills';

const api = vi.hoisted(() => ({
  getMine: vi.fn(), getTenancies: vi.fn(), getSharePayment: vi.fn(), markSharePaid: vi.fn(), unmarkShare: vi.fn(),
  confirmShare: vi.fn(), rejectShare: vi.fn(), getCompanyPayment: vi.fn(), payCompanyBill: vi.fn(), getProfile: vi.fn(),
  createTenancy: vi.fn(), getCandidates: vi.fn(),
}));
const nav = vi.hoisted(() => ({ navigate: vi.fn() }));

vi.mock('../shared/api/bills', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../shared/api/bills')>()),
  billsApi: api,
}));
vi.mock('../shared/api/apartments', () => ({ apartmentsApi: { getMyApartments: vi.fn().mockResolvedValue([]) } }));
vi.mock('../shared/context/AuthContext', () => ({ useAuth: () => ({ user: { userId: 2, firstName: 'Ana', lastName: 'Anić' } }) }));
vi.mock('../shared/context/NotificationContext', () => ({ useNotifications: () => ({ addNotification: vi.fn() }) }));
vi.mock('react-router-dom', async (importOriginal) => ({
  ...(await importOriginal<typeof import('react-router-dom')>()),
  useNavigate: () => nav.navigate,
}));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, options?: Record<string, unknown>) => (options?.name ? `${key}:${options.name}` : key),
    i18n: { language: 'sr' },
  }),
}));

import BillsPage from './BillsPage';

const person = (userId: number, name: string) => ({ userId, name });

const rent = (over: Partial<Bill> = {}): Bill => ({
  billId: 10, tenancyId: 5, apartmentTitle: 'Stan na Vračaru', tenantCount: 2, kind: 'rent', title: 'Kirija 10/2026',
  amount: 30000, dueDate: '2026-10-20', period: '2026-10', payTo: 'landlord', payeeName: 'Lazar Lazić',
  payeeAccount: '160-0000000123456-78', paymentCode: '289', purpose: 'Kirija 10/2026',
  createdBy: person(1, 'Lazar Lazić'), createdDate: '2026-10-10T08:00:00Z', status: 'open', isOverdue: false,
  canPayCompany: false, canUndoCompanyPayment: false, canEdit: false, shares: [], ...over,
});

const mine = (over: Partial<MyBills> = {}): MyBills => ({
  toPay: [{ type: 'share', billShareId: 101, bill: rent(), amount: 15000, receiver: person(1, 'Lazar Lazić'), status: 'unpaid' }],
  toConfirm: [],
  ...over,
});

const tenancy: Tenancy = {
  tenancyId: 5, apartmentId: 3, apartmentTitle: 'Stan na Vračaru', landlord: person(1, 'Lazar Lazić'), isLandlord: false,
  monthlyRent: 30000, rentDueDay: 20, payeeName: 'Lazar Lazić', payeeAccount: '160-0000000123456-78',
  startDate: '2026-10-01', status: 'active', members: [], openBills: 1,
};

const renderPage = (url = '/bills') => render(
  <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
    <MemoryRouter initialEntries={[url]}><BillsPage /></MemoryRouter>
  </QueryClientProvider>,
);

beforeEach(() => {
  vi.clearAllMocks();
  api.getMine.mockResolvedValue(mine());
  api.getTenancies.mockResolvedValue([tenancy]);
  api.getSharePayment.mockResolvedValue({
    payeeName: 'Lazar Lazić', payeeAccount: '160-0000000123456-78', amount: 15000, currency: 'RSD', paymentCode: '289',
    purpose: 'Kirija 10/2026', qrPayload: 'K:PR', qrCodeDataUrl: 'data:image/png;base64,AA==',
  });
  api.markSharePaid.mockResolvedValue(rent());
  api.confirmShare.mockResolvedValue(rent());
  api.rejectShare.mockResolvedValue(rent());
  api.unmarkShare.mockResolvedValue(rent());
});

describe('BillsPage', () => {
  it('lists what to pay with the rent named after its month, and the tenancies', async () => {
    renderPage();

    const item = await screen.findByTestId('to-pay-item');
    expect(within(item).getByText('bills:rentFor')).toBeInTheDocument();
    expect(within(item).getByText('15.000,00 RSD')).toBeInTheDocument();
    expect(within(item).getByText(/bills:receiverLine:Lazar Lazić/)).toBeInTheDocument();
    expect(await screen.findByText('bills:landlordLine:Lazar Lazić')).toBeInTheDocument();
  });

  it('pays a part with the QR code and then marks it paid', async () => {
    renderPage();

    fireEvent.click(await screen.findByRole('button', { name: 'bills:pay' }));
    expect(await screen.findByTestId('ips-qr')).toBeInTheDocument();
    expect(api.getSharePayment).toHaveBeenCalledWith(101);
    fireEvent.click(screen.getByRole('button', { name: 'bills:iPaid' }));

    await waitFor(() => expect(api.markSharePaid).toHaveBeenCalledWith(101));
  });

  it('a part marked paid waits for the receiver and can be taken back', async () => {
    api.getMine.mockResolvedValue(mine({
      toPay: [{ type: 'share', billShareId: 101, bill: rent(), amount: 15000, receiver: person(1, 'Lazar Lazić'), status: 'marked_paid' }],
    }));
    renderPage();

    expect(await screen.findByText('bills:markedPaidWaiting')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'bills:undoPaid' }));

    await waitFor(() => expect(api.unmarkShare).toHaveBeenCalledWith(101));
  });

  it('shows payments to confirm with "received" and "not received"', async () => {
    api.getMine.mockResolvedValue(mine({
      toPay: [],
      toConfirm: [{
        share: {
          billShareId: 202, payer: person(3, 'Marko Marković'), receiver: person(2, 'Ana Anić'), amount: 3000,
          status: 'marked_paid', canMarkPaid: false, canConfirm: true,
        },
        bill: rent({ kind: 'electricity', title: 'Struja', period: null, payTo: 'company' }),
      }],
    }));
    renderPage();

    const item = await screen.findByTestId('to-confirm-item');
    fireEvent.click(within(item).getByRole('button', { name: 'bills:received' }));
    await waitFor(() => expect(api.confirmShare).toHaveBeenCalledWith(202));
    fireEvent.click(within(item).getByRole('button', { name: 'bills:notReceived' }));
    await waitFor(() => expect(api.rejectShare).toHaveBeenCalledWith(202));
  });

  it('says when there is nothing to pay', async () => {
    api.getMine.mockResolvedValue(mine({ toPay: [] }));
    renderPage();

    expect(await screen.findByText('bills:toPayEmpty')).toBeInTheDocument();
  });

  it('opens a tenancy from its card', async () => {
    renderPage();

    // the apartment title appears on the bill too; the tenancy card is the one saying who the landlord is
    const card = (await screen.findByText('bills:landlordLine:Lazar Lazić')).closest('button')!;
    fireEvent.click(card);

    expect(nav.navigate).toHaveBeenCalledWith('/bills/tenancy/5');
  });

  it('opens the new tenancy form when an approved application links here', async () => {
    renderPage('/bills?newTenancy=3');

    expect(await screen.findByRole('dialog')).toBeInTheDocument();
    expect(screen.getByText('bills:newTenancy', { selector: 'h2' })).toBeInTheDocument();
  });
});
