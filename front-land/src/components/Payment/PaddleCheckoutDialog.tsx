import React, { useEffect, useRef, useState } from 'react';
import { Dialog, DialogContent, DialogTitle, Box, CircularProgress, Typography, IconButton, Button } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import { paymentsApi } from '../../shared/api/paymentsApi';
import { getPaddle, onPaddleEvent } from '../../shared/payments/paddle';

interface PaddleCheckoutDialogProps {
  open: boolean;
  planId: string | null;
  apartmentId?: number | null;
  onClose: () => void;
  /** Fired once Paddle confirms the payment completed. */
  onSuccess: () => void;
}

// Paddle renders the inline checkout iframe into the element carrying this class.
const FRAME_CLASS = 'paddle-checkout-frame';

/**
 * Inline Paddle checkout inside a modal. On open it asks the backend to create a
 * transaction (which stamps buyer/plan into custom_data server-side), then opens the
 * Paddle inline checkout against that transaction id. Payment completion arrives via the
 * Paddle event stream, not a redirect.
 */
const PaddleCheckoutDialog: React.FC<PaddleCheckoutDialogProps> = ({ open, planId, apartmentId, onClose, onSuccess }) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const openedTxnRef = useRef<string | null>(null);

  // Relay Paddle checkout events → success / close.
  useEffect(() => {
    const unsubscribe = onPaddleEvent((event) => {
      if (event.name === 'checkout.completed') {
        onSuccess();
      } else if (event.name === 'checkout.closed') {
        onClose();
      } else if (event.name === 'checkout.error') {
        setError('Došlo je do greške tokom plaćanja. Pokušajte ponovo.');
      }
    });
    return unsubscribe;
  }, [onSuccess, onClose]);

  // Create the transaction and open the inline checkout whenever the dialog opens.
  useEffect(() => {
    if (!open || !planId) return;

    let cancelled = false;
    setError(null);
    setLoading(true);
    openedTxnRef.current = null;

    (async () => {
      try {
        const { transactionId } = await paymentsApi.createPayment(planId, apartmentId ?? undefined);
        const paddle = await getPaddle();
        if (cancelled) return;

        openedTxnRef.current = transactionId;
        paddle.Checkout.open({
          transactionId,
          settings: {
            displayMode: 'inline',
            frameTarget: FRAME_CLASS,
            frameInitialHeight: 460,
            frameStyle: 'width:100%;min-width:312px;background:transparent;border:none;',
            theme: 'light',
            locale: 'sr',
          },
        });
      } catch (err: any) {
        if (!cancelled) {
          setError(err?.response?.data?.message || err?.message || 'Plaćanje trenutno nije dostupno.');
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => { cancelled = true; };
  }, [open, planId, apartmentId]);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', pr: 1 }}>
        Plaćanje
        <IconButton onClick={onClose} aria-label="Zatvori" size="small">
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        {loading && (
          <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2, py: 5 }}>
            <CircularProgress />
            <Typography variant="body2" color="text.secondary">Priprema sigurnog plaćanja…</Typography>
          </Box>
        )}

        {error && (
          <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2, py: 4, textAlign: 'center' }}>
            <Typography color="error">{error}</Typography>
            <Button variant="outlined" onClick={onClose}>Zatvori</Button>
          </Box>
        )}

        {/* Paddle injects the checkout iframe here. Kept mounted so the frame target exists. */}
        <Box className={FRAME_CLASS} sx={{ display: error ? 'none' : 'block', minHeight: loading ? 0 : 400 }} />
      </DialogContent>
    </Dialog>
  );
};

export default PaddleCheckoutDialog;
