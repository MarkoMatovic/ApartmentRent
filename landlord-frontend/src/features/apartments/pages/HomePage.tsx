import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTheme } from '@mui/material/styles';
import {
  Box,
  Container,
  Typography,
  TextField,
  Button,
  Card,
  CardContent,
  Grid,
  Stack,
} from '@mui/material';
import { AppLayout } from '@/shared/components/layout/AppLayout';
import { useAuthStore } from '@/features/auth/store/authStore';

const HomePage = () => {
  const navigate = useNavigate();
  const theme = useTheme();
  const { isAuthenticated, userGuid } = useAuthStore();
  const [location, setLocation] = useState('');
  const [moveInDate, setMoveInDate] = useState('');
  const [moveOutDate, setMoveOutDate] = useState('');

  const handleSearch = () => {
    // Navigate to apartments with search params
    // TEMPORARY: Auth disabled for development - bypassing auth check
    // if (isAuthenticated) {
      navigate('/apartments');
    // } else {
    //   navigate('/login');
    // }
  };

  const handleQuickAction = (path: string) => {
    // TEMPORARY: Auth disabled for development - bypassing auth check
    // if (isAuthenticated) {
      navigate(path);
    // } else {
    //   navigate('/login');
    // }
  };

  return (
    <AppLayout showSearch>
      <Box sx={{ bgcolor: 'background.default', minHeight: '100vh' }}>
        {/* Hero Section with Gradient */}
        <Box
          sx={{
            background: theme.palette.mode === 'dark'
              ? 'linear-gradient(135deg, #2d5a4a 0%, #3d6b5a 100%)'
              : 'linear-gradient(135deg, #7FD3C1 0%, #A8D5BA 100%)',
            position: 'relative',
            py: 9,
            mt: 2,
            mb: 2,
            '&::before': {
              content: '""',
              position: 'absolute',
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              background: theme.palette.mode === 'dark'
                ? 'linear-gradient(to bottom, rgba(255, 255, 255, 0.03), transparent)'
                : 'linear-gradient(to bottom, rgba(255, 255, 255, 0.05), transparent)',
              pointerEvents: 'none',
            },
          }}
        >
          <Container maxWidth="lg" sx={{ position: 'relative', zIndex: 1 }}>
            <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: '100%' }}>
              <Typography
                variant="h2"
                component="h2"
                align="center"
                sx={{ 
                  fontWeight: 700, 
                  mb: 2, 
                  color: theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, 0.95)' : 'rgba(0, 0, 0, 0.9)',
                  letterSpacing: '-0.02em',
                  fontSize: { xs: '2rem', md: '2.75rem' }
                }}
              >
                Discover Your Perfect Space
              </Typography>
              <Typography 
                variant="h6" 
                align="center" 
                sx={{ 
                  mb: 5, 
                  color: theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, 0.8)' : 'rgba(0, 0, 0, 0.75)',
                  fontWeight: 300,
                  lineHeight: 1.6,
                  maxWidth: 600
                }}
              >
                Find apartments and roommates that match your lifestyle
              </Typography>

              {/* Search Form - Inline Layout */}
              <Box
                sx={{
                  width: '100%',
                  maxWidth: 1000,
                  bgcolor: 'background.paper',
                  p: 3,
                  borderRadius: 3,
                  boxShadow: theme.palette.mode === 'dark'
                    ? '0 4px 16px rgba(0, 0, 0, 0.3)'
                    : '0 4px 16px rgba(0, 0, 0, 0.12)',
                }}
              >
                <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} alignItems="stretch">
                  <TextField
                    label="Where are you looking?"
                    value={location}
                    onChange={(e) => setLocation(e.target.value)}
                    variant="outlined"
                    sx={{ 
                      flex: 1,
                      '& .MuiOutlinedInput-root': {
                        height: '60px',
                      },
                    }}
                  />
                  <TextField
                    label="Move-in Date (dd-yyyy)"
                    value={moveInDate}
                    onChange={(e) => setMoveInDate(e.target.value)}
                    variant="outlined"
                    placeholder="dd-yyyy"
                    sx={{ 
                      flex: 1,
                      '& .MuiOutlinedInput-root': {
                        height: '60px',
                      },
                    }}
                  />
                  <TextField
                    label="Move-out Date (Optional)"
                    value={moveOutDate}
                    onChange={(e) => setMoveOutDate(e.target.value)}
                    variant="outlined"
                    placeholder="dd-yyyy"
                    sx={{ 
                      flex: 1,
                      '& .MuiOutlinedInput-root': {
                        height: '60px',
                      },
                    }}
                  />
                  <Button
                    variant="contained"
                    onClick={handleSearch}
                    sx={{
                      bgcolor: 'success.main',
                      color: 'white',
                      px: 4,
                      minWidth: 140,
                      height: '60px',
                      fontWeight: 600,
                      textTransform: 'none',
                      fontSize: '1rem',
                      boxShadow: '0 2px 8px rgba(76, 175, 80, 0.3)',
                      '&:hover': {
                        bgcolor: 'success.dark',
                        boxShadow: '0 4px 12px rgba(76, 175, 80, 0.4)',
                      },
                    }}
                  >
                    SEARCH
                  </Button>
                </Stack>
              </Box>
            </Box>
          </Container>
        </Box>

        {/* Quick Actions */}
        <Container maxWidth="xl" sx={{ pb: 6, pt: 1 }}>
          <Typography variant="h5" component="h3" gutterBottom sx={{ mb: 4, textAlign: 'center', fontWeight: 600 }}>
            Quick Actions
          </Typography>
          <Grid container spacing={4} justifyContent="center">
            <Grid item xs={12} sm={10} md={3.8}>
              <Card sx={{ boxShadow: 2 }}>
                <CardContent sx={{ py: 1.5, px: 3 }}>
                  <Typography variant="h6" gutterBottom sx={{ mb: 0.5, fontWeight: 600 }}>
                    Browse Apartments
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2, lineHeight: 1.5 }}>
                    Explore all available listings
                  </Typography>
                  <Button
                    variant="contained"
                    onClick={() => handleQuickAction('/apartments')}
                    sx={{
                      bgcolor: 'success.main',
                      color: 'white',
                      textTransform: 'none',
                      fontWeight: 500,
                      '&:hover': {
                        bgcolor: 'success.dark',
                      },
                    }}
                  >
                    GO
                  </Button>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={10} md={3.8}>
              <Card sx={{ boxShadow: 2 }}>
                <CardContent sx={{ py: 1.5, px: 3 }}>
                  <Typography variant="h6" gutterBottom sx={{ mb: 0.5, fontWeight: 600 }}>
                    Find Roommates
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2, lineHeight: 1.5 }}>
                    Connect with potential roommates
                  </Typography>
                  <Button
                    variant="contained"
                    onClick={() => handleQuickAction('/roommates')}
                    sx={{
                      bgcolor: 'success.main',
                      color: 'white',
                      textTransform: 'none',
                      fontWeight: 500,
                      '&:hover': {
                        bgcolor: 'success.dark',
                      },
                    }}
                  >
                    GO
                  </Button>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={10} md={3.8}>
              <Card sx={{ boxShadow: 2 }}>
                <CardContent sx={{ py: 1.5, px: 3 }}>
                  <Typography variant="h6" gutterBottom sx={{ mb: 0.5, fontWeight: 600 }}>
                    Messages
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2, lineHeight: 1.5 }}>
                    Check your messages
                  </Typography>
                  <Button
                    variant="contained"
                    onClick={() => handleQuickAction('/messages')}
                    sx={{
                      bgcolor: 'success.main',
                      color: 'white',
                      textTransform: 'none',
                      fontWeight: 500,
                      '&:hover': {
                        bgcolor: 'success.dark',
                      },
                    }}
                  >
                    GO
                  </Button>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </Container>
      </Box>
    </AppLayout>
  );
};

export default HomePage;

