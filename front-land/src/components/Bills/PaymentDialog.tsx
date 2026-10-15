import React, { useEffect, useState } from 'react';
import {
  Alert, Box, Button, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, IconButton, Stack,
  Tooltip, Typography,
} from '@mui/material';
import CopyIcon from '@mui/icons-material/ContentCopy';
import CheckIcon from '@mui/icons-material/Check';
import DownloadIcon from '@mui/icons-material/Download';
import { useTranslation } from 'react-i18next';
import { formatMoney, PaymentInstructions } from '../../shared/api/bills';
import { getApiErrorMessage } from '../../shared/utils/apiError';

interface Props {
  open: boolean;
  title: string;
  /** Loads the payment order (and its IPS QR code) when the dialog opens. */
  load: () => Promise<PaymentInstructions>;
  onClose: () => void;
  /** What happens after paying ("I have paid"); omitted when there is nothing to mark. */
  confirmLabel?: string;
  onConfirm?: () => Promise<void>;
  /** Extra content between the payment data and the buttons (e.g. where roommates pay back). */
  children?: React.ReactNode;
  confirmDisabled?: boolean;
}

/**
 * The payment order of a bill: the NBS IPS QR code a banking app scans ("IPS skeniraj"), and the
 * same data as text to copy, for paying from the phone the code is shown on.
 */
const PaymentDialog: React.FC<Props> = ({ open, title, load, onClose, confirmLabel, onConfirm, children, confirmDisabled }) => {
  const { t, i18n } = useTranslation(['bills', 'common', 'validation']);
  const [payment, setPayment] = useState<PaymentInstructions | null>(null);
  const [error, setError] = useState('');
  const [copied, setCopied] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    setPayment(null);
    setError('');
    load()
      .then(result => { if (!cancelled) setPayment(result); })
      .catch(err => { if (!cancelled) setError(getApiErrorMessage(err, t)); });
    return () => { cancelled = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  const copy = async (field: string, value: string) => {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(field);
    } catch {
      // No clipboard here: the value is on screen to write down.
    }
  };

  const confirm = async () => {
    if (!onConfirm) return;
    setBusy(true);
    setError('');
    try {
      await onConfirm();
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, t));
    } finally {
      setBusy(false);
    }
  };

  const reference = payment?.paymentReference ? `${payment.paymentModel ?? ''} ${payment.paymentReference}`.trim() : null;
  const fields: { key: string; label: string; value: string; copyValue?: string }[] = payment
    ? [
      { key: 'payee', label: t('bills:payee'), value: payment.payeeName },
      { key: 'account', label: t('bills:account'), value: payment.payeeAccount },
      {
        key: 'amount', label: t('bills:amount'),
        value: `${formatMoney(payment.amount, i18n.language)} RSD`,
        copyValue: payment.amount.toFixed(2).replace('.', ','),
      },
      { key: 'purpose', label: t('bills:purpose'), value: payment.purpose },
      { key: 'code', label: t('bills:paymentCode'), value: payment.paymentCode },
      ...(reference ? [{ key: 'reference', label: t('bills:reference'), value: reference, copyValue: payment.paymentReference! }] : []),
    ]
    : [];

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{t('bills:paymentTitle')}: {title}</DialogTitle>
      <DialogContent>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        {!payment && !error && (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}><CircularProgress /></Box>
        )}
        {payment && (
          <>
            <Typography variant="body2" sx={{ mb: 1 }}>{t('bills:scanHint')}</Typography>
            <Box sx={{ display: 'flex', justifyContent: 'center', my: 1 }}>
              <Box
                component="img"
                src={payment.qrCodeDataUrl}
                alt={t('bills:qrAlt')}
                data-testid="ips-qr"
                sx={{ width: 220, height: 220, bgcolor: '#fff', p: 1, borderRadius: 1 }}
              />
            </Box>
            <Box sx={{ textAlign: 'center', mb: 1 }}>
              <Button
                size="small"
                startIcon={<DownloadIcon />}
                href={payment.qrCodeDataUrl}
                download="ips-qr.png"
              >
                {t('bills:downloadQr')}
              </Button>
            </Box>
            <Typography variant="caption" color="text.secondary" display="block" sx={{ mb: 2 }}>
              {t('bills:onPhoneHint')}
            </Typography>
            <Stack spacing={1} data-testid="payment-fields">
              {fields.map(field => (
                <Box key={field.key} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Box sx={{ flex: 1, minWidth: 0 }}>
                    <Typography variant="caption" color="text.secondary">{field.label}</Typography>
                    <Typography fontWeight={600} sx={{ wordBreak: 'break-word' }}>{field.value}</Typography>
                  </Box>
                  <Tooltip title={copied === field.key ? t('bills:copied') : t('bills:copy')}>
                    <IconButton
                      size="small"
                      aria-label={`${t('bills:copy')}: ${field.label}`}
                      onClick={() => copy(field.key, field.copyValue ?? field.value)}
                    >
                      {copied === field.key ? <CheckIcon fontSize="small" color="success" /> : <CopyIcon fontSize="small" />}
                    </IconButton>
                  </Tooltip>
                </Box>
              ))}
            </Stack>
            {children}
            {onConfirm && (
              <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>{t('bills:afterPayingHint')}</Typography>
            )}
          </>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common:close')}</Button>
        {onConfirm && (
          <Button variant="contained" onClick={confirm} disabled={!payment || busy || confirmDisabled}>
            {busy ? <CircularProgress size={22} /> : confirmLabel}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
};

export default PaymentDialog;
