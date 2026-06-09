import React, { useState, useEffect } from 'react';
import { Box, Typography, Button, Link, Paper, Slide } from '@mui/material';
import CookieIcon from '@mui/icons-material/Cookie';

const CONSENT_KEY = 'cookieConsent';

const CookieConsentBanner: React.FC = () => {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    // Check consent on mount — show banner only if not yet decided
    const stored = localStorage.getItem(CONSENT_KEY);
    if (!stored) setVisible(true);
  }, []);

  const accept = () => {
    localStorage.setItem(CONSENT_KEY, 'accepted');
    setVisible(false);
  };

  const decline = () => {
    // Decline means no analytics/optional cookies (we only use functional so this is informational)
    localStorage.setItem(CONSENT_KEY, 'declined');
    setVisible(false);
  };

  if (!visible) return null;

  return (
    <Slide direction="up" in={visible} mountOnEnter unmountOnExit>
      <Paper
        elevation={8}
        sx={{
          position: 'fixed',
          bottom: 0,
          left: 0,
          right: 0,
          zIndex: 9999,
          px: { xs: 2, sm: 4 },
          py: 2.5,
          display: 'flex',
          flexDirection: { xs: 'column', md: 'row' },
          alignItems: { xs: 'flex-start', md: 'center' },
          gap: 2,
          bgcolor: 'background.paper',
          borderTop: 2,
          borderColor: 'primary.main',
        }}
      >
        <CookieIcon sx={{ color: 'primary.main', flexShrink: 0, fontSize: 32 }} />

        <Box sx={{ flex: 1 }}>
          <Typography variant="body2" fontWeight="bold" gutterBottom>
            Koristimo kolačiće (cookies)
          </Typography>
          <Typography variant="body2" color="text.secondary">
            TuRentaj koristi isključivo <strong>funkcionalne kolačiće</strong> neophodne za sigurnu
            autentifikaciju (httpOnly refresh token kolačić). Ne koristimo kolačiće za praćenje ni
            ciljano oglašavanje.{' '}
            <Link href="/politika-kolacica" sx={{ whiteSpace: 'nowrap' }}>
              Saznajte više
            </Link>
          </Typography>
        </Box>

        <Box sx={{ display: 'flex', gap: 1.5, flexShrink: 0, flexWrap: 'wrap' }}>
          <Button
            variant="outlined"
            size="small"
            onClick={decline}
            sx={{ whiteSpace: 'nowrap' }}
          >
            Samo neophodni
          </Button>
          <Button
            variant="contained"
            size="small"
            onClick={accept}
            sx={{ whiteSpace: 'nowrap' }}
          >
            Prihvatam
          </Button>
        </Box>
      </Paper>
    </Slide>
  );
};

export default CookieConsentBanner;
