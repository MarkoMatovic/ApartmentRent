import React, { useState } from 'react';
import {
  Container,
  Typography,
  Box,
  Paper,
  Grid,
  TextField,
  Button,
  Card,
  CardContent,
  CardActions,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../shared/context/AuthContext';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { Search as SearchIcon, Home as HomeIcon, People as PeopleIcon, Message as MessageIcon } from '@mui/icons-material';

const HomePage: React.FC = () => {
  const { t } = useTranslation(['common', 'apartments']);
  const navigate = useNavigate();
  const { user, isAuthenticated } = useAuth();


  const [location, setLocation] = useState('');
  const [moveInDate, setMoveInDate] = useState<Date | null>(null);
  const [moveOutDate, setMoveOutDate] = useState<Date | null>(null);

  const handleSearch = () => {
    const params = new URLSearchParams();
    if (location) params.append('location', location);
    if (moveInDate) params.append('moveIn', moveInDate.toISOString().split('T')[0]);
    if (moveOutDate) params.append('moveOut', moveOutDate.toISOString().split('T')[0]);

    navigate(`/apartments?${params.toString()}`);
  };

  const quickActions = [
    {
      title: t('apartments:browseApartments'),
      description: t('apartments:exploreListings'),
      icon: <HomeIcon sx={{ fontSize: 40 }} />,
      path: '/apartments',
      color: '#4caf50',
    },
    {
      title: t('findRoommates'),
      description: t('roommates:connectWithRoommates', { ns: 'roommates' }),
      icon: <PeopleIcon sx={{ fontSize: 40 }} />,
      path: '/roommates',
      color: '#4caf50',
    },
    {
      title: t('messages'),
      description: t('chat:checkMessages', { ns: 'chat' }),
      icon: <MessageIcon sx={{ fontSize: 40 }} />,
      path: '/messages',
      color: '#4caf50',
    },
  ];

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Box>
        {/* Hero Search Section */}
        <Box
          sx={{
            py: { xs: 8, md: 12 },
            position: 'relative',
            color: 'white',
            backgroundImage: 'url(/hero-bg.png)',
            backgroundSize: 'cover',
            backgroundPosition: 'center',
            minHeight: { xs: '50vh', md: '70vh' },
            display: 'flex',
            alignItems: 'center',
            '&::before': {
              content: '""',
              position: 'absolute',
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              bgcolor: 'rgba(0, 0, 0, 0.45)',
              zIndex: 1,
            },
          }}
        >
          <Container maxWidth="lg" sx={{ position: 'relative', zIndex: 2, px: { xs: 2, md: 4 } }}>
            <Typography 
              variant="h3" 
              component="h1" 
              gutterBottom 
              align="center" 
              sx={{ 
                fontWeight: 800, 
                textShadow: '2px 2px 4px rgba(0,0,0,0.6)', 
                fontSize: { xs: '2rem', md: '3.5rem' } 
              }}
            >
              {t('apartments:discoverTitle')}
            </Typography>
            <Typography 
              variant="h6" 
              align="center" 
              sx={{ 
                mb: { xs: 4, md: 6 }, 
                textShadow: '1px 1px 2px rgba(0,0,0,0.5)', 
                fontSize: { xs: '1.1rem', md: '1.5rem' }, 
                fontWeight: 400 
              }}
            >
              {t('apartments:discoverSubtitle')}
            </Typography>

            <Paper
              elevation={24}
              sx={{
                p: { xs: 2, md: 3 },
                bgcolor: 'rgba(255, 255, 255, 0.95)',
                backdropFilter: 'blur(10px)',
                borderRadius: 4,
                maxWidth: 1000,
                mx: 'auto',
              }}
            >
              <Grid container spacing={2} alignItems="center">
                <Grid item xs={12} md={4}>
                  <TextField
                    fullWidth
                    placeholder={t('apartments:searchPlaceholder')}
                    value={location}
                    onChange={(e) => setLocation(e.target.value)}
                    variant="outlined"
                    sx={{
                      '& .MuiOutlinedInput-root': {
                        bgcolor: 'background.default',
                      },
                    }}
                  />
                </Grid>
                <Grid item xs={12} md={3}>
                  <DatePicker
                    label={t('apartments:moveInDate')}
                    value={moveInDate}
                    onChange={(newValue) => setMoveInDate(newValue)}
                    slotProps={{
                      textField: {
                        fullWidth: true,
                        variant: 'outlined',
                        sx: {
                          '& .MuiOutlinedInput-root': {
                            bgcolor: 'background.default',
                          },
                        },
                      },
                    }}
                  />
                </Grid>
                <Grid item xs={12} md={3}>
                  <DatePicker
                    label={t('apartments:moveOutDate')}
                    value={moveOutDate}
                    onChange={(newValue) => setMoveOutDate(newValue)}
                    slotProps={{
                      textField: {
                        fullWidth: true,
                        variant: 'outlined',
                        sx: {
                          '& .MuiOutlinedInput-root': {
                            bgcolor: 'background.default',
                          },
                        },
                      },
                    }}
                  />
                </Grid>
                <Grid item xs={12} md={2}>
                  <Button
                    fullWidth
                    variant="contained"
                    color="secondary"
                    size="large"
                    onClick={handleSearch}
                    sx={{
                      height: { xs: '48px', md: '56px' },
                      fontWeight: 600,
                      fontSize: { xs: '0.875rem', md: '1rem' },
                      minHeight: '44px', // Touch-friendly
                    }}
                  >
                    {t('apartments:quickSearch')}
                  </Button>
                </Grid>
              </Grid>
            </Paper>
          </Container>
        </Box>

        {/* Quick Actions */}
        <Container maxWidth="lg" sx={{ py: 6 }}>
          <Grid container spacing={3}>
            {quickActions.map((action, index) => (
              <Grid item xs={12} md={4} key={index}>
                <Card
                  sx={{
                    height: '100%',
                    display: 'flex',
                    flexDirection: 'column',
                    cursor: 'pointer',
                    transition: 'transform 0.2s',
                    '&:hover': {
                      transform: 'translateY(-4px)',
                      boxShadow: 4,
                    },
                  }}
                  onClick={() => navigate(action.path)}
                >
                  <CardContent sx={{ flexGrow: 1, textAlign: 'center', pt: 4 }}>
                    <Box sx={{ color: action.color, mb: 2 }}>{action.icon}</Box>
                    <Typography variant="h5" component="h3" gutterBottom>
                      {action.title}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {action.description}
                    </Typography>
                  </CardContent>
                  <CardActions sx={{ justifyContent: 'center', pb: 3 }}>
                    <Button
                      variant="contained"
                      color="secondary"
                      endIcon={<SearchIcon />}
                    >
                      {t('apartments:go')}
                    </Button>
                  </CardActions>
                </Card>
              </Grid>
            ))}
          </Grid>
        </Container>
      </Box>
    </LocalizationProvider>
  );
};

export default HomePage;

