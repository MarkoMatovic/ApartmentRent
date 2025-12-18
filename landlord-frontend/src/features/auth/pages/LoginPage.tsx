import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTheme } from '@mui/material/styles';
import {
  Box,
  Paper,
  TextField,
  Button,
  Typography,
  Link,
  Alert,
  Stack,
} from '@mui/material';
import { useAuthStore } from '../store/authStore';
import { AppLayout } from '@/shared/components/layout/AppLayout';

const LoginPage = () => {
  const navigate = useNavigate();
  const { login, isLoading, error } = useAuthStore();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLocalError(null);

    if (!email || !password) {
      setLocalError('Please fill in all fields');
      return;
    }

    try {
      await login(email, password);
      navigate('/');
    } catch (err: any) {
      setLocalError(err.message || 'Login failed');
    }
  };

  const theme = useTheme();

  return (
    <AppLayout>
      <Box
        sx={{
          minHeight: 'calc(100vh - 64px)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          background: theme.palette.mode === 'dark'
            ? 'linear-gradient(to bottom, #1a1a1a, #121212)'
            : 'linear-gradient(to bottom, #f5f7fa, #ffffff)',
          py: 4,
          px: 2,
        }}
      >
        <Paper
          elevation={0}
          sx={{
            p: 5,
            width: '100%',
            maxWidth: 440,
            borderRadius: 3,
            boxShadow: '0 2px 8px rgba(0, 0, 0, 0.08)',
          }}
        >
          <Stack spacing={3}>
            <Box sx={{ textAlign: 'center', mb: 1 }}>
              <Typography
                variant="h4"
                component="h1"
                sx={{
                  fontWeight: 700,
                  letterSpacing: '-0.02em',
                  mb: 1,
                  color: 'text.primary',
                }}
              >
                Login
              </Typography>
              <Typography
                variant="body2"
                sx={{
                  color: 'text.secondary',
                  fontWeight: 400,
                }}
              >
                Sign in to your account to continue
              </Typography>
            </Box>

            {(error || localError) && (
              <Alert severity="error" sx={{ borderRadius: 2 }}>
                {localError || error}
              </Alert>
            )}

            <Box component="form" onSubmit={handleSubmit}>
              <Stack spacing={2.5}>
                <TextField
                  fullWidth
                  label="Email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  disabled={isLoading}
                  sx={{
                    '& .MuiOutlinedInput-root': {
                      height: '56px',
                    },
                  }}
                />
                <TextField
                  fullWidth
                  label="Password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  disabled={isLoading}
                  sx={{
                    '& .MuiOutlinedInput-root': {
                      height: '56px',
                    },
                  }}
                />
                <Button
                  type="submit"
                  fullWidth
                  variant="contained"
                  disabled={isLoading}
                  sx={{
                    mt: 1,
                    py: 1.5,
                    height: '48px',
                    bgcolor: 'primary.main',
                    fontWeight: 600,
                    textTransform: 'none',
                    fontSize: '1rem',
                    '&:hover': {
                      bgcolor: 'primary.dark',
                    },
                  }}
                >
                  {isLoading ? 'Logging in...' : 'Login'}
                </Button>
              </Stack>
            </Box>

            <Box sx={{ textAlign: 'center', mt: 1 }}>
              <Typography
                variant="body2"
                sx={{
                  color: 'text.secondary',
                  fontSize: '0.875rem',
                }}
              >
                Don't have an account?{' '}
                <Link
                  component="button"
                  onClick={() => navigate('/register')}
                  sx={{
                    cursor: 'pointer',
                    color: 'text.secondary',
                    textDecoration: 'underline',
                    textUnderlineOffset: 2,
                    '&:hover': {
                      color: 'primary.main',
                    },
                  }}
                >
                  Register
                </Link>
              </Typography>
            </Box>
          </Stack>
        </Paper>
      </Box>
    </AppLayout>
  );
};

export default LoginPage;

