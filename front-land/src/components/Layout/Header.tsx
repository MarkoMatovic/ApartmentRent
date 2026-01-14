import React, { useState } from 'react';
import {
  AppBar,
  Toolbar,
  Typography,
  Button,
  Box,
  IconButton,
  Menu,
  MenuItem,
  Badge,
  TextField,
  InputAdornment,
  useTheme,
  useMediaQuery,
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
} from '@mui/material';
import {
  Search as SearchIcon,
  Notifications as NotificationsIcon,
  DarkMode as DarkModeIcon,
  LightMode as LightModeIcon,
  Menu as MenuIcon,
  AccountCircle,
  Analytics as AnalyticsIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../shared/context/AuthContext';
import { useTheme as useThemeContext } from '../../shared/context/ThemeContext';
import { useNotifications } from '../../shared/context/NotificationContext';
import LanguageSwitcher from './LanguageSwitcher';
import NotificationPanel from '../Notification/NotificationPanel';

const Header: React.FC = () => {
  const { t } = useTranslation('common');
  const navigate = useNavigate();
  const { user, logout, isAuthenticated } = useAuth();
  const { mode, toggleTheme } = useThemeContext();
  const { unreadCount } = useNotifications();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [notificationAnchor, setNotificationAnchor] = useState<null | HTMLElement>(null);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleNotificationClick = (event: React.MouseEvent<HTMLElement>) => {
    setNotificationAnchor(event.currentTarget);
  };

  const handleNotificationClose = () => {
    setNotificationAnchor(null);
  };

  const handleLogout = async () => {
    handleMenuClose();
    await logout();
    navigate('/', { replace: true });
  };

  const handleSearch = () => {
    if (searchQuery.trim()) {
      navigate(`/apartments?search=${encodeURIComponent(searchQuery)}`);
    }
  };

  const navItems = [
    { label: t('home'), path: '/' },
    { label: t('apartments'), path: '/apartments' },
    // Prikazuj roommates i messages samo ako je korisnik ulogovan
    ...(isAuthenticated ? [
      { label: t('messages'), path: '/messages' },
      { label: t('roommates'), path: '/roommates' },
    ] : []),
  ];

  return (
    <AppBar
      position="sticky"
      sx={{
        bgcolor: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        boxShadow: '0 4px 20px rgba(0,0,0,0.1)',
        backdropFilter: 'blur(10px)',
        borderBottom: '1px solid rgba(255,255,255,0.1)'
      }}
    >
      <Toolbar>
        {/* Logo */}
        <Typography
          variant="h6"
          component="div"
          sx={{
            flexGrow: 0,
            mr: 3,
            cursor: 'pointer',
            fontWeight: 800,
            fontSize: '1.5rem',
            background: 'linear-gradient(45deg, #fff 30%, #f0f0f0 90%)',
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            textShadow: '0 2px 4px rgba(0,0,0,0.1)',
            letterSpacing: '0.5px'
          }}
          onClick={() => navigate('/')}
        >
          Landlander
        </Typography>

        {/* Desktop Navigation */}
        {!isMobile && (
          <Box sx={{ display: 'flex', gap: 1, flexGrow: 1 }}>
            {navItems.map((item) => (
              <Button
                key={item.path}
                color="inherit"
                onClick={() => navigate(item.path)}
                sx={{
                  textTransform: 'none',
                  fontWeight: 600,
                  fontSize: '0.95rem',
                  px: 2,
                  py: 1,
                  borderRadius: '20px',
                  position: 'relative',
                  overflow: 'hidden',
                  '&::before': {
                    content: '""',
                    position: 'absolute',
                    top: 0,
                    left: '-100%',
                    width: '100%',
                    height: '100%',
                    background: 'linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent)',
                    transition: 'left 0.5s',
                  },
                  '&:hover::before': {
                    left: '100%',
                  },
                  '&:hover': {
                    backgroundColor: 'rgba(255,255,255,0.1)',
                    transform: 'translateY(-1px)',
                    boxShadow: '0 4px 8px rgba(0,0,0,0.15)',
                  },
                  transition: 'all 0.3s ease',
                }}
              >
                {item.label}
              </Button>
            ))}
          </Box>
        )}

        {/* Search Bar */}
        {!isMobile && (
          <TextField
            size="small"
            placeholder="Q Search apartments"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon />
                </InputAdornment>
              ),
            }}
            sx={{
              mr: 2,
              bgcolor: 'white',
              borderRadius: 1,
              '& .MuiOutlinedInput-root': {
                '& fieldset': {
                  borderColor: 'transparent',
                },
                '&:hover fieldset': {
                  borderColor: 'transparent',
                },
                '&.Mui-focused fieldset': {
                  borderColor: 'transparent',
                },
              },
            }}
          />
        )}

        {/* Right side icons */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <LanguageSwitcher />

          <IconButton color="inherit" onClick={toggleTheme}>
            {mode === 'light' ? <DarkModeIcon /> : <LightModeIcon />}
          </IconButton>

          <IconButton color="inherit" onClick={handleNotificationClick}>
            <Badge badgeContent={unreadCount} color="error">
              <NotificationsIcon />
            </Badge>
          </IconButton>

          {isAuthenticated ? (
            <>
              <IconButton color="inherit" onClick={handleMenuOpen}>
                <AccountCircle />
                <Typography variant="body2" sx={{ ml: 1, display: { xs: 'none', sm: 'block' } }}>
                  {user?.firstName} {user?.lastName}
                </Typography>
              </IconButton>
              <Menu
                anchorEl={anchorEl}
                open={Boolean(anchorEl)}
                onClose={handleMenuClose}
              >
                <MenuItem onClick={() => { navigate('/profile'); handleMenuClose(); }}>
                  {t('profile')}
                </MenuItem>
                <MenuItem onClick={() => { navigate('/my-apartments'); handleMenuClose(); }}>
                  {t('myApartments')}
                </MenuItem>
                {user?.userRoleId === 1 && (
                  <MenuItem onClick={() => { navigate('/admin/analytics'); handleMenuClose(); }}>
                    <AnalyticsIcon sx={{ mr: 1, fontSize: '1.2rem' }} />
                    Analytics Dashboard
                  </MenuItem>
                )}
                <MenuItem onClick={handleLogout}>{t('logout')}</MenuItem>
              </Menu>
            </>
          ) : (
            <Box sx={{ display: 'flex', gap: 1 }}>
              <Button color="inherit" onClick={() => navigate('/login')}>
                {t('login')}
              </Button>
              <Button
                color="inherit"
                variant="outlined"
                onClick={() => navigate('/register')}
                sx={{ borderColor: 'white', color: 'white' }}
              >
                {t('register')}
              </Button>
            </Box>
          )}

          {isMobile && (
            <IconButton color="inherit" onClick={() => setMobileOpen(true)}>
              <MenuIcon />
            </IconButton>
          )}
        </Box>
      </Toolbar>

      {/* Mobile Drawer */}
      <Drawer anchor="right" open={mobileOpen} onClose={() => setMobileOpen(false)}>
        <Box sx={{ width: 250, pt: 2 }}>
          <List>
            {navItems.map((item) => (
              <ListItem key={item.path} disablePadding>
                <ListItemButton onClick={() => { navigate(item.path); setMobileOpen(false); }}>
                  <ListItemText primary={item.label} />
                </ListItemButton>
              </ListItem>
            ))}
          </List>
        </Box>
      </Drawer>

      {/* Notification Panel */}
      <NotificationPanel
        anchorEl={notificationAnchor}
        open={Boolean(notificationAnchor)}
        onClose={handleNotificationClose}
      />
    </AppBar>
  );
};

export default Header;

