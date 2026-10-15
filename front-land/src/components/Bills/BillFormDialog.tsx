import React, { useEffect, useState } from 'react';
import {
  Alert, Box, Button, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, FormControl,
  FormControlLabel, FormLabel, MenuItem, Radio, RadioGroup, TextField, Typography,
} from '@mui/material';
import { useTranslation } from 'react-i18next';
import {
  Bill, BillKind, CreateBillInput, isValidSerbianAccount, todayIso, UpdateBillInput, UTILITY_KINDS,
} from '../../shared/api/bills';
import { getApiErrorMessage } from '../../shared/utils/apiError';

interface Props {
  open: boolean;
  tenancyId: number;
  /** The landlord may ask to be paid; a tenant enters bills that are paid to a company. */
  isLandlord: boolean;
  tenantCount: number;
  /** Set to change an existing bill (only its name, amount and due date). */
  bill?: Bill | null;
  onClose: () => void;
  onCreate: (input: CreateBillInput) => Promise<void>;
  onUpdate: (billId: number, input: UpdateBillInput) => Promise<void>;
}

const emptyForm = (isLandlord: boolean) => ({
  kind: 'electricity' as BillKind,
  title: '',
  amount: '',
  dueDate: todayIso(),
  payTo: (isLandlord ? 'landlord' : 'company') as 'landlord' | 'company',
  payeeName: '',
  payeeAccount: '',
  paymentCode: '289',
  paymentModel: '',
  paymentReference: '',
});

/** A utility bill as printed on the paper one: amount, due date and, for a company, its account and reference. */
const BillFormDialog: React.FC<Props> = ({ open, tenancyId, isLandlord, tenantCount, bill, onClose, onCreate, onUpdate }) => {
  const { t } = useTranslation(['bills', 'common', 'validation']);
  const [form, setForm] = useState(emptyForm(isLandlord));
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!open) return;
    setError('');
    setForm(bill
      ? { ...emptyForm(isLandlord), kind: bill.kind, title: bill.title, amount: String(bill.amount), dueDate: bill.dueDate.slice(0, 10), payTo: bill.payTo }
      : emptyForm(isLandlord));
  }, [open, bill, isLandlord]);

  const set = (name: keyof ReturnType<typeof emptyForm>) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(prev => ({ ...prev, [name]: e.target.value }));

  const amount = Number(form.amount.replace(',', '.'));
  const amountValid = Number.isFinite(amount) && amount > 0;
  const toCompany = form.payTo === 'company';
  const accountValid = !toCompany || bill != null || isValidSerbianAccount(form.payeeAccount);
  const canSave = form.title.trim().length > 0 && amountValid && !!form.dueDate
    && (!toCompany || bill != null || (form.payeeName.trim().length > 0 && accountValid));

  const save = async () => {
    setBusy(true);
    setError('');
    try {
      const rounded = Math.round(amount * 100) / 100;
      if (bill) {
        await onUpdate(bill.billId, { title: form.title.trim(), amount: rounded, dueDate: form.dueDate });
      } else {
        await onCreate({
          tenancyId,
          kind: form.kind,
          title: form.title.trim(),
          amount: rounded,
          dueDate: form.dueDate,
          payTo: form.payTo,
          ...(toCompany ? {
            payeeName: form.payeeName.trim(),
            payeeAccount: form.payeeAccount.trim(),
            paymentCode: form.paymentCode.trim() || undefined,
            paymentModel: form.paymentModel.trim() || undefined,
            paymentReference: form.paymentReference.trim() || undefined,
          } : {}),
        });
      }
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, t));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{bill ? t('bills:editBill') : t('bills:addBill')}</DialogTitle>
      <DialogContent>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        {!bill && (
          <TextField select fullWidth margin="normal" label={t('bills:kind')} value={form.kind} onChange={set('kind')}>
            {UTILITY_KINDS.map(kind => <MenuItem key={kind} value={kind}>{t(`bills:kind_${kind}`)}</MenuItem>)}
          </TextField>
        )}
        <TextField
          fullWidth margin="normal" label={t('bills:billTitle')} placeholder={t('bills:billTitlePlaceholder')}
          value={form.title} onChange={set('title')} inputProps={{ maxLength: 100 }}
        />
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
          <TextField
            sx={{ flex: 1, minWidth: 160 }} margin="normal" label={t('bills:amountRsd')} value={form.amount}
            onChange={set('amount')} inputProps={{ inputMode: 'decimal' }}
            error={form.amount !== '' && !amountValid}
          />
          <TextField
            sx={{ flex: 1, minWidth: 160 }} margin="normal" type="date" label={t('bills:dueDate')} value={form.dueDate}
            onChange={set('dueDate')} InputLabelProps={{ shrink: true }}
          />
        </Box>

        {!bill && (
          <FormControl sx={{ mt: 2 }} fullWidth>
            <FormLabel>{t('bills:payTo')}</FormLabel>
            <RadioGroup value={form.payTo} onChange={set('payTo')}>
              {isLandlord && <FormControlLabel value="landlord" control={<Radio />} label={t('bills:payToLandlord')} />}
              <FormControlLabel value="company" control={<Radio />} label={t('bills:payToCompany')} />
            </RadioGroup>
          </FormControl>
        )}

        {!bill && toCompany && (
          <>
            <TextField fullWidth margin="normal" label={t('bills:companyName')} value={form.payeeName} onChange={set('payeeName')} inputProps={{ maxLength: 70 }} />
            <TextField
              fullWidth margin="normal" label={t('bills:companyAccount')} value={form.payeeAccount} onChange={set('payeeAccount')}
              error={form.payeeAccount !== '' && !accountValid}
              helperText={form.payeeAccount !== '' && !accountValid ? t('bills:accountInvalid') : ' '}
            />
            <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
              <TextField
                sx={{ width: 140 }} margin="normal" label={t('bills:paymentCode')} value={form.paymentCode}
                onChange={set('paymentCode')} helperText={t('bills:paymentCodeHint')} inputProps={{ maxLength: 3, inputMode: 'numeric' }}
              />
              <TextField
                sx={{ width: 100 }} margin="normal" label={t('bills:referenceModel')} value={form.paymentModel}
                onChange={set('paymentModel')} inputProps={{ maxLength: 2, inputMode: 'numeric' }}
              />
              <TextField
                sx={{ flex: 1, minWidth: 180 }} margin="normal" label={t('bills:referenceNumber')} value={form.paymentReference}
                onChange={set('paymentReference')} inputProps={{ maxLength: 22 }}
              />
            </Box>
          </>
        )}

        {tenantCount > 0 && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            {t('bills:splitHint', { count: tenantCount })}
          </Typography>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common:cancel')}</Button>
        <Button variant="contained" onClick={save} disabled={!canSave || busy}>
          {busy ? <CircularProgress size={22} /> : t('bills:save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default BillFormDialog;
