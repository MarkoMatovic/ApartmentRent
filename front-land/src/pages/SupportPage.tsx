import React from 'react';
import {
  Container,
  Typography,
  Box,
  Paper,
  Grid,
  Link,
} from '@mui/material';
import {
  Phone as PhoneIcon,
  Email as EmailIcon,
} from '@mui/icons-material';
import { useTranslation } from 'react-i18next';

const SupportPage: React.FC = () => {
  const { t } = useTranslation(['common', 'support']);

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={3} sx={{ p: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom align="center" sx={{ mb: 4 }}>
          {t('support:title')}
        </Typography>

        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Box
              sx={{
                p: 3,
                border: 1,
                borderColor: 'divider',
                borderRadius: 2,
                textAlign: 'center',
              }}
            >
              <PhoneIcon sx={{ fontSize: 40, color: 'secondary.main', mb: 2 }} />
              <Typography variant="h6" gutterBottom>
                {t('support:phone')}
              </Typography>
              <Link
                href="tel:+381637721040"
                sx={{
                  color: 'secondary.main',
                  textDecoration: 'none',
                  fontSize: '1.1rem',
                  fontWeight: 500,
                  '&:hover': {
                    textDecoration: 'underline',
                  },
                }}
              >
                0637721040
              </Link>
            </Box>
          </Grid>

          <Grid item xs={12}>
            <Box
              sx={{
                p: 3,
                border: 1,
                borderColor: 'divider',
                borderRadius: 2,
                textAlign: 'center',
              }}
            >
              <EmailIcon sx={{ fontSize: 40, color: 'secondary.main', mb: 2 }} />
              <Typography variant="h6" gutterBottom>
                {t('support:email')}
              </Typography>
              <Link
                href="mailto:marko.matovic.6992@gmail.com"
                sx={{
                  color: 'secondary.main',
                  textDecoration: 'none',
                  fontSize: '1rem',
                  fontWeight: 500,
                  wordBreak: 'break-word',
                  '&:hover': {
                    textDecoration: 'underline',
                  },
                }}
              >
                marko.matovic.6992@gmail.com
              </Link>
            </Box>
          </Grid>
        </Grid>
      </Paper>
    </Container>
  );
};

export default SupportPage;
