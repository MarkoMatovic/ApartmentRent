import React, { useState } from 'react';
import {
  Container,
  Typography,
  Box,
  Paper,
  Divider,
  Button,
  CircularProgress,
} from '@mui/material';
import { useTranslation } from 'react-i18next';
import { paymentsApi } from '../shared/api/paymentsApi';
import { useNotifications } from '../shared/context/NotificationContext';

const PricingPage: React.FC = () => {
  const { t } = useTranslation(['common', 'pricing']);
  const { addNotification } = useNotifications();
  const [loading, setLoading] = useState(false);

  const handleSubscribe = async (priceId: string) => {
    setLoading(true);
    try {
      const { url } = await paymentsApi.createCheckoutSession(
        priceId,
        `${window.location.origin}/payment-success`,
        `${window.location.origin}/pricing`
      );
      window.location.href = url;
    } catch (error: any) {
      addNotification({
        title: 'Error',
        message: error.response?.data?.error || 'Failed to create checkout session',
        type: 'error'
      });
      setLoading(false);
    }
  };

  return (
    <Container maxWidth="sm" sx={{ py: { xs: 4, md: 8 }, px: { xs: 2, md: 3 } }}>
      <Paper elevation={3} sx={{ p: { xs: 2, md: 4 } }}>
        <Typography variant="h4" component="h1" gutterBottom align="center" sx={{ mb: 4 }}>
          {t('pricing:title')}
        </Typography>

        <Divider sx={{ my: 4 }} />

        {/* Personal Analytics */}
        <Box sx={{ mb: 4 }}>
          <Typography variant="body1" gutterBottom sx={{ mb: 1 }}>
            {t('pricing:analyticsRoommateTitle')}
          </Typography>
          <Typography variant="h5" color="secondary.main" sx={{ fontWeight: 700, mb: 2 }}>
            {t('pricing:analyticsRoommatePrice')}
          </Typography>
          <Button
            variant="contained"
            color="primary"
            fullWidth
            onClick={() => handleSubscribe('price_personal_analytics')}
            disabled={loading}
          >
            {loading ? <CircularProgress size={24} /> : 'Subscribe'}
          </Button>
        </Box>

        <Divider sx={{ my: 4 }} />

        {/* Landlord Analytics */}
        <Box>
          <Typography variant="body1" gutterBottom sx={{ mb: 1 }}>
            {t('pricing:analyticsLandlordTitle')}
          </Typography>
          <Typography variant="h5" color="secondary.main" sx={{ fontWeight: 700, mb: 2 }}>
            {t('pricing:analyticsLandlordPrice')}
          </Typography>
          <Button
            variant="contained"
            color="secondary"
            fullWidth
            onClick={() => handleSubscribe('price_landlord_analytics')}
            disabled={loading}
          >
            {loading ? <CircularProgress size={24} /> : 'Subscribe'}
          </Button>
        </Box>
      </Paper>
    </Container>
  );
};

export default PricingPage;
