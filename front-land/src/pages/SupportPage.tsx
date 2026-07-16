import React from 'react';
import {
  Container,
  Typography,
  Box,
  Paper,
  Grid,
  Link,
  Alert,
} from '@mui/material';
import {
  Email as EmailIcon,
  AccessTime as AccessTimeIcon,
} from '@mui/icons-material';
import { useTranslation } from 'react-i18next';

const SupportPage: React.FC = () => {
  const { t } = useTranslation(['common', 'support']);

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={3} sx={{ p: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom align="center" sx={{ mb: 1 }}>
          {t('support:title', 'Podrška')}
        </Typography>
        <Typography variant="body2" color="text.secondary" align="center" sx={{ mb: 4 }}>
          Tu smo da pomognemo. Odgovaramo u roku od 1–2 radna dana.
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
              <EmailIcon sx={{ fontSize: 40, color: 'secondary.main', mb: 2 }} />
              <Typography variant="h6" gutterBottom>
                {t('support:email', 'Email podrška')}
              </Typography>
              <Link
                href="mailto:info@turentaj.com"
                sx={{
                  color: 'secondary.main',
                  textDecoration: 'none',
                  fontSize: '1.05rem',
                  fontWeight: 600,
                  wordBreak: 'break-word',
                  '&:hover': { textDecoration: 'underline' },
                }}
              >
                info@turentaj.com
              </Link>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                Za opšta pitanja, tehničku podršku i fakture
              </Typography>
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
              <AccessTimeIcon sx={{ fontSize: 40, color: 'secondary.main', mb: 2 }} />
              <Typography variant="h6" gutterBottom>
                Radno vreme podrške
              </Typography>
              <Typography variant="body1">
                Pon – Pet: 09:00 – 17:00 (CET)
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                Vikendi i praznici: odgovor u roku od 48h
              </Typography>
            </Box>
          </Grid>
        </Grid>

        <Alert severity="info" sx={{ mt: 3 }}>
          Za zahteve za povraćaj novca ili pritužbe: priložite broj narudžbenice
          i pošaljite na <strong>info@turentaj.com</strong>.
          Pogledajte i našu{' '}
          <Link href="/politika-povracaja" underline="hover">Politiku povraćaja</Link>.
        </Alert>
      </Paper>
    </Container>
  );
};

export default SupportPage;
