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
  const { t } = useTranslation(['common', 'footer', 'payments']);

  return (
    <Box
      component="footer"
      sx={{ bgcolor: 'background.paper', py: 4, mt: 'auto', borderTop: 1, borderColor: 'divider' }}
    >
      <Container maxWidth="lg">
        {/* Main navigation links */}
        <Box sx={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'center', gap: { xs: 2, sm: 4 }, mb: 3 }}>
          <FooterLink to="/support">{t('footer:support')}</FooterLink>
          <FooterLink to="/pricing">{t('footer:pricing')}</FooterLink>
          <FooterLink to="/moje-pretplate">{t('payments:btnSubscriptions')}</FooterLink>
          <FooterLink to="/istorija-placanja">{t('payments:historyTitle')}</FooterLink>
        </Box>

        <Divider sx={{ mb: 3 }} />

        {/* Legal links — required by ZET and ZZPL */}
        <Box sx={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'center', gap: { xs: 1.5, sm: 3 }, mb: 2 }}>
          <FooterLink to="/politika-privatnosti">{t('footer:privacy')}</FooterLink>
          <FooterLink to="/uslovi-koriscenja">{t('footer:terms')}</FooterLink>
          <FooterLink to="/politika-kolacica">{t('footer:cookies')}</FooterLink>
          <FooterLink to="/politika-povracaja">{t('footer:refund')}</FooterLink>
        </Box>

        {/* Copyright + legal disclaimer */}
        <Typography variant="caption" color="text.secondary" display="block" textAlign="center" sx={{ mb: 0.5 }}>
          © {new Date().getFullYear()} TuRentaj. {t('footer:copyright')}
        </Typography>
        <Typography variant="caption" color="text.secondary" display="block" textAlign="center">
          {t('footer:disclaimer')}{' '}
          <Link href="mailto:info@turentaj.com" sx={{ fontSize: 'inherit', color: 'inherit' }}>
            info@turentaj.com
          </Link>
        </Typography>
      </Container>
    </Box>
  );
};

export default Footer;
