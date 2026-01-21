import React from 'react';
import { Box, Paper, Typography, Button } from '@mui/material';
import { Lock as LockIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../shared/context/AuthContext';

interface PremiumBlurProps {
  children: React.ReactNode;
  feature: 'personalAnalytics' | 'landlordAnalytics';
  requiredFeature: string;
}

const PremiumBlur: React.FC<PremiumBlurProps> = ({ children, feature, requiredFeature }) => {
  const { user, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const { t } = useTranslation(['common', 'premium']);

  // If user is not authenticated, show content normally (they'll be redirected elsewhere)
  if (!isAuthenticated || !user) {
    return <>{children}</>;
  }

  // TEMPORARY: Always show content for testing
  return <>{children}</>;

  // Original logic (commented out for testing):
  // const hasAccess = feature === 'personalAnalytics' 
  //   ? user?.hasPersonalAnalytics 
  //   : user?.hasLandlordAnalytics;

  // if (hasAccess) {
  //   return <>{children}</>;
  // }

  return (
    <Box sx={{ position: 'relative' }}>
      {/* Blurred content */}
      <Box
        sx={{
          filter: 'blur(5px)',
          pointerEvents: 'none',
          userSelect: 'none',
          opacity: 0.5,
        }}
      >
        {children}
      </Box>

      {/* Overlay with upgrade message */}
      <Box
        sx={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          bgcolor: 'rgba(255, 255, 255, 0.9)',
          zIndex: 10,
        }}
      >
        <Paper
          elevation={3}
          sx={{
            p: 4,
            textAlign: 'center',
            maxWidth: 400,
            mx: 2,
          }}
        >
          <LockIcon sx={{ fontSize: 60, color: 'secondary.main', mb: 2 }} />
          <Typography variant="h5" gutterBottom>
            {t('premium:lockedTitle') || 'Premium Funkcija'}
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
            {String(t('premium:lockedMessage', { feature: requiredFeature }) || `Ova funkcija je dostupna samo za premium korisnike. ${requiredFeature}`)}
          </Typography>
          <Button
            variant="contained"
            color="secondary"
            size="large"
            onClick={() => navigate('/pricing')}
            sx={{ minWidth: 200 }}
          >
            {String(t('premium:upgradeButton') || 'Nadogradi na Premium')}
          </Button>
        </Paper>
      </Box>
    </Box>
  );
};

export default PremiumBlur;
