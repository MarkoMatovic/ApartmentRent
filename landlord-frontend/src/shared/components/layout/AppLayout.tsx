import { Box, AppBar, Toolbar, Typography, Button, TextField, InputAdornment, Container, Stack, IconButton, useTheme } from '@mui/material';
import { Search as SearchIcon, LightMode as LightModeIcon, DarkMode as DarkModeIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/features/auth/store/authStore';
import { useColorMode } from '@/shared/theme/ColorModeContext';

interface AppLayoutProps {
  children: React.ReactNode;
  showSearch?: boolean;
}

export const AppLayout = ({ children, showSearch = false }: AppLayoutProps) => {
  const navigate = useNavigate();
  const theme = useTheme();
  const { isAuthenticated, logout, userGuid } = useAuthStore();
  const { mode, toggleColorMode } = useColorMode();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppBar 
        position="static" 
        elevation={0}
        sx={{ 
          bgcolor: 'primary.main',
          borderBottom: '1px solid rgba(255, 255, 255, 0.12)',
        }}
      >
        <Container maxWidth="xl">
          <Toolbar 
            disableGutters
            sx={{ 
              minHeight: { xs: 64, sm: 72 },
              py: 1,
              justifyContent: 'space-between',
              alignItems: 'center',
            }}
          >
            {/* Logo */}
            <Typography 
              variant="h5" 
              component="div" 
              sx={{ 
                fontWeight: 600,
                fontSize: '1.5rem',
                letterSpacing: '0.02em',
                cursor: 'pointer',
                color: 'white',
                mr: 4,
              }} 
              onClick={() => navigate('/')}
            >
              Landlord
            </Typography>
            
            {/* Navigation Links */}
            <Stack 
              direction="row" 
              spacing={3}
              sx={{ 
                flexGrow: 1,
                justifyContent: 'flex-start',
                alignItems: 'center',
                ml: 2,
              }}
            >
              <Button 
                color="inherit" 
                onClick={() => navigate('/')}
                sx={{ 
                  textTransform: 'uppercase', 
                  fontWeight: 500,
                  letterSpacing: '0.8px',
                  fontSize: '0.875rem',
                  px: 1.5,
                  py: 0.5,
                  '&:hover': {
                    opacity: 0.8,
                    backgroundColor: 'rgba(255, 255, 255, 0.08)',
                  },
                }}
              >
                HOME
              </Button>
              <Button 
                color="inherit" 
                onClick={() => navigate('/apartments')}
                sx={{ 
                  textTransform: 'uppercase', 
                  fontWeight: 500,
                  letterSpacing: '0.8px',
                  fontSize: '0.875rem',
                  px: 1.5,
                  py: 0.5,
                  '&:hover': {
                    opacity: 0.8,
                    backgroundColor: 'rgba(255, 255, 255, 0.08)',
                  },
                }}
              >
                APARTMENTS
              </Button>
              <Button 
                color="inherit" 
                onClick={() => navigate('/messages')}
                sx={{ 
                  textTransform: 'uppercase', 
                  fontWeight: 500,
                  letterSpacing: '0.8px',
                  fontSize: '0.875rem',
                  px: 1.5,
                  py: 0.5,
                  '&:hover': {
                    opacity: 0.8,
                    backgroundColor: 'rgba(255, 255, 255, 0.08)',
                  },
                }}
              >
                MESSAGES
              </Button>
              <Button 
                color="inherit" 
                onClick={() => navigate('/roommates')}
                sx={{ 
                  textTransform: 'uppercase', 
                  fontWeight: 500,
                  letterSpacing: '0.8px',
                  fontSize: '0.875rem',
                  px: 1.5,
                  py: 0.5,
                  '&:hover': {
                    opacity: 0.8,
                    backgroundColor: 'rgba(255, 255, 255, 0.08)',
                  },
                }}
              >
                ROOMMATES
              </Button>
            </Stack>
            
            {/* Search Input */}
            {showSearch && (
              <TextField
                placeholder="Search apartments"
                size="small"
                sx={{ 
                  mx: 3,
                  width: 280,
                  bgcolor: theme.palette.mode === 'dark' 
                    ? 'rgba(255, 255, 255, 0.1)' 
                    : 'rgba(255, 255, 255, 0.95)',
                  borderRadius: 2,
                  '& .MuiOutlinedInput-root': {
                    height: '40px',
                    color: 'white',
                    '& fieldset': {
                      border: 'none',
                    },
                    '&:hover': {
                      bgcolor: theme.palette.mode === 'dark'
                        ? 'rgba(255, 255, 255, 0.15)'
                        : 'rgba(255, 255, 255, 1)',
                    },
                  },
                }}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchIcon sx={{ color: 'rgba(255, 255, 255, 0.7)', fontSize: '1.2rem' }} />
                    </InputAdornment>
                  ),
                }}
              />
            )}
            
            {/* Theme Toggle & Auth Actions */}
            <Stack direction="row" spacing={1.5} sx={{ ml: 2 }} alignItems="center">
              <IconButton
                onClick={toggleColorMode}
                color="inherit"
                sx={{
                  '&:hover': {
                    backgroundColor: 'rgba(255, 255, 255, 0.08)',
                  },
                }}
                aria-label="toggle theme"
              >
                {mode === 'dark' ? <LightModeIcon /> : <DarkModeIcon />}
              </IconButton>
              
              {isAuthenticated ? (
                <>
                  <Typography 
                    variant="body2" 
                    sx={{ 
                      fontWeight: 500,
                      textTransform: 'uppercase',
                      color: 'white',
                      alignSelf: 'center',
                    }}
                  >
                    {userGuid ? 'MARKO MATOVIC' : 'User'}
                  </Typography>
                  <Button 
                    color="inherit" 
                    onClick={handleLogout}
                    sx={{ 
                      textTransform: 'none', 
                      fontWeight: 500,
                      px: 2,
                      '&:hover': {
                        backgroundColor: 'rgba(255, 255, 255, 0.08)',
                      },
                    }}
                  >
                    Logout
                  </Button>
                </>
              ) : (
                <>
                  <Button 
                    color="inherit" 
                    onClick={() => navigate('/login')}
                    sx={{ 
                      textTransform: 'none', 
                      fontWeight: 500,
                      px: 2,
                      '&:hover': {
                        backgroundColor: 'rgba(255, 255, 255, 0.08)',
                      },
                    }}
                  >
                    Login
                  </Button>
                  <Button 
                    color="inherit" 
                    onClick={() => navigate('/register')}
                    sx={{ 
                      textTransform: 'none', 
                      fontWeight: 600,
                      px: 2,
                      '&:hover': {
                        backgroundColor: 'rgba(255, 255, 255, 0.12)',
                      },
                    }}
                  >
                    Register
                  </Button>
                </>
              )}
            </Stack>
          </Toolbar>
        </Container>
      </AppBar>
      <Box sx={{ flex: 1 }}>
        {children}
      </Box>
    </Box>
  );
};

