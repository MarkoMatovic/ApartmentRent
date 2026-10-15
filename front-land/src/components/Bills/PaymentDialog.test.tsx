import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import type { PaymentInstructions } from '../../shared/api/bills';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'sr' } }),
}));

import PaymentDialog from './PaymentDialog';

const PAYMENT: PaymentInstructions = {
  payeeName: 'Lazar Lazić',
  payeeAccount: '160-0000000123456-78',
  amount: 15000.5,
  currency: 'RSD',
  paymentCode: '289',
  purpose: 'Kirija 10/2026',
  paymentModel: '00',
  paymentReference: '12-202610',
  qrPayload: 'K:PR|V:01|C:1|R:160000000012345678|N:Lazar Lazić|I:RSD15000,50|SF:289|S:Kirija 10/2026',
  qrCodeDataUrl: 'data:image/png;base64,iVBORw0KGgo=',
};

const writeText = vi.fn();

beforeEach(() => {
  vi.clearAllMocks();
  Object.assign(navigator, { clipboard: { writeText } });
  writeText.mockResolvedValue(undefined);
});

describe('PaymentDialog', () => {
  it('shows the IPS QR code, a download of it, and the payment data as text', async () => {
    render(<PaymentDialog open title="Kirija" load={() => Promise.resolve(PAYMENT)} onClose={vi.fn()} />);

    const qr = await screen.findByTestId('ips-qr');
    expect(qr).toHaveAttribute('src', PAYMENT.qrCodeDataUrl);
    expect(screen.getByRole('link', { name: 'bills:downloadQr' })).toHaveAttribute('download', 'ips-qr.png');
    const fields = screen.getByTestId('payment-fields');
    expect(within(fields).getByText('Lazar Lazić')).toBeInTheDocument();
    expect(within(fields).getByText('160-0000000123456-78')).toBeInTheDocument();
    expect(within(fields).getByText('15.000,50 RSD')).toBeInTheDocument();
    expect(within(fields).getByText('00 12-202610')).toBeInTheDocument();
  });

  it('copies each value, the amount the way a payment order wants it', async () => {
    render(<PaymentDialog open title="Kirija" load={() => Promise.resolve(PAYMENT)} onClose={vi.fn()} />);
    await screen.findByTestId('ips-qr');

    fireEvent.click(screen.getByRole('button', { name: 'bills:copy: bills:account' }));
    fireEvent.click(screen.getByRole('button', { name: 'bills:copy: bills:amount' }));
    fireEvent.click(screen.getByRole('button', { name: 'bills:copy: bills:reference' }));

    await waitFor(() => expect(writeText).toHaveBeenCalledTimes(3));
    expect(writeText).toHaveBeenNthCalledWith(1, '160-0000000123456-78');
    expect(writeText).toHaveBeenNthCalledWith(2, '15000,50');
    expect(writeText).toHaveBeenNthCalledWith(3, '12-202610');
  });

  it('"I have paid" runs the step and closes', async () => {
    const onConfirm = vi.fn().mockResolvedValue(undefined);
    const onClose = vi.fn();
    render(<PaymentDialog open title="Kirija" load={() => Promise.resolve(PAYMENT)} onClose={onClose}
      confirmLabel="bills:iPaid" onConfirm={onConfirm} />);
    await screen.findByTestId('ips-qr');

    fireEvent.click(screen.getByRole('button', { name: 'bills:iPaid' }));

    await waitFor(() => expect(onClose).toHaveBeenCalled());
    expect(onConfirm).toHaveBeenCalledTimes(1);
  });

  it('a failed step stays open with the reason', async () => {
    const onClose = vi.fn();
    const onConfirm = vi.fn().mockRejectedValue({ response: { status: 422, data: { code: 'PAYMENT_ALREADY_CONFIRMED' } } });
    render(<PaymentDialog open title="Kirija" load={() => Promise.resolve(PAYMENT)} onClose={onClose}
      confirmLabel="bills:iPaid" onConfirm={onConfirm} />);
    await screen.findByTestId('ips-qr');

    fireEvent.click(screen.getByRole('button', { name: 'bills:iPaid' }));

    expect(await screen.findByText('validation:PAYMENT_ALREADY_CONFIRMED')).toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
  });

  it('says so when the payment order cannot be loaded, and offers nothing to confirm', async () => {
    render(<PaymentDialog open title="Kirija" load={() => Promise.reject({ response: { status: 403 } })} onClose={vi.fn()}
      confirmLabel="bills:iPaid" onConfirm={vi.fn()} />);

    expect(await screen.findByText('validation:errForbidden')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'bills:iPaid' })).toBeDisabled();
  });
});
