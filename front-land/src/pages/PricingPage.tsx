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

  const handleSubscribe = async (planId: string) => {
    setLoading(true);
    try {
      const formFields = await paymentsApi.createPayment(
        planId,
        `${window.location.origin}/payment-success`,
        `${window.location.origin}/payment-failure`
      );

      // Dynamically build and submit a form to Monri's hosted payment page
      const form = document.createElement('form');
      form.method = 'POST';
      form.action = formFields.formAction;

      const fields: Record<string, string> = {
        authenticity_token: formFields.authenticityToken,
        order_number: formFields.orderNumber,
        amount: String(formFields.amount),
        currency: formFields.currency,
        order_info: formFields.orderInfo,
        digest: formFields.digest,
        success_url_override: formFields.successUrl,
        failure_url_override: formFields.failureUrl,
        callback_url: formFields.callbackUrl,
        buyer_name: formFields.buyerName,
        buyer_email: formFields.buyerEmail,
        language: 'sr',
      };

      Object.entries(fields).forEach(([name, value]) => {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        input.value = value;
        form.appendChild(input);
      });

      document.body.appendChild(form);
      form.submit();
    } catch (error: any) {
      addNotification({
        title: 'Greška',
        message: error.response?.data?.error || 'Nije moguće pokrenuti plaćanje',
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
            onClick={() => handleSubscribe('personal_analytics')}
            disabled={loading}
          >
            {loading ? <CircularProgress size={24} /> : 'Pretplati se'}
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
            onClick={() => handleSubscribe('landlord_analytics')}
            disabled={loading}
          >
            {loading ? <CircularProgress size={24} /> : 'Pretplati se'}
          </Button>
        </Box>
      </Paper>
    </Container>
  );
};

export default PricingPage;
