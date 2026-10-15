import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { Bill, BillShare } from '../../shared/api/bills';

const api = vi.hoisted(() => ({
  confirmShare: vi.fn(), rejectShare: vi.fn(), unmarkShare: vi.fn(), markSharePaid: vi.fn(), getSharePayment: vi.fn(),
  undoCompanyPayment: vi.fn(), cancelBill: vi.fn(), getCompanyPayment: vi.fn(), payCompanyBill: vi.fn(), getProfile: vi.fn(),
}));

vi.mock('../../shared/api/bills', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../../shared/api/bills')>()),
  billsApi: api,
}));
vi.mock('../../shared/context/NotificationContext', () => ({ useNotifications: () => ({ addNotification: vi.fn() }) }));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'sr' } }),
}));

import BillCard from './BillCard';

const LANDLORD = 1;
const ANA = 2;
const MARKO = 3;
const person = (userId: number) => ({ userId, name: ['', 'Lazar', 'Ana', 'Marko'][userId] });

const share = (billShareId: number, payer: number, over: Partial<BillShare> = {}): BillShare => ({
  billShareId, payer: person(payer), receiver: person(LANDLORD), amount: 15000, status: 'unpaid',
  canMarkPaid: false, canConfirm: false, ...over,
});

const bill = (over: Partial<Bill> = {}): Bill => ({
  billId: 10, tenancyId: 5, apartmentTitle: 'Stan', tenantCount: 2, kind: 'water', title: 'Voda', amount: 30000,
  dueDate: '2026-10-20', payTo: 'landlord', payeeName: 'Lazar Lazić', payeeAccount: '160-0000000123456-78',
  paymentCode: '289', purpose: 'Voda', createdBy: person(LANDLORD), createdDate: '2026-10-01T00:00:00Z',
  status: 'open', isOverdue: false, canPayCompany: false, canUndoCompanyPayment: false, canEdit: false,
  shares: [share(101, ANA), share(102, MARKO)], ...over,
});

const renderCard = (b: Bill, me: number, onEdit = vi.fn()) => render(
  <QueryClientProvider client={new QueryClient()}>
    <BillCard bill={b} currentUserId={me} onEdit={onEdit} />
  </QueryClientProvider>,
);

beforeEach(() => {
  vi.clearAllMocks();
  for (const fn of Object.values(api)) fn.mockResolvedValue(undefined);
});

describe('BillCard', () => {
  it('offers the payer "Pay" on their own unpaid part only', () => {
    renderCard(bill({ shares: [share(101, ANA, { canMarkPaid: true }), share(102, MARKO)] }), ANA);

    expect(within(screen.getByTestId('share-101')).getByRole('button', { name: 'bills:pay' })).toBeInTheDocument();
    expect(within(screen.getByTestId('share-102')).queryByRole('button')).not.toBeInTheDocument();
  });

  it('offers the receiver "received" and, for a marked part, "not received"', async () => {
    renderCard(bill({
      shares: [share(101, ANA, { status: 'marked_paid', canConfirm: true }), share(102, MARKO, { canConfirm: true })],
    }), LANDLORD);

    const marked = within(screen.getByTestId('share-101'));
    fireEvent.click(marked.getByRole('button', { name: 'bills:notReceived' }));
    await waitFor(() => expect(api.rejectShare).toHaveBeenCalledWith(101));
    fireEvent.click(within(screen.getByTestId('share-102')).getByRole('button', { name: 'bills:received' }));
    await waitFor(() => expect(api.confirmShare).toHaveBeenCalledWith(102));
    expect(within(screen.getByTestId('share-102')).queryByRole('button', { name: 'bills:notReceived' })).not.toBeInTheDocument();
  });

  it('lets the payer take back a mark', async () => {
    renderCard(bill({ shares: [share(101, ANA, { status: 'marked_paid' })] }), ANA);

    fireEvent.click(screen.getByRole('button', { name: 'bills:undoPaid' }));

    await waitFor(() => expect(api.unmarkShare).toHaveBeenCalledWith(101));
  });

  it('cancels only after asking', async () => {
    renderCard(bill({ canEdit: true }), LANDLORD);

    fireEvent.click(screen.getByRole('button', { name: 'bills:cancelBill' }));
    expect(api.cancelBill).not.toHaveBeenCalled();
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'bills:cancelBill' }));

    await waitFor(() => expect(api.cancelBill).toHaveBeenCalledWith(10));
  });

  it('offers to pay a company bill in full, asking where the roommates pay back', async () => {
    api.getCompanyPayment.mockResolvedValue({
      payeeName: 'EPS', payeeAccount: '840-0000000001000-11', amount: 6000, currency: 'RSD', paymentCode: '289',
      purpose: 'Struja', qrPayload: 'K:PR', qrCodeDataUrl: 'data:image/png;base64,AA==',
    });
    api.getProfile.mockResolvedValue({ payoutName: null, payoutAccount: null });
    renderCard(bill({ payTo: 'company', shares: [], canPayCompany: true, amount: 6000 }), ANA);

    fireEvent.click(screen.getByRole('button', { name: 'bills:payWhole' }));

    expect(await screen.findByTestId('payout-fields')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'bills:confirmPaidCompany' })).toBeDisabled();
  });

  it('shows who paid a company bill and lets them undo it', async () => {
    renderCard(bill({ payTo: 'company', paidBy: person(ANA), canUndoCompanyPayment: true, shares: [] }), ANA);

    expect(screen.getByText('bills:paidByLine')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'bills:undoCompanyPayment' }));

    await waitFor(() => expect(api.undoCompanyPayment).toHaveBeenCalledWith(10));
  });
});
