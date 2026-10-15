import React, { useState } from 'react';
import {
  Container,
  Paper,
  TextField,
  Button,
  Typography,
  Box,
  Link,
  Alert,
} from '@mui/material';
import { useNavigate, Link as RouterLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { getApiErrorMessage, isApiErrorCode } from '../shared/utils/apiError';
import { useAuth } from '../shared/context/AuthContext';

const LoginPage: React.FC = () => {
  const { t } = useTranslation('auth');
  const navigate = useNavigate();
  const { login, completeTwoFactorLogin } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  // Set once the password was right and the account wants the code from the authenticator app.
  const [twoFactorToken, setTwoFactorToken] = useState<string | null>(null);
  const [code, setCode] = useState('');

  const handleCodeSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!twoFactorToken) return;
    setError('');
    setLoading(true);
    try {
      await completeTwoFactorLogin(twoFactorToken, code.trim());
      navigate('/');
    } catch (err: unknown) {
      const message = getApiErrorMessage(err, t);
      if (isApiErrorCode(err, 'TWO_FACTOR_EXPIRED')) {
        // Too late for this code: back to the password, with the reason.
        setTwoFactorToken(null);
        setPassword('');
      }
      setCode('');
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  const backToPassword = () => {
    setTwoFactorToken(null);
    setCode('');
    setError('');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      setError(t('invalidEmail'));
      return;
    }
    if (password.length < 1) {
      setError(t('passwordRequired'));
      return;
    }

    setLoading(true);

    try {
      const outcome = await login(email, password);
      if (outcome.twoFactorToken) {
        setTwoFactorToken(outcome.twoFactorToken);
        return;
      }
      navigate('/');
    } catch (err: unknown) {
      setError(getApiErrorMessage(err, t, {
        401: t('invalidCredentials'),
        403: t('emailNotVerified'),
      }));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={3} sx={{ p: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom align="center">
          {t('login')}
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {twoFactorToken ? (
          <Box component="form" onSubmit={handleCodeSubmit} sx={{ mt: 3 }}>
            <Typography variant="h6" gutterBottom>{t('twoFactorStepTitle')}</Typography>
            <Typography variant="body2" color="text.secondary">{t('twoFactorStepHint')}</Typography>
            <TextField
              fullWidth
              autoFocus
              label={t('twoFactorCode')}
              value={code}
              onChange={(e) => setCode(e.target.value)}
              required
              margin="normal"
              autoComplete="one-time-code"
              inputProps={{ inputMode: 'text', maxLength: 20, 'aria-describedby': 'two-factor-recovery-hint' }}
            />
            <Typography id="two-factor-recovery-hint" variant="caption" color="text.secondary">
              {t('twoFactorRecoveryHint')}
            </Typography>
            <Button
              type="submit"
              fullWidth
              variant="contained"
              color="secondary"
              size="large"
              disabled={loading || code.trim().length === 0}
              sx={{ mt: 3 }}
            >
              {t('twoFactorVerify')}
            </Button>
            <Box sx={{ mt: 2, textAlign: 'center' }}>
              <Link component="button" type="button" variant="body2" onClick={backToPassword}>
                {t('backToLogin')}
              </Link>
            </Box>
          </Box>
        ) : (
        <Box component="form" onSubmit={handleSubmit} sx={{ mt: 3 }}>
          <TextField
            fullWidth
            label={t('email')}
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            margin="normal"
            autoComplete="email"
          />
          <TextField
            fullWidth
            label={t('password')}
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            margin="normal"
            autoComplete="current-password"
          />
          <Box sx={{ mt: 2, mb: 2 }}>
            <Link component={RouterLink} to="/forgot-password" variant="body2">
              {t('forgotPassword')}
            </Link>
          </Box>
          <Button
            type="submit"
            fullWidth
            variant="contained"
            color="secondary"
            size="large"
            disabled={loading}
            sx={{ mt: 2 }}
          >
            {t('loginButton')}
          </Button>
          <Box sx={{ mt: 2, textAlign: 'center' }}>
            <Typography variant="body2">
              {t('dontHaveAccount')}{' '}
              <Link component={RouterLink} to="/register">
                {t('createAccount')}
              </Link>
            </Typography>
          </Box>
        </Box>
        )}
      </Paper>
    </Container>
  );
};

export default LoginPage;

