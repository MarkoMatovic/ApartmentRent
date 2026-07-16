import React, { useEffect, useState } from 'react';
import {
  Container, Typography, Box, Table, TableBody, TableCell, TableHead,
  TableRow, Paper, CircularProgress, Alert, Button, Chip,
} from '@mui/material';
import ReceiptIcon from '@mui/icons-material/Receipt';
import { useTranslation } from 'react-i18next';
import { apiClient } from '../shared/api/client';
import { useNotifications } from '../shared/context/NotificationContext';
import { useNavigate } from 'react-router-dom';

interface PaymentOrder {
  orderNumber: string;
  planId: string;
  planName: string;
  amount: number;
  currency: string;
  processedAt: string;
}

const PaymentHistoryPage: React.FC = () => {
  const { t } = useTranslation('payments');
  const [orders, setOrders] = useState<PaymentOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const { addNotification } = useNotifications();
  const navigate = useNavigate();

  useEffect(() => {
    apiClient.get('/api/payments/my-orders')
      .then(res => setOrders(res.data ?? []))
      .catch(() => addNotification({ title: t('common:error', { defaultValue: 'Greška' }), message: t('errorLoading'), type: 'error' }))
      .finally(() => setLoading(false));
  }, []);

  const formatDate = (iso: string) =>
    new Date(iso).toLocaleDateString(undefined, { day: '2-digit', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' });

  const formatAmount = (amount: number, currency: string) =>
    `${amount.toFixed(2)} ${currency}`;

  const getPlanCategory = (planId: string): { label: string; color: 'primary' | 'success' | 'warning' | 'info' | 'default' } => {
    if (planId.startsWith('analytics')) return { label: t('catAnalytics'), color: 'primary' };
    if (planId.startsWith('tokens'))    return { label: t('catTokens'),    color: 'success' };
    if (planId.startsWith('featured'))  return { label: t('catFeatured'),  color: 'warning' };
    if (planId.startsWith('listing'))   return { label: t('catListing'),   color: 'info' };
    if (planId === 'boost-7')           return { label: t('catBoost'),     color: 'primary' };
    if (planId === 'priority-30')       return { label: t('catPriority'),  color: 'info' };
    return { label: planId, color: 'default' };
  };

  if (loading) return <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}><CircularProgress /></Box>;

  return (
    <Container maxWidth="lg" sx={{ py: 5 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1 }}>
        <ReceiptIcon fontSize="large" color="primary" />
        <Typography variant="h4" fontWeight="bold">{t('historyTitle')}</Typography>
      </Box>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 4 }}>
        {t('historySubtitle')}
      </Typography>

      {orders.length === 0 ? (
        <Alert severity="info" sx={{ mb: 3 }}
          dangerouslySetInnerHTML={{ __html: t('noTransactions') }}
        />
      ) : (
        <Paper variant="outlined">
          <Table>
            <TableHead>
              <TableRow sx={{ bgcolor: 'action.hover' }}>
                <TableCell><strong>{t('colDate')}</strong></TableCell>
                <TableCell><strong>{t('colService')}</strong></TableCell>
                <TableCell><strong>{t('colCategory')}</strong></TableCell>
                <TableCell align="right"><strong>{t('colAmount')}</strong></TableCell>
                <TableCell><strong>{t('colOrderNo')}</strong></TableCell>
                <TableCell><strong>{t('colStatus')}</strong></TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {orders.map(order => {
                const cat = getPlanCategory(order.planId);
                return (
                  <TableRow key={order.orderNumber} hover>
                    <TableCell sx={{ whiteSpace: 'nowrap' }}>
                      {formatDate(order.processedAt)}
                    </TableCell>
                    <TableCell>{order.planName}</TableCell>
                    <TableCell>
                      <Chip label={cat.label} color={cat.color} size="small" variant="outlined" />
                    </TableCell>
                    <TableCell align="right" sx={{ fontWeight: 'bold' }}>
                      {formatAmount(order.amount, order.currency)}
                    </TableCell>
                    <TableCell>
                      <Typography variant="caption" sx={{ fontFamily: 'monospace' }}>
                        {order.orderNumber}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Chip label={t('statusSuccess')} color="success" size="small" />
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </Paper>
      )}

      <Alert severity="info" sx={{ mt: 3 }} icon={<ReceiptIcon />}>
        <Typography variant="body2">
          <strong>{t('invoiceNote')}</strong> {t('invoiceNoteBody')}
        </Typography>
      </Alert>

      <Box sx={{ display: 'flex', gap: 2, mt: 3 }}>
        <Button variant="outlined" onClick={() => navigate('/moje-pretplate')}>← {t('btnSubscriptions')}</Button>
        <Button variant="outlined" onClick={() => navigate('/pricing')}>{t('btnBuyServices')}</Button>
        <Button variant="text" href="/politika-povracaja" size="small" sx={{ ml: 'auto' }}>
          {t('btnRefundPolicy')}
        </Button>
      </Box>
    </Container>
  );
};

export default PaymentHistoryPage;
