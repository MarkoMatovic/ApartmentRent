import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';

const api = vi.hoisted(() => ({ twoFactorSetup: vi.fn(), twoFactorEnable: vi.fn(), twoFactorDisable: vi.fn() }));
const auth = vi.hoisted(() => ({ adoptAccessToken: vi.fn() }));

vi.mock('../../shared/api/auth', () => ({ authApi: api }));
vi.mock('../../shared/context/AuthContext', () => ({ useAuth: () => auth }));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import TwoFactorSection from './TwoFactorSection';

const SETUP = {
  secret: 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567',
  setupUri: 'otpauth://totp/TuRentaj:ana%40test.com?secret=ABCDEFGHIJKLMNOPQRSTUVWXYZ234567',
  qrCodeDataUrl: 'data:image/png;base64,iVBORw0KGgo=',
};
const RECOVERY = ['aaaaa-bbbbb', 'ccccc-ddddd'];

const onChanged = vi.fn();
const renderSection = (enabled = false, required = false) =>
  render(<TwoFactorSection enabled={enabled} required={required} onChanged={onChanged} />);

beforeEach(() => {
  vi.clearAllMocks();
});

describe('TwoFactorSection', () => {
  it('turns it on: QR code and key, then the first code, then the recovery codes once', async () => {
    api.twoFactorSetup.mockResolvedValue(SETUP);
    api.twoFactorEnable.mockResolvedValue({ recoveryCodes: RECOVERY, accessToken: 'confirmed-token' });
    renderSection();

    fireEvent.click(screen.getByRole('button', { name: 'profile:twoFactorEnable' }));

    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByRole('img')).toHaveAttribute('src', SETUP.qrCodeDataUrl);
    expect(within(dialog).getByTestId('two-factor-key')).toHaveTextContent('ABCD EFGH IJKL MNOP QRST UVWX YZ23 4567');
    fireEvent.change(within(dialog).getByLabelText(/^profile:twoFactorCode/), { target: { value: '123456' } });
    fireEvent.click(within(dialog).getByRole('button', { name: 'profile:twoFactorConfirm' }));

    const codes = await screen.findByTestId('recovery-codes');
    expect(within(codes).getAllByRole('listitem').map(li => li.textContent)).toEqual(RECOVERY);
    expect(api.twoFactorEnable).toHaveBeenCalledWith('123456');
    expect(auth.adoptAccessToken).toHaveBeenCalledWith('confirmed-token');
    expect(onChanged).toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: 'profile:twoFactorSaved' }));
    await waitFor(() => expect(screen.queryByTestId('recovery-codes')).not.toBeInTheDocument());
  });

  it('a wrong first code keeps the dialog open with the reason', async () => {
    api.twoFactorSetup.mockResolvedValue(SETUP);
    api.twoFactorEnable.mockRejectedValue({ response: { status: 422, data: { code: 'INVALID_TWO_FACTOR_CODE' } } });
    renderSection();

    fireEvent.click(screen.getByRole('button', { name: 'profile:twoFactorEnable' }));
    const dialog = await screen.findByRole('dialog');
    fireEvent.change(within(dialog).getByLabelText(/^profile:twoFactorCode/), { target: { value: '000000' } });
    fireEvent.click(within(dialog).getByRole('button', { name: 'profile:twoFactorConfirm' }));

    expect(await within(dialog).findByText('validation:INVALID_TWO_FACTOR_CODE')).toBeInTheDocument();
    expect(auth.adoptAccessToken).not.toHaveBeenCalled();
    expect(onChanged).not.toHaveBeenCalled();
  });

  it('turns it off with a code and takes the new token for this session', async () => {
    api.twoFactorDisable.mockResolvedValue('plain-token');
    renderSection(true);

    expect(screen.getByText('profile:twoFactorOn')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'profile:twoFactorDisable' }));
    const dialog = await screen.findByRole('dialog');
    fireEvent.change(within(dialog).getByLabelText(/^profile:twoFactorCode/), { target: { value: 'aaaaa-bbbbb' } });
    fireEvent.click(within(dialog).getByRole('button', { name: 'profile:twoFactorDisable' }));

    await waitFor(() => expect(api.twoFactorDisable).toHaveBeenCalledWith('aaaaa-bbbbb'));
    expect(auth.adoptAccessToken).toHaveBeenCalledWith('plain-token');
    expect(onChanged).toHaveBeenCalled();
    expect(await screen.findByText('profile:twoFactorDisabledSuccess')).toBeInTheDocument();
  });

  it('explains why it matters when the account needs it and it is off', () => {
    renderSection(false, true);

    expect(screen.getByText('profile:twoFactorRequiredHint')).toBeInTheDocument();
  });

  it('says nothing of the kind when it is on, or when the account does not need it', () => {
    const { unmount } = renderSection(true, true);
    expect(screen.queryByText('profile:twoFactorRequiredHint')).not.toBeInTheDocument();
    unmount();

    renderSection(false, false);
    expect(screen.queryByText('profile:twoFactorRequiredHint')).not.toBeInTheDocument();
  });
});
