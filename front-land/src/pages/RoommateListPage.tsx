import React from 'react';
import { Container, Typography, Box, Paper } from '@mui/material';
import { useTranslation } from 'react-i18next';

const RoommateListPage: React.FC = () => {
  const { t } = useTranslation(['common', 'roommates']);

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        {t('roommates:title')}
      </Typography>
      <Paper sx={{ p: 4, textAlign: 'center' }}>
        <Typography variant="body1" color="text.secondary">
          {t('roommates:noRoommates')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
          Roommate functionality will be implemented
        </Typography>
      </Paper>
    </Container>
  );
};

export default RoommateListPage;

