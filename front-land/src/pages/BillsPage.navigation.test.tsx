import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';

const api = vi.hoisted(() => ({
  getMine: vi.fn().mockResolvedValue({ toPay: [], toConfirm: [] }),
  getTenancies: vi.fn().mockResolvedValue([]),
  getCandidates: vi.fn().mockResolvedValue([{ userId: 2, name: 'Ana Anić', reason: 'application' }]),
  createTenancy: vi.fn(),
}));

vi.mock('../shared/api/bills', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../shared/api/bills')>()),
  billsApi: api,
}));
vi.mock('../shared/api/apartments', () => ({
  apartmentsApi: { getMyApartments: vi.fn().mockResolvedValue([{ apartmentId: 3, title: 'Stan na Vračaru' }]) },
}));
vi.mock('../shared/context/AuthContext', () => ({ useAuth: () => ({ user: { userId: 1, firstName: 'Lazar', lastName: 'Lazić' } }) }));
vi.mock('../shared/context/NotificationContext', () => ({ useNotifications: () => ({ addNotification: vi.fn() }) }));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'sr' } }),
}));

import BillsPage from './BillsPage';

const validAccount = () => {
  let r = 0;
  for (const c of '160000000012345600') r = (r * 10 + Number(c)) % 97;
  return `160-0000000123456-${String(98 - r).padStart(2, '0')}`;
};

describe('BillsPage - after creating a tenancy', () => {
  it('ends up on the new tenancy, also when an approved application linked to the form', async () => {
    api.createTenancy.mockResolvedValue({ tenancyId: 77 });
    render(
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <MemoryRouter initialEntries={['/bills?newTenancy=3']}>
          <Routes>
            <Route path="/bills" element={<BillsPage />} />
            <Route path="/bills/tenancy/:id" element={<p>tenancy page</p>} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    fireEvent.click(await screen.findByLabelText(/Ana Anić/));
    fireEvent.change(screen.getByLabelText(/^bills:monthlyRent/), { target: { value: '30000' } });
    fireEvent.change(screen.getByLabelText(/^bills:payeeAccount/), { target: { value: validAccount() } });
    fireEvent.click(screen.getByRole('button', { name: 'bills:createTenancy' }));

    // Closing the form used to clear "?newTenancy" afterwards, which took the user back to /bills.
    expect(await screen.findByText('tenancy page')).toBeInTheDocument();
  });
});
