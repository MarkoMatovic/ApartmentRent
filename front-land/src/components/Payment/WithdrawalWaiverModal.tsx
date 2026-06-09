import React, { useState } from 'react';
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Typography, FormControlLabel, Checkbox, Button, Link, Box, Alert,
} from '@mui/material';
import GavelIcon from '@mui/icons-material/Gavel';

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
        Potvrda kupovine digitalne usluge
      </DialogTitle>

      <DialogContent>
        <Box sx={{ mb: 2 }}>
          <Typography variant="body1" gutterBottom>
            Kupujete: <strong>{planName}</strong>
          </Typography>
          <Typography variant="body1" color="primary" fontWeight="bold" gutterBottom>
            Iznos: {amount}
          </Typography>
        </Box>

        <Alert severity="info" sx={{ mb: 2 }}>
          <Typography variant="body2">
            <strong>Obaveštenje u skladu sa Zakonom o zaštiti potrošača RS (čl. 30, st. 6):</strong>
            <br />
            Ova usluga je <strong>digitalne prirode</strong> i biće aktivirana <strong>odmah</strong> po
            potvrdi plaćanja. Prema srpskom pravu, kupovinom digitalne usluge koja počinje odmah,
            odričete se prava na odustajanje od ugovora u roku od 14 dana.
          </Typography>
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
              <strong>Razumijem i izričito prihvatam</strong> da aktivacijom digitalne usluge odmah
              po kupovini <strong>gubim pravo na odustajanje od ugovora u roku od 14 dana</strong>,
              u skladu sa čl. 30, st. 6 Zakona o zaštiti potrošača Republike Srbije.{' '}
              <Link href="/politika-povracaja" target="_blank" rel="noopener">
                Politika povraćaja
              </Link>
            </Typography>
          }
          sx={{ alignItems: 'flex-start', mt: 1 }}
        />
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2, gap: 1 }}>
        <Button variant="outlined" onClick={handleClose}>
          Odustani
        </Button>
        <Button
          variant="contained"
          onClick={handleConfirm}
          disabled={!checked}
          color="primary"
        >
          Nastavi na plaćanje
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default WithdrawalWaiverModal;
