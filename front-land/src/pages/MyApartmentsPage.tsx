import React from 'react';
import { Container, Typography, Box, Paper, Button } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { Add as AddIcon } from '@mui/icons-material';

const MyApartmentsPage: React.FC = () => {
  const { t } = useTranslation(['common', 'apartments']);

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
        <Typography variant="h4" component="h1">
          {t('myApartments')}
        </Typography>
        <Button variant="contained" color="secondary" startIcon={<AddIcon />}>
          {t('apartments:createApartment', { defaultValue: 'Create Apartment' })}
        </Button>
      </Box>
      <Paper sx={{ p: 4, textAlign: 'center' }}>
        <Typography variant="body1" color="text.secondary">
          {t('apartments:noApartments')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
          Your apartment listings will appear here
        </Typography>
      </Paper>
    </Container>
  );
};

export default MyApartmentsPage;

