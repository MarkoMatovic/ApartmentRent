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
import { UserRegistrationInputDto } from '../types';
import { AppLayout } from '@/shared/components/layout/AppLayout';

const RegisterPage = () => {
  const navigate = useNavigate();
  const { register, isLoading, error } = useAuthStore();
  const [formData, setFormData] = useState<UserRegistrationInputDto>({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    dateOfBirth: '',
    phoneNumber: '',
    profilePicture: '',
    createdDate: new Date().toISOString(),
  });
  const [localError, setLocalError] = useState<string | null>(null);

  const handleChange = (field: keyof UserRegistrationInputDto) => (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    setFormData({ ...formData, [field]: e.target.value });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLocalError(null);

    if (!formData.firstName || !formData.lastName || !formData.email || !formData.password) {
      setLocalError('Please fill in all required fields');
      return;
    }

    try {
      await register(formData);
      navigate('/login');
    } catch (err: any) {
      setLocalError(err.message || 'Registration failed');
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
                Register
              </Typography>
              <Typography
                variant="body2"
                sx={{
                  color: 'text.secondary',
                  fontWeight: 400,
                }}
              >
                Create your account to get started
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
                  label="First Name"
                  value={formData.firstName}
                  onChange={handleChange('firstName')}
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
                  label="Last Name"
                  value={formData.lastName}
                  onChange={handleChange('lastName')}
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
                  label="Email"
                  type="email"
                  value={formData.email}
                  onChange={handleChange('email')}
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
                  value={formData.password}
                  onChange={handleChange('password')}
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
                  label="Date of Birth"
                  type="date"
                  value={formData.dateOfBirth}
                  onChange={handleChange('dateOfBirth')}
                  InputLabelProps={{ shrink: true }}
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
                  label="Phone Number"
                  value={formData.phoneNumber}
                  onChange={handleChange('phoneNumber')}
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
                  label="Profile Picture URL (optional)"
                  value={formData.profilePicture || ''}
                  onChange={handleChange('profilePicture')}
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
                  {isLoading ? 'Registering...' : 'Register'}
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
                Already have an account?{' '}
                <Link
                  component="button"
                  onClick={() => navigate('/login')}
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
                  Login
                </Link>
              </Typography>
            </Box>
          </Stack>
        </Paper>
      </Box>
    </AppLayout>
  );
};

export default RegisterPage;

