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
  Divider,
} from '@mui/material';
import {
  Search as SearchIcon,
  Notifications as NotificationsIcon,
  DarkMode as DarkModeIcon,
  LightMode as LightModeIcon,
  Menu as MenuIcon,
  AccountCircle,
  Analytics as AnalyticsIcon,
  Star as StarIcon,
  Report as ReportIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../shared/context/AuthContext';
import { useTheme as useThemeContext } from '../../shared/context/ThemeContext';
import { useNotifications } from '../../shared/context/NotificationContext';
import LanguageSwitcher from './LanguageSwitcher';
import NotificationPanel from '../Notification/NotificationPanel';

const Header: React.FC = () => {
  const { t } = useTranslation(['common', 'dashboard', 'premium', 'savedSearches', 'searchRequests', 'machineLearning', 'chat']);
  const navigate = useNavigate();
  const { user, logout, isAuthenticated } = useAuth();
  const { mode, toggleTheme } = useThemeContext();
  const { unreadCount, unreadMessagesCount } = useNotifications();
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
      <Toolbar sx={{ minHeight: { xs: 56, sm: 64 } }}>
        {/* Logo */}
        <Typography
          variant="h6"
          component="div"
          sx={{
            flexGrow: 0,
            mr: { xs: 'auto', md: 3 },
            cursor: 'pointer',
            fontWeight: 800,
            fontSize: { xs: '1.25rem', md: '1.5rem' },
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
            {navItems.map((item) => {
              const showBadge = item.path === '/messages' && unreadMessagesCount > 0;
              return (
                <Box
                  key={item.path}
                  sx={{
                    position: 'relative',
                    display: 'inline-block',
                  }}
                >
                  <Button
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
                  {showBadge && (
                    <Badge
                      badgeContent={unreadMessagesCount}
                      color="error"
                      sx={{
                        position: 'absolute',
                        top: -4,
                        right: -4,
                        pointerEvents: 'none',
                        '& .MuiBadge-badge': {
                          fontSize: '0.7rem',
                          minWidth: '18px',
                          height: '18px',
                          padding: '0 4px',
                        },
                      }}
                    />
                  )}
                </Box>
              );
            })}
          </Box>
        )}

        {/* Search Bar - Desktop only */}
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
        <Box sx={{ display: 'flex', alignItems: 'center', gap: { xs: 0.5, sm: 1 } }}>
          <LanguageSwitcher />

          <IconButton color="inherit" onClick={toggleTheme} size={isMobile ? 'small' : 'medium'}>
            {mode === 'light' ? <DarkModeIcon /> : <LightModeIcon />}
          </IconButton>

          <IconButton color="inherit" onClick={handleNotificationClick} size={isMobile ? 'small' : 'medium'}>
            <Badge badgeContent={unreadCount} color="error">
              <NotificationsIcon />
            </Badge>
          </IconButton>

          {isAuthenticated ? (
            <>
              {!isMobile && (
                <IconButton color="inherit" onClick={handleMenuOpen}>
                  <AccountCircle />
                  <Typography variant="body2" sx={{ ml: 1, display: { xs: 'none', sm: 'block' } }}>
                    {user?.firstName} {user?.lastName}
                  </Typography>
                </IconButton>
              )}
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
                <MenuItem onClick={() => { navigate('/appointments/my'); handleMenuClose(); }}>
                  {t('myAppointments')}
                </MenuItem>
                <MenuItem onClick={() => { navigate('/appointments/manage'); handleMenuClose(); }}>
                  {t('manageAppointments')}
                </MenuItem>
                <MenuItem onClick={() => { navigate('/appointments/availability'); handleMenuClose(); }}>
                  My Availability
                </MenuItem>
                <MenuItem onClick={() => { navigate('/applications/sent'); handleMenuClose(); }}>
                  My Applications
                </MenuItem>
                <MenuItem onClick={() => { navigate('/applications/received'); handleMenuClose(); }}>
                  Received Applications
                </MenuItem>
                <MenuItem onClick={() => { navigate('/analytics/roommate'); handleMenuClose(); }}>
                  <AnalyticsIcon sx={{ mr: 1, fontSize: '1.2rem' }} />
                  {t('common:personalAnalytics')}
                </MenuItem>
                <MenuItem onClick={() => { navigate('/saved-searches'); handleMenuClose(); }}>
                  {t('savedSearches:title')}
                </MenuItem>
                <MenuItem onClick={() => { navigate('/search-requests'); handleMenuClose(); }}>
                  {t('searchRequests:title')}
                </MenuItem>
                <MenuItem onClick={() => { navigate('/price-predictor'); handleMenuClose(); }}>
                  {t('machineLearning:pricePredictor.title')}
                </MenuItem>
                {!user?.roleName?.includes('Premium') && (
                  <MenuItem
                    onClick={() => { navigate('/pricing'); handleMenuClose(); }}
                    sx={{ color: 'warning.main', fontWeight: 'bold' }}
                  >
                    <StarIcon sx={{ mr: 1, fontSize: '1.2rem' }} />
                    {t('premium:upgradeToPremium')}
                  </MenuItem>
                )}
                {user?.userRoleId === 1 && (
                  <>
                    <MenuItem onClick={() => { navigate('/admin/analytics'); handleMenuClose(); }}>
                      <AnalyticsIcon sx={{ mr: 1, fontSize: '1.2rem' }} />
                      {t('dashboard:analyticsDashboard')}
                    </MenuItem>
                    <MenuItem onClick={() => { navigate('/admin/reports'); handleMenuClose(); }}>
                      <ReportIcon sx={{ mr: 1, fontSize: '1.2rem' }} />
                      {t('chat:abuseReports')}
                    </MenuItem>
                  </>
                )}
                <MenuItem onClick={handleLogout}>{t('logout')}</MenuItem>
              </Menu>
            </>
          ) : (
            <Box sx={{ display: { xs: 'none', md: 'flex' }, gap: 1 }}>
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

          {/* Hamburger Menu - Mobile only */}
          {isMobile && (
            <IconButton color="inherit" onClick={() => setMobileOpen(true)} edge="end">
              <MenuIcon />
            </IconButton>
          )}
        </Box>
      </Toolbar>

      {/* Mobile Drawer */}
      <Drawer anchor="right" open={mobileOpen} onClose={() => setMobileOpen(false)}>
        <Box sx={{ width: 280, pt: 2 }}>
          {/* User Info or Login/Register */}
          {isAuthenticated ? (
            <Box sx={{ px: 2, pb: 2, borderBottom: '1px solid', borderColor: 'divider' }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                <AccountCircle sx={{ fontSize: 40 }} />
                <Box>
                  <Typography variant="subtitle1" fontWeight={600}>
                    {user?.firstName} {user?.lastName}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {user?.email}
                  </Typography>
                </Box>
              </Box>
            </Box>
          ) : (
            <Box sx={{ px: 2, pb: 2, borderBottom: '1px solid', borderColor: 'divider', display: 'flex', flexDirection: 'column', gap: 1 }}>
              <Button
                fullWidth
                variant="contained"
                onClick={() => { navigate('/login'); setMobileOpen(false); }}
              >
                {t('login')}
              </Button>
              <Button
                fullWidth
                variant="outlined"
                onClick={() => { navigate('/register'); setMobileOpen(false); }}
              >
                {t('register')}
              </Button>
            </Box>
          )}

          {/* Navigation Items */}
          <List>
            {navItems.map((item) => {
              const showBadge = item.path === '/messages' && unreadMessagesCount > 0;
              return (
                <ListItem key={item.path} disablePadding>
                  <ListItemButton onClick={() => { navigate(item.path); setMobileOpen(false); }}>
                    <ListItemText primary={item.label} />
                    {showBadge && (
                      <Badge badgeContent={unreadMessagesCount} color="error" sx={{ ml: 2 }} />
                    )}
                  </ListItemButton>
                </ListItem>
              );
            })}
          </List>

          {/* User Menu Items (if authenticated) */}
          {isAuthenticated && (
            <>
              <Divider />
              <List>
                <ListItem disablePadding>
                  <ListItemButton onClick={() => { navigate('/profile'); setMobileOpen(false); }}>
                    <ListItemText primary={t('profile')} />
                  </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                  <ListItemButton onClick={() => { navigate('/my-apartments'); setMobileOpen(false); }}>
                    <ListItemText primary={t('myApartments')} />
                  </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                  <ListItemButton onClick={() => { navigate('/appointments/my'); setMobileOpen(false); }}>
                    <ListItemText primary={t('myAppointments')} />
                  </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                  <ListItemButton onClick={() => { navigate('/appointments/manage'); setMobileOpen(false); }}>
                    <ListItemText primary={t('manageAppointments')} />
                  </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                  <ListItemButton onClick={() => { navigate('/appointments/availability'); setMobileOpen(false); }}>
                    <ListItemText primary="My Availability" />
                  </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                  <ListItemButton onClick={() => { navigate('/applications/sent'); setMobileOpen(false); }}>
                    <ListItemText primary="My Applications" />
                  </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                  <ListItemButton onClick={() => { navigate('/applications/received'); setMobileOpen(false); }}>
                    <ListItemText primary="Received Applications" />
                  </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                  <ListItemButton onClick={() => { navigate('/analytics/roommate'); setMobileOpen(false); }}>
                    <Box sx={{ display: 'flex', alignItems: 'center' }}>
                      <AnalyticsIcon sx={{ mr: 1, fontSize: '1.2rem' }} />
                      <ListItemText primary={t('common:personalAnalytics')} />
                    </Box>
                  </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                  <ListItemButton onClick={() => { navigate('/saved-searches'); setMobileOpen(false); }}>
                    <ListItemText primary={t('savedSearches:title')} />
                  </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                  <ListItemButton onClick={() => { navigate('/search-requests'); setMobileOpen(false); }}>
                    <ListItemText primary={t('searchRequests:title')} />
                  </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                  <ListItemButton onClick={() => { navigate('/price-predictor'); setMobileOpen(false); }}>
                    <ListItemText primary={t('machineLearning:pricePredictor.title')} />
                  </ListItemButton>
                </ListItem>
                {!user?.roleName?.includes('Premium') && (
                  <ListItem disablePadding>
                    <ListItemButton
                      onClick={() => { navigate('/pricing'); setMobileOpen(false); }}
                      sx={{ bgcolor: 'warning.light' }}
                    >
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <StarIcon sx={{ mr: 1, fontSize: '1.2rem', color: 'warning.main' }} />
                        <ListItemText
                          primary={t('premium:upgradeToPremium')}
                          sx={{ color: 'warning.main', fontWeight: 'bold' }}
                        />
                      </Box>
                    </ListItemButton>
                  </ListItem>
                )}
                {user?.userRoleId === 1 && (
                  <>
                    <ListItem disablePadding>
                      <ListItemButton onClick={() => { navigate('/admin/analytics'); setMobileOpen(false); }}>
                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                          <AnalyticsIcon sx={{ mr: 1, fontSize: '1.2rem' }} />
                          <ListItemText primary={t('dashboard:analyticsDashboard')} />
                        </Box>
                      </ListItemButton>
                    </ListItem>
                    <ListItem disablePadding>
                      <ListItemButton onClick={() => { navigate('/admin/reports'); setMobileOpen(false); }}>
                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                          <ReportIcon sx={{ mr: 1, fontSize: '1.2rem' }} />
                          <ListItemText primary={t('chat:abuseReports')} />
                        </Box>
                      </ListItemButton>
                    </ListItem>
                  </>
                )}
                <Divider sx={{ my: 1 }} />
                <ListItem disablePadding>
                  <ListItemButton onClick={() => { handleLogout(); setMobileOpen(false); }}>
                    <ListItemText primary={t('logout')} sx={{ color: 'error.main' }} />
                  </ListItemButton>
                </ListItem>
              </List>
            </>
          )}
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
