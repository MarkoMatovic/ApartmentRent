import React from 'react';
import { Box, Container, Typography, Link, Divider } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

const FooterLink: React.FC<{ to: string; children: React.ReactNode }> = ({ to, children }) => (
  <Link
    component={RouterLink}
    to={to}
    sx={{
      color: 'text.secondary',
      textDecoration: 'none',
      fontSize: '0.85rem',
      '&:hover': { color: 'primary.main', textDecoration: 'underline' },
    }}
  >
    {children}
  </Link>
);

const Footer: React.FC = () => {
  const { t } = useTranslation(['common', 'footer']);

  return (
    <Box
      component="footer"
      sx={{ bgcolor: 'background.paper', py: 4, mt: 'auto', borderTop: 1, borderColor: 'divider' }}
    >
      <Container maxWidth="lg">
        {/* Main navigation links */}
        <Box sx={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'center', gap: { xs: 2, sm: 4 }, mb: 3 }}>
          <FooterLink to="/support">{t('footer:support', 'Podrška')}</FooterLink>
          <FooterLink to="/pricing">{t('footer:pricing', 'Cenovnik')}</FooterLink>
          <FooterLink to="/moje-pretplate">Moje pretplate</FooterLink>
          <FooterLink to="/istorija-placanja">Istorija plaćanja</FooterLink>
        </Box>

        <Divider sx={{ mb: 3 }} />

        {/* Legal links — required by ZET and ZZPL */}
        <Box sx={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'center', gap: { xs: 1.5, sm: 3 }, mb: 2 }}>
          <FooterLink to="/politika-privatnosti">Politika privatnosti</FooterLink>
          <FooterLink to="/uslovi-koriscenja">Uslovi korišćenja</FooterLink>
          <FooterLink to="/politika-kolacica">Politika kolačića</FooterLink>
          <FooterLink to="/politika-povracaja">Politika povraćaja</FooterLink>
        </Box>

        {/* Copyright + legal disclaimer */}
        <Typography variant="caption" color="text.secondary" display="block" textAlign="center" sx={{ mb: 0.5 }}>
          © {new Date().getFullYear()} TuRentaj. Sva prava zadržana.
        </Typography>
        <Typography variant="caption" color="text.secondary" display="block" textAlign="center">
          TuRentaj je oglasna platforma i <strong>ne vrši posredovanje</strong> u prometu i zakupu
          nepokretnosti (Zakon o posredovanju, Sl. gl. RS 95/2013). Kontakt:{' '}
          <Link href="mailto:info@turentaj.com" sx={{ fontSize: 'inherit', color: 'inherit' }}>
            info@turentaj.com
          </Link>
        </Typography>
      </Container>
    </Box>
  );
};

export default Footer;
