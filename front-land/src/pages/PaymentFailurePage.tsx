import React from 'react';
import { Container, Typography, Paper, Button, Box } from '@mui/material';
import { ErrorOutline as ErrorOutlineIcon } from '@mui/icons-material';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

const PaymentFailurePage: React.FC = () => {
  const { t } = useTranslation('payments');
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const responseCode = searchParams.get('pgw_response_code') ?? '';
  const orderNumber  = searchParams.get('order_number') ?? '';

  const errorKeyMap: Record<string, string> = {
    '1001': 'error1001',
    '1007': 'error1007',
    '0023': 'error0023',
    '1003': 'error1003',
    '1005': 'error1005',
    '9999': 'error9999',
  };

  const message = responseCode && errorKeyMap[responseCode]
    ? t(errorKeyMap[responseCode])
    : t('failureDefault');

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={3} sx={{ p: 4, textAlign: 'center' }}>
        <ErrorOutlineIcon sx={{ fontSize: 80, color: 'error.main', mb: 2 }} />
        <Typography variant="h4" gutterBottom>
          {t('failureTitle')}
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 1 }}>
          {message}
        </Typography>
        {orderNumber && (
          <Typography variant="caption" color="text.disabled" display="block" sx={{ mb: 3 }}>
            {t('orderReference')}: {orderNumber}
            {responseCode && ` · ${t('errorCode')}: ${responseCode}`}
          </Typography>
        )}
        <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center', mt: 3 }}>
          <Button variant="contained" onClick={() => navigate('/pricing')}>
            {t('btnRetry')}
          </Button>
          <Button variant="outlined" onClick={() => navigate('/')}>
            {t('btnHome')}
          </Button>
        </Box>
      </Paper>
    </Container>
  );
};

export default PaymentFailurePage;
