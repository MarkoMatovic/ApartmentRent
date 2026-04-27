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
import { useAuth } from '../shared/context/AuthContext';

const RegisterPage: React.FC = () => {
  const { t } = useTranslation('auth');
  const navigate = useNavigate();
  const { register } = useAuth();
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: '',
    phoneNumber: '',
    dateOfBirth: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [registeredEmail, setRegisteredEmail] = useState<string | null>(null);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!formData.firstName.trim() || !formData.lastName.trim()) {
      setError(t('nameRequired'));
      return;
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      setError(t('invalidEmail'));
      return;
    }

    if (formData.phoneNumber && !/^\+?[\d\s\-()]{6,20}$/.test(formData.phoneNumber)) {
      setError(t('invalidPhone'));
      return;
    }

    if (formData.dateOfBirth && new Date(formData.dateOfBirth) > new Date()) {
      setError(t('dobInFuture', { defaultValue: 'Date of birth cannot be in the future.' }));
      return;
    }

    if (formData.password.length < 8) {
      setError(t('passwordRequirements'));
      return;
    }

    if (formData.password !== formData.confirmPassword) {
      setError(t('passwordsDoNotMatch'));
      return;
    }

    setLoading(true);

    try {
      await register({
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        password: formData.password,
        phoneNumber: formData.phoneNumber || undefined,
        dateOfBirth: formData.dateOfBirth || undefined,
      });
      setRegisteredEmail(formData.email);
    } catch (err: any) {
      if (err.code === 'ERR_NETWORK' || err.message?.includes('Failed to fetch') || err.message?.includes('CONNECTION_REFUSED')) {
        setError('Cannot connect to server. Please make sure the backend is running on https://localhost:5002');
      } else {
        const d = err.response?.data;
        setError(typeof d === 'string' ? d : d?.message || d?.title || err.message || 'Registration failed');
      }
    } finally {
      setLoading(false);
    }
  };

  if (registeredEmail) {
    return (
      <Container maxWidth="sm" sx={{ py: 8 }}>
        <Paper elevation={3} sx={{ p: 4, textAlign: 'center' }}>
          <Typography variant="h5" gutterBottom>
            {t('checkYourEmail', { defaultValue: 'Check your email' })}
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mt: 2, mb: 3 }}>
            {t('verificationSent', {
              defaultValue: 'We sent a verification link to {{email}}. Click the link to activate your account, then log in.',
              email: registeredEmail,
            })}
          </Typography>
          <Button variant="contained" onClick={() => navigate('/login')}>
            {t('goToLogin', { defaultValue: 'Go to Login' })}
          </Button>
        </Paper>
      </Container>
    );
  }

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={3} sx={{ p: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom align="center">
          {t('register')}
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        <Box component="form" onSubmit={handleSubmit} sx={{ mt: 3 }}>
          <TextField
            fullWidth
            label={t('firstName')}
            name="firstName"
            value={formData.firstName}
            onChange={handleChange}
            required
            margin="normal"
          />
          <TextField
            fullWidth
            label={t('lastName')}
            name="lastName"
            value={formData.lastName}
            onChange={handleChange}
            required
            margin="normal"
          />
          <TextField
            fullWidth
            label={t('email')}
            name="email"
            type="email"
            value={formData.email}
            onChange={handleChange}
            required
            margin="normal"
          />
          <TextField
            fullWidth
            label={t('phoneNumber')}
            name="phoneNumber"
            value={formData.phoneNumber}
            onChange={handleChange}
            margin="normal"
          />
          <TextField
            fullWidth
            label={t('dateOfBirth')}
            name="dateOfBirth"
            type="date"
            value={formData.dateOfBirth}
            onChange={handleChange}
            margin="normal"
            InputLabelProps={{ shrink: true }}
          />
          <TextField
            fullWidth
            label={t('password')}
            name="password"
            type="password"
            value={formData.password}
            onChange={handleChange}
            required
            margin="normal"
            helperText={t('passwordRequirements')}
          />
          <TextField
            fullWidth
            label={t('confirmPassword')}
            name="confirmPassword"
            type="password"
            value={formData.confirmPassword}
            onChange={handleChange}
            required
            margin="normal"
          />
          <Button
            type="submit"
            fullWidth
            variant="contained"
            color="secondary"
            size="large"
            disabled={loading}
            sx={{ mt: 3 }}
          >
            {t('registerButton')}
          </Button>
          <Box sx={{ mt: 2, textAlign: 'center' }}>
            <Typography variant="body2">
              {t('alreadyHaveAccount')}{' '}
              <Link component={RouterLink} to="/login">
                {t('login')}
              </Link>
            </Typography>
          </Box>
        </Box>
      </Paper>
    </Container>
  );
};

export default RegisterPage;

