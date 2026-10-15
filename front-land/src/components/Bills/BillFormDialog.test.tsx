import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { Bill } from '../../shared/api/bills';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'sr' } }),
}));

import BillFormDialog from './BillFormDialog';

// 160-0000000123456-xx with the right control digits
const control = (() => {
  let r = 0;
  for (const c of '160000000012345600') r = (r * 10 + Number(c)) % 97;
  return String(98 - r).padStart(2, '0');
})();
const ACCOUNT = `160-0000000123456-${control}`;

const onCreate = vi.fn();
const onUpdate = vi.fn();
const onClose = vi.fn();

const renderForm = (isLandlord: boolean, bill: Bill | null = null) =>
  render(<BillFormDialog open tenancyId={7} isLandlord={isLandlord} tenantCount={2} bill={bill}
    onClose={onClose} onCreate={onCreate} onUpdate={onUpdate} />);

const type = (label: RegExp, value: string) => fireEvent.change(screen.getByLabelText(label), { target: { value } });

beforeEach(() => {
  vi.clearAllMocks();
  onCreate.mockResolvedValue(undefined);
  onUpdate.mockResolvedValue(undefined);
});

describe('BillFormDialog', () => {
  it('lets a tenant enter only a bill paid to a company, with its account', async () => {
    renderForm(false);

    expect(screen.queryByLabelText('bills:payToLandlord')).not.toBeInTheDocument();
    type(/^bills:billTitle/, 'Struja oktobar');
    type(/^bills:amountRsd/, '6123,40');
    type(/^bills:companyName/, 'EPS');
    type(/^bills:companyAccount/, '160-123');
    expect(screen.getByText('bills:accountInvalid')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'bills:save' })).toBeDisabled();

    type(/^bills:companyAccount/, ACCOUNT);
    type(/^bills:referenceModel/, '97');
    type(/^bills:referenceNumber/, '5501234');
    fireEvent.click(screen.getByRole('button', { name: 'bills:save' }));

    await waitFor(() => expect(onCreate).toHaveBeenCalled());
    expect(onCreate.mock.calls[0][0]).toMatchObject({
      tenancyId: 7, kind: 'electricity', title: 'Struja oktobar', amount: 6123.4, payTo: 'company',
      payeeName: 'EPS', payeeAccount: ACCOUNT, paymentCode: '289', paymentModel: '97', paymentReference: '5501234',
    });
    expect(onClose).toHaveBeenCalled();
  });

  it('lets the landlord ask the tenants to pay them, without company details', async () => {
    renderForm(true);

    fireEvent.click(screen.getByLabelText('bills:payToLandlord'));
    expect(screen.queryByLabelText(/^bills:companyAccount/)).not.toBeInTheDocument();
    type(/^bills:billTitle/, 'Voda');
    type(/^bills:amountRsd/, '900');
    fireEvent.click(screen.getByRole('button', { name: 'bills:save' }));

    await waitFor(() => expect(onCreate).toHaveBeenCalled());
    const input = onCreate.mock.calls[0][0];
    expect(input.payTo).toBe('landlord');
    expect(input.payeeAccount).toBeUndefined();
  });

  it('says how the bill is split', () => {
    renderForm(true);

    expect(screen.getByText('bills:splitHint')).toBeInTheDocument();
  });

  it('changes only the name, amount and due date of an existing bill', async () => {
    renderForm(true, {
      billId: 3, kind: 'water', title: 'Voda', amount: 900, dueDate: '2026-10-20', payTo: 'landlord',
    } as Bill);

    expect(screen.queryByLabelText(/^bills:kind/)).not.toBeInTheDocument();
    type(/^bills:amountRsd/, '950');
    fireEvent.click(screen.getByRole('button', { name: 'bills:save' }));

    await waitFor(() => expect(onUpdate).toHaveBeenCalledWith(3, { title: 'Voda', amount: 950, dueDate: '2026-10-20' }));
  });

  it('shows why the server refused it and stays open', async () => {
    onCreate.mockRejectedValue({ response: { status: 422, data: { code: 'TENANCY_NOT_ACTIVE' } } });
    renderForm(true);
    fireEvent.click(screen.getByLabelText('bills:payToLandlord'));
    type(/^bills:billTitle/, 'Voda');
    type(/^bills:amountRsd/, '900');

    fireEvent.click(screen.getByRole('button', { name: 'bills:save' }));

    expect(await screen.findByText('validation:TENANCY_NOT_ACTIVE')).toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
  });
});
