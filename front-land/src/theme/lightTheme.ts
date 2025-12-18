import { createTheme } from '@mui/material/styles';
import { colors } from './palette';

export const lightTheme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: colors.primaryBlue,
      light: colors.primaryBlueLight,
      dark: colors.primaryBlueDark,
    },
    secondary: {
      main: colors.primaryGreen,
      light: colors.primaryGreenLight,
      dark: colors.primaryGreenDark,
    },
    background: {
      default: colors.background,
      paper: '#f5f5f5',
    },
    text: {
      primary: colors.textPrimary,
      secondary: colors.textSecondary,
    },
    success: {
      main: colors.success,
    },
    error: {
      main: colors.error,
    },
    warning: {
      main: colors.warning,
    },
    info: {
      main: colors.info,
    },
  },
  typography: {
    fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
    h1: {
      fontWeight: 600,
    },
    h2: {
      fontWeight: 600,
    },
    h3: {
      fontWeight: 600,
    },
    h4: {
      fontWeight: 600,
    },
    h5: {
      fontWeight: 600,
    },
    h6: {
      fontWeight: 600,
    },
  },
  shape: {
    borderRadius: 8,
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          fontWeight: 600,
          borderRadius: 8,
        },
        contained: {
          boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
          '&:hover': {
            boxShadow: '0 4px 8px rgba(0,0,0,0.15)',
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
          borderRadius: 12,
        },
      },
    },
  },
});

