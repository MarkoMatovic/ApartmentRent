import React, { useEffect, useRef, useState } from 'react';
import { Container, Paper, Typography, Box, Link, Alert, CircularProgress } from '@mui/material';
import { useSearchParams, Link as RouterLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { authApi } from '../shared/api/auth';

type Status = 'loading' | 'success' | 'error';

const VerifyEmailPage: React.FC = () => {
  const { t } = useTranslation('auth');
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const [status, setStatus] = useState<Status>(token ? 'loading' : 'error');
  // The token is single-use, so guard against the effect running twice (React StrictMode in dev).
  const requested = useRef(false);

  useEffect(() => {
    if (!token || requested.current) return;
    requested.current = true;
    authApi
      .verifyEmail(token)
      .then(() => setStatus('success'))
      .catch(() => setStatus('error'));
  }, [token]);

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={3} sx={{ p: 4, textAlign: 'center' }}>
        {status === 'loading' && (
          <>
            <CircularProgress sx={{ mb: 2 }} />
            <Typography>{t('verifyingEmail')}</Typography>
          </>
        )}
        {status === 'success' && (
          <Alert severity="success" sx={{ mb: 2 }}>
            {t('emailVerified')}
          </Alert>
        )}
        {status === 'error' && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {t('emailVerificationFailed')}
          </Alert>
        )}
        {status !== 'loading' && (
          <Box sx={{ mt: 2 }}>
            <Link component={RouterLink} to="/login">
              {t('login')}
            </Link>
          </Box>
        )}
      </Paper>
    </Container>
  );
};

export default VerifyEmailPage;
