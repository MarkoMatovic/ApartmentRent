import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { Tenancy } from '../../shared/api/bills';

const api = vi.hoisted(() => ({ getCandidates: vi.fn() }));
const apartments = vi.hoisted(() => ({ getMyApartments: vi.fn() }));

vi.mock('../../shared/api/bills', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../../shared/api/bills')>()),
  billsApi: api,
}));
vi.mock('../../shared/api/apartments', () => ({ apartmentsApi: apartments }));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'sr' } }),
}));

import TenancyFormDialog from './TenancyFormDialog';

const control = (() => {
  let r = 0;
  for (const c of '160000000012345600') r = (r * 10 + Number(c)) % 97;
  return String(98 - r).padStart(2, '0');
})();
const ACCOUNT = `160-0000000123456-${control}`;

const onCreate = vi.fn();
const onUpdate = vi.fn();

const renderForm = (props: { tenancy?: Tenancy; initialApartmentId?: number } = {}) => render(
  <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
    <TenancyFormDialog open defaultPayeeName="Lazar Lazić" onClose={vi.fn()} onCreate={onCreate} onUpdate={onUpdate} {...props} />
  </QueryClientProvider>,
);

const type = (label: RegExp, value: string) => fireEvent.change(screen.getByLabelText(label), { target: { value } });

beforeEach(() => {
  vi.clearAllMocks();
  onCreate.mockResolvedValue(undefined);
  onUpdate.mockResolvedValue(undefined);
  apartments.getMyApartments.mockResolvedValue([{ apartmentId: 3, title: 'Stan na Vračaru' }]);
  api.getCandidates.mockResolvedValue([
    { userId: 7, name: 'Ana Anić', reason: 'application' },
    { userId: 8, name: 'Marko Marković', reason: 'roommate' },
  ]);
});

describe('TenancyFormDialog', () => {
  it('creates a tenancy with the chosen tenants and the rent settings', async () => {
    renderForm({ initialApartmentId: 3 });

    expect(await screen.findByText('Ana Anić')).toBeInTheDocument();
    expect(screen.getByText('(bills:candidateRoommate)')).toBeInTheDocument();
    fireEvent.click(screen.getByLabelText(/Ana Anić/));
    fireEvent.click(screen.getByLabelText(/Marko Marković/));
    type(/^bills:monthlyRent/, '30000');
    type(/^bills:dueDay/, '20');
    type(/^bills:payeeAccount/, ACCOUNT);
    fireEvent.click(screen.getByRole('button', { name: 'bills:createTenancy' }));

    await waitFor(() => expect(onCreate).toHaveBeenCalled());
    expect(api.getCandidates).toHaveBeenCalledWith(3);
    expect(onCreate.mock.calls[0][0]).toMatchObject({
      apartmentId: 3, tenantUserIds: [7, 8], monthlyRent: 30000, rentDueDay: 20,
      payeeName: 'Lazar Lazić', payeeAccount: ACCOUNT,
    });
  });

  it('will not create one without a tenant, a valid account or a due day of 1–28', async () => {
    renderForm({ initialApartmentId: 3 });
    await screen.findByText('Ana Anić');
    type(/^bills:monthlyRent/, '30000');
    type(/^bills:payeeAccount/, ACCOUNT);
    const create = screen.getByRole('button', { name: 'bills:createTenancy' });
    expect(create).toBeDisabled();

    fireEvent.click(screen.getByLabelText(/Ana Anić/));
    expect(create).toBeEnabled();
    type(/^bills:dueDay/, '31');
    expect(create).toBeDisabled();
    type(/^bills:dueDay/, '5');
    type(/^bills:payeeAccount/, '160-0000000123456-00');
    expect(screen.getByText('bills:accountInvalid')).toBeInTheDocument();
    expect(create).toBeDisabled();
  });

  it('explains when nobody can be added yet', async () => {
    api.getCandidates.mockResolvedValue([]);
    renderForm({ initialApartmentId: 3 });

    expect(await screen.findByText('bills:noCandidates')).toBeInTheDocument();
  });

  it('edits only the rent settings of an existing tenancy', async () => {
    renderForm({
      tenancy: {
        tenancyId: 5, apartmentId: 3, monthlyRent: 30000, rentDueDay: 20, payeeName: 'Lazar Lazić', payeeAccount: ACCOUNT,
      } as Tenancy,
    });

    expect(screen.queryByLabelText(/^bills:apartment/)).not.toBeInTheDocument();
    type(/^bills:monthlyRent/, '32000');
    fireEvent.click(screen.getByRole('button', { name: 'bills:save' }));

    await waitFor(() => expect(onUpdate).toHaveBeenCalledWith(5, {
      monthlyRent: 32000, rentDueDay: 20, payeeName: 'Lazar Lazić', payeeAccount: ACCOUNT,
    }));
  });
});
