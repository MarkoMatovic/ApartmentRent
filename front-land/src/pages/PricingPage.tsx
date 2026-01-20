import React from 'react';
import {
  Container,
  Typography,
  Box,
  Paper,
  Divider,
} from '@mui/material';
import { useTranslation } from 'react-i18next';

const PricingPage: React.FC = () => {
  const { t } = useTranslation(['common', 'pricing']);

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={3} sx={{ p: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom align="center" sx={{ mb: 4 }}>
          {t('pricing:title')}
        </Typography>

        <Divider sx={{ my: 4 }} />

        {/* Listing Price */}
        <Box sx={{ mb: 4 }}>
          <Typography variant="body1" gutterBottom sx={{ mb: 1 }}>
            {t('pricing:listingTitle')}
          </Typography>
          <Typography variant="h5" color="secondary.main" sx={{ fontWeight: 700 }}>
            {t('pricing:listingPrice')}
          </Typography>
        </Box>

        <Divider sx={{ my: 4 }} />

        {/* Analytics Price */}
        <Box>
          <Typography variant="body1" gutterBottom sx={{ mb: 1 }}>
            {t('pricing:analyticsTitle')}
          </Typography>
          <Typography variant="h5" color="secondary.main" sx={{ fontWeight: 700 }}>
            {t('pricing:analyticsPrice')}
          </Typography>
        </Box>
      </Paper>
    </Container>
  );
};

export default PricingPage;
