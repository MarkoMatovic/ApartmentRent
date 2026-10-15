import React, { useEffect, useState } from 'react';
import { Box, TextField, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { Bill, billsApi, formatMoney, isValidSerbianAccount } from '../../shared/api/bills';
import PaymentDialog from './PaymentDialog';
import { billTitle } from './billUi';

interface Props {
  bill: Bill | null;
  onClose: () => void;
  onPaid: (payout: { payoutName?: string; payoutAccount?: string }) => Promise<void>;
}

/**
 * A company bill (EPS, Infostan...) is paid once, in full, by one tenant. When there are roommates,
 * the payer says where they pay their part back (remembered for next time).
 */
const PayCompanyDialog: React.FC<Props> = ({ bill, onClose, onPaid }) => {
  const { t, i18n } = useTranslation(['bills', 'common']);
  const [payoutName, setPayoutName] = useState('');
  const [payoutAccount, setPayoutAccount] = useState('');
  const sharesBack = (bill?.tenantCount ?? 1) > 1;

  useEffect(() => {
    if (!bill || !sharesBack) return;
    let cancelled = false;
    billsApi.getProfile()
      .then(profile => {
        if (cancelled) return;
        setPayoutName(profile.payoutName ?? '');
        setPayoutAccount(profile.payoutAccount ?? '');
      })
      .catch(() => { /* nothing saved yet */ });
    return () => { cancelled = true; };
  }, [bill, sharesBack]);

  if (!bill) return null;
  const accountValid = isValidSerbianAccount(payoutAccount);
  const myPart = bill.amount / Math.max(1, bill.tenantCount);

  return (
    <PaymentDialog
      open
      title={billTitle(t, bill, i18n.language)}
      load={() => billsApi.getCompanyPayment(bill.billId)}
      onClose={onClose}
      confirmLabel={t('bills:confirmPaidCompany')}
      confirmDisabled={sharesBack && (!accountValid || payoutName.trim().length === 0)}
      onConfirm={() => onPaid(sharesBack ? { payoutName: payoutName.trim(), payoutAccount: payoutAccount.trim() } : {})}
    >
      <Typography variant="body2" sx={{ mt: 2 }}>{t('bills:payCompanyHint')}</Typography>
      {sharesBack && (
        <Box sx={{ mt: 2 }} data-testid="payout-fields">
          <Typography variant="subtitle2">{t('bills:payoutTitle')}</Typography>
          <Typography variant="caption" color="text.secondary">
            {t('bills:yourShare', { amount: formatMoney(myPart, i18n.language) })}
          </Typography>
          <TextField
            fullWidth size="small" margin="dense" label={t('bills:payoutName')} value={payoutName}
            onChange={e => setPayoutName(e.target.value)} inputProps={{ maxLength: 70 }}
          />
          <TextField
            fullWidth size="small" margin="dense" label={t('bills:payoutAccount')} value={payoutAccount}
            onChange={e => setPayoutAccount(e.target.value)}
            error={payoutAccount !== '' && !accountValid}
            helperText={payoutAccount !== '' && !accountValid ? t('bills:accountInvalid') : ' '}
          />
        </Box>
      )}
    </PaymentDialog>
  );
};

export default PayCompanyDialog;
