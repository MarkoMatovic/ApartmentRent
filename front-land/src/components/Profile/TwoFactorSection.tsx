import React, { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
  Typography,
} from '@mui/material';
import { useTranslation } from 'react-i18next';
import { authApi, TwoFactorSetup } from '../../shared/api/auth';
import { useAuth } from '../../shared/context/AuthContext';
import { getApiErrorMessage } from '../../shared/utils/apiError';

interface Props {
  enabled: boolean;
  /** Some features of this account (administration) need it. */
  required: boolean;
  /** Called after it was turned on or off, so the stored profile is read again. */
  onChanged: () => void;
}

/** "Key ABCD EFGH ..." is easier to type into a phone in groups of four. */
const groupKey = (secret: string) => secret.match(/.{1,4}/g)?.join(' ') ?? secret;

/**
 * Two-step sign-in in the profile: turning it on (QR code for the authenticator app, the first code,
 * then the recovery codes, shown once) and turning it off with a code.
 */
const TwoFactorSection: React.FC<Props> = ({ enabled, required, onChanged }) => {
  const { t } = useTranslation(['profile', 'common', 'validation']);
  const { adoptAccessToken } = useAuth();
  const [setup, setSetup] = useState<TwoFactorSetup | null>(null);
  const [recoveryCodes, setRecoveryCodes] = useState<string[] | null>(null);
  const [disableOpen, setDisableOpen] = useState(false);
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [copied, setCopied] = useState(false);
  const [notice, setNotice] = useState('');

  const closeAll = () => {
    setSetup(null);
    setRecoveryCodes(null);
    setDisableOpen(false);
    setCode('');
    setError('');
    setCopied(false);
  };

  const startSetup = async () => {
    setBusy(true);
    setError('');
    setNotice('');
    try {
      setSetup(await authApi.twoFactorSetup());
    } catch (err) {
      setNotice(getApiErrorMessage(err, t));
    } finally {
      setBusy(false);
    }
  };

  const confirmSetup = async () => {
    setBusy(true);
    setError('');
    try {
      const result = await authApi.twoFactorEnable(code.trim());
      adoptAccessToken(result.accessToken);
      setRecoveryCodes(result.recoveryCodes);
      setSetup(null);
      setCode('');
      onChanged();
    } catch (err) {
      setError(getApiErrorMessage(err, t));
    } finally {
      setBusy(false);
    }
  };

  const confirmDisable = async () => {
    setBusy(true);
    setError('');
    try {
      adoptAccessToken(await authApi.twoFactorDisable(code.trim()));
      closeAll();
      setNotice(t('profile:twoFactorDisabledSuccess'));
      onChanged();
    } catch (err) {
      setError(getApiErrorMessage(err, t));
    } finally {
      setBusy(false);
    }
  };

  const copyCodes = async () => {
    if (!recoveryCodes) return;
    try {
      await navigator.clipboard.writeText(recoveryCodes.join('\n'));
      setCopied(true);
    } catch {
      // No clipboard (insecure page or refused): the codes are on screen to write down.
    }
  };

  const codeField = (
    <TextField
      fullWidth
      autoFocus
      label={t('profile:twoFactorCode')}
      value={code}
      onChange={e => setCode(e.target.value)}
      margin="normal"
      autoComplete="one-time-code"
      inputProps={{ maxLength: 20 }}
    />
  );

  return (
    <Box sx={{ mt: 3 }} data-testid="two-factor-section">
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
        <Typography variant="subtitle1" fontWeight={600}>{t('profile:twoFactorTitle')}</Typography>
        <Chip
          size="small"
          color={enabled ? 'success' : 'default'}
          label={enabled ? t('profile:twoFactorOn') : t('profile:twoFactorOff')}
        />
      </Box>
      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5, mb: 1.5 }}>
        {t('profile:twoFactorIntro')}
      </Typography>
      {required && !enabled && (
        <Alert severity="warning" sx={{ mb: 1.5 }}>{t('profile:twoFactorRequiredHint')}</Alert>
      )}
      {notice && <Alert severity="info" sx={{ mb: 1.5 }} onClose={() => setNotice('')}>{notice}</Alert>}
      {enabled ? (
        <Button variant="outlined" color="error" onClick={() => { setError(''); setDisableOpen(true); }}>
          {t('profile:twoFactorDisable')}
        </Button>
      ) : (
        <Button variant="contained" onClick={startSetup} disabled={busy}>
          {busy && !setup ? <CircularProgress size={22} /> : t('profile:twoFactorEnable')}
        </Button>
      )}

      {/* Turning it on: scan, then the first code */}
      <Dialog open={!!setup} onClose={closeAll} maxWidth="xs" fullWidth>
        <DialogTitle>{t('profile:twoFactorEnable')}</DialogTitle>
        <DialogContent>
          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
          <Typography variant="body2">{t('profile:twoFactorScan')}</Typography>
          {setup && (
            <>
              <Box sx={{ display: 'flex', justifyContent: 'center', my: 2 }}>
                <Box
                  component="img"
                  src={setup.qrCodeDataUrl}
                  alt={t('profile:twoFactorQrAlt')}
                  sx={{ width: 200, height: 200, bgcolor: '#fff', p: 1, borderRadius: 1 }}
                />
              </Box>
              <Typography variant="body2" color="text.secondary">{t('profile:twoFactorManualKey')}</Typography>
              <Typography
                data-testid="two-factor-key"
                sx={{ fontFamily: 'monospace', fontSize: '1rem', wordBreak: 'break-all', my: 1, userSelect: 'all' }}
              >
                {groupKey(setup.secret)}
              </Typography>
            </>
          )}
          <Typography variant="body2" sx={{ mt: 2 }}>{t('profile:twoFactorEnterCode')}</Typography>
          {codeField}
        </DialogContent>
        <DialogActions>
          <Button onClick={closeAll}>{t('common:cancel')}</Button>
          <Button variant="contained" onClick={confirmSetup} disabled={busy || code.trim().length === 0}>
            {busy ? <CircularProgress size={22} /> : t('profile:twoFactorConfirm')}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Turned on: the recovery codes, this once */}
      <Dialog open={!!recoveryCodes} maxWidth="xs" fullWidth>
        <DialogTitle>{t('profile:twoFactorRecoveryTitle')}</DialogTitle>
        <DialogContent>
          <Alert severity="success" sx={{ mb: 2 }}>{t('profile:twoFactorEnabledSuccess')}</Alert>
          <Typography variant="body2" sx={{ mb: 2 }}>{t('profile:twoFactorRecoveryHint')}</Typography>
          <Box
            component="ul"
            data-testid="recovery-codes"
            sx={{ listStyle: 'none', p: 0, m: 0, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1, fontFamily: 'monospace' }}
          >
            {recoveryCodes?.map(c => <li key={c}>{c}</li>)}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={copyCodes}>{copied ? t('profile:twoFactorCopied') : t('profile:twoFactorCopy')}</Button>
          <Button variant="contained" onClick={closeAll}>{t('profile:twoFactorSaved')}</Button>
        </DialogActions>
      </Dialog>

      {/* Turning it off */}
      <Dialog open={disableOpen} onClose={closeAll} maxWidth="xs" fullWidth>
        <DialogTitle>{t('profile:twoFactorDisableTitle')}</DialogTitle>
        <DialogContent>
          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
          <Typography variant="body2">{t('profile:twoFactorDisableHint')}</Typography>
          {required && <Alert severity="warning" sx={{ mt: 2 }}>{t('profile:twoFactorRequiredHint')}</Alert>}
          {codeField}
        </DialogContent>
        <DialogActions>
          <Button onClick={closeAll}>{t('common:cancel')}</Button>
          <Button variant="contained" color="error" onClick={confirmDisable} disabled={busy || code.trim().length === 0}>
            {busy ? <CircularProgress size={22} /> : t('profile:twoFactorDisable')}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default TwoFactorSection;
