import React, { useEffect, useState } from 'react';
import {
  Container, Typography, Box, Table, TableBody, TableCell, TableHead,
  TableRow, Paper, CircularProgress, Alert, Button, Chip,
} from '@mui/material';
import ReceiptIcon from '@mui/icons-material/Receipt';
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
  const [orders, setOrders] = useState<PaymentOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const { addNotification } = useNotifications();
  const navigate = useNavigate();

  useEffect(() => {
    apiClient.get('/api/payments/my-orders')
      .then(res => setOrders(res.data ?? []))
      .catch(() => addNotification({ title: 'Greška', message: 'Nije moguće učitati istoriju plaćanja.', type: 'error' }))
      .finally(() => setLoading(false));
  }, []);

  const formatDate = (iso: string) =>
    new Date(iso).toLocaleDateString('sr-RS', { day: '2-digit', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' });

  const formatAmount = (amount: number, currency: string) =>
    `${amount.toFixed(2)} ${currency}`;

  const getPlanCategory = (planId: string): { label: string; color: 'primary' | 'success' | 'warning' | 'info' | 'default' } => {
    if (planId.startsWith('analytics')) return { label: 'Analitika', color: 'primary' };
    if (planId.startsWith('tokens')) return { label: 'Tokeni', color: 'success' };
    if (planId.startsWith('featured')) return { label: 'Isticanje oglasa', color: 'warning' };
    if (planId.startsWith('listing')) return { label: 'Listing kredit', color: 'info' };
    if (planId === 'boost-7') return { label: 'Boost profila', color: 'primary' };
    if (planId === 'priority-30') return { label: 'Priority Inbox', color: 'info' };
    return { label: planId, color: 'default' };
  };

  if (loading) return <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}><CircularProgress /></Box>;

  return (
    <Container maxWidth="lg" sx={{ py: 5 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1 }}>
        <ReceiptIcon fontSize="large" color="primary" />
        <Typography variant="h4" fontWeight="bold">Istorija plaćanja</Typography>
      </Box>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 4 }}>
        Sve obrađene transakcije na vašem nalogu. Za fiskalni račun ili potvrdu transakcije
        kontaktirajte nas na <strong>info@turentaj.com</strong> s brojem narudžbenice.
      </Typography>

      {orders.length === 0 ? (
        <Alert severity="info" sx={{ mb: 3 }}>
          Nemate nijednu zabeleženu transakciju. Kupite premium uslugu na{' '}
          <strong>Cenovniku</strong> da biste je videli ovdje.
        </Alert>
      ) : (
        <Paper variant="outlined">
          <Table>
            <TableHead>
              <TableRow sx={{ bgcolor: 'action.hover' }}>
                <TableCell><strong>Datum</strong></TableCell>
                <TableCell><strong>Usluga</strong></TableCell>
                <TableCell><strong>Kategorija</strong></TableCell>
                <TableCell align="right"><strong>Iznos</strong></TableCell>
                <TableCell><strong>Br. narudžbenice</strong></TableCell>
                <TableCell><strong>Status</strong></TableCell>
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
                      <Chip label="Uspešno" color="success" size="small" />
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
          <strong>Napomena o računima:</strong> Za izdavanje formalnog fiskalnog/elektronskog računa (e-faktura)
          koji je u skladu sa Zakonom o elektronskom fakturisanju RS, kontaktirajte nas na{' '}
          <strong>info@turentaj.com</strong> s brojem narudžbenice i vašim PIB-om (za pravna lica).
          Odgovaramo u roku od 2 radna dana.
        </Typography>
      </Alert>

      <Box sx={{ display: 'flex', gap: 2, mt: 3 }}>
        <Button variant="outlined" onClick={() => navigate('/moje-pretplate')}>← Moje pretplate</Button>
        <Button variant="outlined" onClick={() => navigate('/pricing')}>Kupovina usluga</Button>
        <Button variant="text" href="/politika-povracaja" size="small" sx={{ ml: 'auto' }}>
          Politika povraćaja
        </Button>
      </Box>
    </Container>
  );
};

export default PaymentHistoryPage;
