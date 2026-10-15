import React, { useEffect, useState } from 'react';
import {
  Alert, Avatar, Box, Button, Checkbox, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControlLabel, MenuItem, TextField, Typography,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { apartmentsApi } from '../../shared/api/apartments';
import {
  billsApi, CreateTenancyInput, isValidSerbianAccount, Tenancy, TenancyInput, todayIso,
} from '../../shared/api/bills';
import { getApiErrorMessage } from '../../shared/utils/apiError';

interface Props {
  open: boolean;
  /** Set to change the rent settings of an existing tenancy. */
  tenancy?: Tenancy | null;
  /** The apartment to start with (e.g. from the apartment page). */
  initialApartmentId?: number;
  defaultPayeeName?: string;
  onClose: () => void;
  onCreate: (input: CreateTenancyInput) => Promise<void>;
  onUpdate: (tenancyId: number, input: TenancyInput) => Promise<void>;
}

/** Rent of one of the landlord's apartments: who lives there, how much, by when and to which account. */
const TenancyFormDialog: React.FC<Props> = ({ open, tenancy, initialApartmentId, defaultPayeeName, onClose, onCreate, onUpdate }) => {
  const { t } = useTranslation(['bills', 'common', 'validation']);
  const editing = tenancy != null;
  const [apartmentId, setApartmentId] = useState<number | ''>('');
  const [tenantIds, setTenantIds] = useState<number[]>([]);
  const [rent, setRent] = useState('');
  const [dueDay, setDueDay] = useState('5');
  const [payeeName, setPayeeName] = useState('');
  const [payeeAccount, setPayeeAccount] = useState('');
  const [startDate, setStartDate] = useState(todayIso());
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!open) return;
    setError('');
    setTenantIds([]);
    setApartmentId(tenancy?.apartmentId ?? initialApartmentId ?? '');
    setRent(tenancy ? String(tenancy.monthlyRent) : '');
    setDueDay(tenancy ? String(tenancy.rentDueDay) : '5');
    setPayeeName(tenancy?.payeeName ?? defaultPayeeName ?? '');
    setPayeeAccount(tenancy?.payeeAccount ?? '');
    setStartDate(todayIso());
  }, [open, tenancy, initialApartmentId, defaultPayeeName]);

  const { data: apartments = [], isLoading: loadingApartments } = useQuery({
    queryKey: ['my-apartments-for-tenancy'],
    queryFn: () => apartmentsApi.getMyApartments(),
    enabled: open && !editing,
  });

  const { data: candidates = [], isFetching: loadingCandidates } = useQuery({
    queryKey: ['tenancy-candidates', apartmentId],
    queryFn: () => billsApi.getCandidates(Number(apartmentId)),
    enabled: open && !editing && apartmentId !== '',
  });

  const rentValue = Number(rent.replace(',', '.'));
  const rentValid = Number.isFinite(rentValue) && rentValue > 0;
  const dayValue = Number(dueDay);
  const dayValid = Number.isInteger(dayValue) && dayValue >= 1 && dayValue <= 28;
  const accountValid = isValidSerbianAccount(payeeAccount);
  const canSave = rentValid && dayValid && accountValid && payeeName.trim().length > 0
    && (editing || (apartmentId !== '' && tenantIds.length > 0 && !!startDate));

  const toggle = (userId: number) =>
    setTenantIds(prev => (prev.includes(userId) ? prev.filter(id => id !== userId) : [...prev, userId]));

  const save = async () => {
    setBusy(true);
    setError('');
    const settings: TenancyInput = {
      monthlyRent: Math.round(rentValue * 100) / 100,
      rentDueDay: dayValue,
      payeeName: payeeName.trim(),
      payeeAccount: payeeAccount.trim(),
    };
    try {
      if (tenancy) await onUpdate(tenancy.tenancyId, settings);
      else await onCreate({ ...settings, apartmentId: Number(apartmentId), tenantUserIds: tenantIds, startDate });
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, t));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{editing ? t('bills:settings') : t('bills:newTenancy')}</DialogTitle>
      <DialogContent>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        {editing && <Typography variant="body2" color="text.secondary">{t('bills:settingsHint')}</Typography>}

        {!editing && (
          <>
            {loadingApartments ? (
              <Box sx={{ py: 2, textAlign: 'center' }}><CircularProgress size={24} /></Box>
            ) : apartments.length === 0 ? (
              <Alert severity="info" sx={{ my: 2 }}>{t('bills:noApartments')}</Alert>
            ) : (
              <TextField
                select fullWidth margin="normal" label={t('bills:apartment')} value={apartmentId}
                onChange={e => { setApartmentId(Number(e.target.value)); setTenantIds([]); }}
              >
                {apartments.map(a => <MenuItem key={a.apartmentId} value={a.apartmentId}>{a.title}</MenuItem>)}
              </TextField>
            )}

            {apartmentId !== '' && (
              <Box sx={{ mt: 1 }}>
                <Typography variant="subtitle2">{t('bills:tenants')}</Typography>
                {loadingCandidates ? (
                  <CircularProgress size={20} sx={{ my: 1 }} />
                ) : candidates.length === 0 ? (
                  <Alert severity="info" sx={{ my: 1 }}>{t('bills:noCandidates')}</Alert>
                ) : (
                  candidates.map(c => (
                    <FormControlLabel
                      key={c.userId}
                      sx={{ display: 'flex', my: 0.5 }}
                      control={<Checkbox checked={tenantIds.includes(c.userId)} onChange={() => toggle(c.userId)} />}
                      label={
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <Avatar src={c.profilePicture || undefined} sx={{ width: 28, height: 28 }}>{c.name[0]}</Avatar>
                          <span>{c.name}</span>
                          <Typography component="span" variant="caption" color="text.secondary">
                            ({c.reason === 'roommate' ? t('bills:candidateRoommate') : t('bills:candidateApplication')})
                          </Typography>
                        </Box>
                      }
                    />
                  ))
                )}
              </Box>
            )}
          </>
        )}

        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
          <TextField
            sx={{ flex: 2, minWidth: 180 }} margin="normal" label={t('bills:monthlyRent')} value={rent}
            onChange={e => setRent(e.target.value)} inputProps={{ inputMode: 'decimal' }} error={rent !== '' && !rentValid}
            helperText={t('bills:rentRsdHint')}
          />
          <TextField
            sx={{ flex: 1, minWidth: 140 }} margin="normal" label={t('bills:dueDay')} value={dueDay}
            onChange={e => setDueDay(e.target.value)} inputProps={{ inputMode: 'numeric', maxLength: 2 }}
            error={!dayValid}
          />
        </Box>
        <TextField
          fullWidth margin="normal" label={t('bills:payeeName')} value={payeeName}
          onChange={e => setPayeeName(e.target.value)} inputProps={{ maxLength: 70 }}
        />
        <TextField
          fullWidth margin="normal" label={t('bills:payeeAccount')} value={payeeAccount}
          onChange={e => setPayeeAccount(e.target.value)} placeholder="160-0000000123456-78"
          error={payeeAccount !== '' && !accountValid}
          helperText={payeeAccount !== '' && !accountValid ? t('bills:accountInvalid') : ' '}
        />
        {!editing && (
          <TextField
            margin="normal" type="date" label={t('bills:startDate')} value={startDate}
            onChange={e => setStartDate(e.target.value)} InputLabelProps={{ shrink: true }}
          />
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common:cancel')}</Button>
        <Button variant="contained" onClick={save} disabled={!canSave || busy}>
          {busy ? <CircularProgress size={22} /> : editing ? t('bills:save') : t('bills:createTenancy')}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default TenancyFormDialog;
