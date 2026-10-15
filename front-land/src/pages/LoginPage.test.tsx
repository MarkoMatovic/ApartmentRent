import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

const auth = vi.hoisted(() => ({ login: vi.fn(), completeTwoFactorLogin: vi.fn() }));
const nav = vi.hoisted(() => ({ navigate: vi.fn() }));

vi.mock('../shared/context/AuthContext', () => ({ useAuth: () => auth }));
vi.mock('react-router-dom', async (importOriginal) => ({
  ...(await importOriginal<typeof import('react-router-dom')>()),
  useNavigate: () => nav.navigate,
}));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import LoginPage from './LoginPage';

const businessError = (code: string) => ({ response: { status: 422, data: { code } } });

const renderPage = () => render(<MemoryRouter><LoginPage /></MemoryRouter>);

const submitPassword = () => {
  fireEvent.change(screen.getByLabelText(/^email/), { target: { value: 'ana@test.com' } });
  fireEvent.change(screen.getByLabelText(/^password/), { target: { value: 'Secret123' } });
  fireEvent.click(screen.getByRole('button', { name: 'loginButton' }));
};

beforeEach(() => {
  vi.clearAllMocks();
});

describe('LoginPage', () => {
  it('signs in with the password alone when the account has no two-step sign-in', async () => {
    auth.login.mockResolvedValue({});
    renderPage();

    submitPassword();

    await waitFor(() => expect(nav.navigate).toHaveBeenCalledWith('/'));
    expect(auth.login).toHaveBeenCalledWith('ana@test.com', 'Secret123');
    expect(auth.completeTwoFactorLogin).not.toHaveBeenCalled();
  });

  it('asks for the code from the app after the password, and signs in with it', async () => {
    auth.login.mockResolvedValue({ twoFactorToken: 'challenge-1' });
    auth.completeTwoFactorLogin.mockResolvedValue(undefined);
    renderPage();

    submitPassword();
    const code = await screen.findByLabelText(/^twoFactorCode/);
    expect(nav.navigate).not.toHaveBeenCalled();
    fireEvent.change(code, { target: { value: ' 123456 ' } });
    fireEvent.click(screen.getByRole('button', { name: 'twoFactorVerify' }));

    await waitFor(() => expect(nav.navigate).toHaveBeenCalledWith('/'));
    expect(auth.completeTwoFactorLogin).toHaveBeenCalledWith('challenge-1', '123456');
  });

  it('a wrong code keeps the code step open and says why', async () => {
    auth.login.mockResolvedValue({ twoFactorToken: 'challenge-1' });
    auth.completeTwoFactorLogin.mockRejectedValue(businessError('INVALID_TWO_FACTOR_CODE'));
    renderPage();

    submitPassword();
    fireEvent.change(await screen.findByLabelText(/^twoFactorCode/), { target: { value: '000000' } });
    fireEvent.click(screen.getByRole('button', { name: 'twoFactorVerify' }));

    expect(await screen.findByText('validation:INVALID_TWO_FACTOR_CODE')).toBeInTheDocument();
    expect(screen.getByLabelText(/^twoFactorCode/)).toHaveValue('');
    expect(nav.navigate).not.toHaveBeenCalled();
  });

  it('goes back to the password when the code step expired', async () => {
    auth.login.mockResolvedValue({ twoFactorToken: 'challenge-1' });
    auth.completeTwoFactorLogin.mockRejectedValue(businessError('TWO_FACTOR_EXPIRED'));
    renderPage();

    submitPassword();
    fireEvent.change(await screen.findByLabelText(/^twoFactorCode/), { target: { value: '123456' } });
    fireEvent.click(screen.getByRole('button', { name: 'twoFactorVerify' }));

    expect(await screen.findByText('validation:TWO_FACTOR_EXPIRED')).toBeInTheDocument();
    expect(screen.getByLabelText(/^password/)).toHaveValue('');
    expect(screen.queryByLabelText(/^twoFactorCode/)).not.toBeInTheDocument();
  });

  it('"back" returns to the password step', async () => {
    auth.login.mockResolvedValue({ twoFactorToken: 'challenge-1' });
    renderPage();

    submitPassword();
    fireEvent.click(await screen.findByText('backToLogin'));

    expect(screen.getByLabelText(/^password/)).toBeInTheDocument();
  });
});
