import React from 'react';
import { Box, Container, Typography, Link } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

const Footer: React.FC = () => {
  const { t } = useTranslation(['common', 'footer']);

  return (
    <Box
      component="footer"
      sx={{
        bgcolor: 'background.paper',
        py: 3,
        mt: 'auto',
        borderTop: 1,
        borderColor: 'divider',
      }}
    >
      <Container maxWidth="lg">
        <Box sx={{ display: 'flex', justifyContent: 'center', gap: 4, mb: 2 }}>
          <Link
            component={RouterLink}
            to="/support"
            sx={{
              color: 'text.primary',
              textDecoration: 'none',
              fontSize: '1rem',
              fontWeight: 500,
              '&:hover': {
                color: 'secondary.main',
                textDecoration: 'underline',
              },
            }}
          >
            {t('footer:support')}
          </Link>
          <Link
            component={RouterLink}
            to="/pricing"
            sx={{
              color: 'text.primary',
              textDecoration: 'none',
              fontSize: '1rem',
              fontWeight: 500,
              '&:hover': {
                color: 'secondary.main',
                textDecoration: 'underline',
              },
            }}
          >
            {t('footer:pricing')}
          </Link>
        </Box>
        <Typography variant="body2" color="text.secondary" align="center">
          © {new Date().getFullYear()} Landlander. All rights reserved.
        </Typography>
      </Container>
    </Box>
  );
};

export default Footer;

