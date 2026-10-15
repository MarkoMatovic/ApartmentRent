import React, { useState } from 'react';
import {
  Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField,
} from '@mui/material';
import SendToRoommateIcon from '@mui/icons-material/GroupAdd';
import CheckIcon from '@mui/icons-material/Check';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNotifications } from '../../shared/context/NotificationContext';
import { useMyPairing } from '../../hooks/useMyPairing';
import { roommatePairingApi } from '../../shared/api/roommatePairing';
import { getApiErrorMessage } from '../../shared/utils/apiError';
import { SHARED_APARTMENTS_KEY } from '../../shared/api/queryKeys';

interface Props {
  apartmentId: number;
}

/**
 * "Send to roommate": puts this apartment on the shared list of the two roommates (they both see it
 * on the "my roommate" page) and lets the other one know. Renders nothing for someone without a
 * confirmed roommate.
 */
const ShareWithRoommateButton: React.FC<Props> = ({ apartmentId }) => {
  const { t } = useTranslation(['pairing', 'validation', 'common']);
  const { addNotification } = useNotifications();
  const { partner } = useMyPairing();
  const [open, setOpen] = useState(false);
  const [note, setNote] = useState('');

  const { data: shared, refetch } = useQuery({
    queryKey: [SHARED_APARTMENTS_KEY, partner?.pairingId],
    queryFn: roommatePairingApi.getSharedApartments,
    enabled: !!partner,
    staleTime: 30_000,
  });
  const alreadyShared = shared?.some(a => a.apartmentId === apartmentId) ?? false;

  const share = useMutation({
    mutationFn: () => roommatePairingApi.shareApartment(apartmentId, note),
    onSuccess: () => {
      setOpen(false);
      setNote('');
      refetch();
      addNotification({ title: t('apartmentShared'), message: '', type: 'success' });
    },
    onError: (error) => addNotification({ title: t('sendToRoommate'), message: getApiErrorMessage(error, t), type: 'error' }),
  });

  if (!partner) return null;

  return (
    <>
      <Button
        fullWidth
        variant="outlined"
        size="large"
        startIcon={alreadyShared ? <CheckIcon /> : <SendToRoommateIcon />}
        sx={{ mt: 1 }}
        onClick={() => setOpen(true)}
        disabled={alreadyShared}
      >
        {alreadyShared ? t('onSharedList') : t('sendToRoommate')}
      </Button>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>{t('sendDialogTitle', { name: partner.partnerName })}</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus fullWidth multiline minRows={2} margin="dense"
            label={t('noteLabel')} helperText={t('noteHelper')}
            value={note} onChange={(e) => setNote(e.target.value)}
            inputProps={{ maxLength: 500 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>{t('common:cancel')}</Button>
          <Button variant="contained" onClick={() => share.mutate()} disabled={share.isPending}>
            {t('sendConfirm')}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default ShareWithRoommateButton;
