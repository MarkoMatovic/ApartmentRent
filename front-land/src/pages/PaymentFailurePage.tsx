import React from 'react';
import { Container, Typography, Paper, Button, Box } from '@mui/material';
import { ErrorOutline as ErrorOutlineIcon } from '@mui/icons-material';
import { useNavigate, useSearchParams } from 'react-router-dom';

const ERROR_MESSAGES: Record<string, string> = {
  '1001': 'Kartica je odbijena. Proverite podatke kartice ili kontaktirajte banku.',
  '1007': 'Prekoračen je limit kartice.',
  '0023': 'Plaćanje je otkazano.',
  '1003': 'Nevažeći broj kartice.',
  '1005': 'Nevažeći datum isteka kartice.',
  '9999': 'Greška platnog sistema. Pokušajte ponovo.',
};

const resolveMessage = (code: string): string =>
  ERROR_MESSAGES[code] ?? 'Greška pri plaćanju. Pokušajte ponovo ili kontaktirajte podršku.';

const PaymentFailurePage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const responseCode = searchParams.get('pgw_response_code') ?? '';
  const orderNumber = searchParams.get('order_number') ?? '';
  const message = resolveMessage(responseCode);

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={3} sx={{ p: 4, textAlign: 'center' }}>
        <ErrorOutlineIcon sx={{ fontSize: 80, color: 'error.main', mb: 2 }} />
        <Typography variant="h4" gutterBottom>
          Plaćanje neuspešno
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 1 }}>
          {message}
        </Typography>
        {orderNumber && (
          <Typography variant="caption" color="text.disabled" display="block" sx={{ mb: 3 }}>
            Referenca narudžbine: {orderNumber}
            {responseCode && ` · Kod: ${responseCode}`}
          </Typography>
        )}
        <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center', mt: 3 }}>
          <Button variant="contained" onClick={() => navigate('/pricing')}>
            Pokušaj ponovo
          </Button>
          <Button variant="outlined" onClick={() => navigate('/')}>
            Nazad na početnu
          </Button>
        </Box>
      </Paper>
    </Container>
  );
};

export default PaymentFailurePage;
