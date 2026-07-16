import React, { useState } from 'react';
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Typography, FormControlLabel, Checkbox, Button, Link, Box, Alert,
} from '@mui/material';
import GavelIcon from '@mui/icons-material/Gavel';
import { useTranslation } from 'react-i18next';

interface Props {
  open: boolean;
  planName: string;
  amount: string;
  onConfirm: () => void;
  onCancel: () => void;
}

/**
 * Mandatory withdrawal waiver modal required by Serbian Consumer Protection Act (ZZP čl. 30).
 * The user must explicitly confirm they waive the 14-day right of withdrawal for
 * digital services that begin immediately upon purchase.
 */
const WithdrawalWaiverModal: React.FC<Props> = ({ open, planName, amount, onConfirm, onCancel }) => {
  const { t } = useTranslation('subscriptions');
  const [checked, setChecked] = useState(false);

  const handleClose = () => {
    setChecked(false);
    onCancel();
  };

  const handleConfirm = () => {
    if (!checked) return;
    setChecked(false);
    onConfirm();
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <GavelIcon color="primary" />
        {t('waiverTitle')}
      </DialogTitle>

      <DialogContent>
        <Box sx={{ mb: 2 }}>
          <Typography variant="body1" gutterBottom>
            {t('waiverBuying')} <strong>{planName}</strong>
          </Typography>
          <Typography variant="body1" color="primary" fontWeight="bold" gutterBottom>
            {t('waiverAmount')} {amount}
          </Typography>
        </Box>

        <Alert severity="info" sx={{ mb: 2 }}>
          <Typography variant="body2" dangerouslySetInnerHTML={{ __html: t('waiverBody') }} />
        </Alert>

        <FormControlLabel
          control={
            <Checkbox
              checked={checked}
              onChange={(e) => setChecked(e.target.checked)}
              color="primary"
            />
          }
          label={
            <Typography variant="body2">
              <span dangerouslySetInnerHTML={{ __html: t('waiverCheckbox') }} />{' '}
              <Link href="/politika-povracaja" target="_blank" rel="noopener">
                {t('waiverRefundPolicy')}
              </Link>
            </Typography>
          }
          sx={{ alignItems: 'flex-start', mt: 1 }}
        />
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2, gap: 1 }}>
        <Button variant="outlined" onClick={handleClose}>
          {t('waiverCancel')}
        </Button>
        <Button
          variant="contained"
          onClick={handleConfirm}
          disabled={!checked}
          color="primary"
        >
          {t('waiverConfirm')}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default WithdrawalWaiverModal;
